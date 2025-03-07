﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.BotBuilder.Errors;
using Microsoft.Agents.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Agents.BotBuilder
{
    /// <summary>
    /// This class enables sending chunked text in a series of "intermediate" messages, on an interval.  This
    /// gives the UX of a streamed message. When complete, a final message (ActivityType.Message) is sent with 
    /// the full message and optional Attachments.
    /// 
    /// The expected sequence of calls is:
    /// 
    /// `QueueInformativeUpdateAsync()`, `QueueTextChunk()`, `QueueTextChunk()`, ..., `EndStreamAsync()`.
    ///
    ///  Once `EndStreamAsync()` is called, the stream is considered ended and no further updates can be sent.
    /// </summary>
    /// <remarks>
    /// Teams channels require that <see cref="QueueInformativeUpdateAsync"/> is called before calling <see cref="QueueTextChunk"/>.
    /// </remarks>
    /// <remarks>
    /// Only Teams and WebChat support streaming messages.  However, channels that do not support
    /// streaming messages will only receive the final message when <see cref="EndStreamAsync"/> is called.
    /// </remarks>
    /// <remarks>
    /// This class support throttling via the <see cref="Interval"/> property.  Teams and Azure Bot Channels require
    /// some throttling since services like OpenAI produce streams that exceed allowed Channel message limits.
    /// Teams defaults to 1000ms per intermediate message, and WebChat 500ms.  Reducing the Interval could result
    /// in message delivery failures.
    /// </remarks>
    public class StreamingResponse
    {
        private readonly ITurnContext _context;
        private int _nextSequence = 1;
        private bool _ended = false;
        private Timer _timer;
        private bool _messageUpdated = false;
        private bool _informativeSent = false;
        private bool _isTeamsChannel;

        // Queue for outgoing activities
        private readonly List<Func<IActivity>> _queue = [];
        private readonly AutoResetEvent _queueEmpty = new(false);

        /// <summary>
        /// Set Attachments to be included on the final message.
        /// </summary>
        public List<Attachment>? Attachments { get; set; } = new();

        /// <summary>
        /// Gets the stream ID of the current response.
        /// Assigned after the initial update is sent.
        /// </summary>
        private string StreamId { get; set; }

        /// <summary>
        /// The buffered message.
        /// </summary>
        public string Message { get; private set; } = "";

        /// <summary>
        /// The interval in milliseconds at which intermediate messages are sent.
        /// </summary>
        /// <remarks>
        /// Teams default: 1000
        /// WebChat default: 500
        /// </remarks>
        public int Interval { get; set; }

        /// <summary>
        /// The amount of time in milliseconds EndStreamSync will wait.
        /// </summary>
        /// <remarks>
        /// This should be adjusted based on Interval and the amount of
        /// text being chunked.
        /// </remarks>
        public int Timeout { get; set; } = System.Threading.Timeout.Infinite;

        /// <summary>
        /// Indicate if the current channel supports intermediate messages.
        /// </summary>
        /// <remarks>
        /// Channels that don't support intermediate messages will buffer
        /// text, and send a normal final message when EndStreamAsync is called.
        /// </remarks>
        public bool IsStreamingChannel { get; private set; }

        /// <summary>
        /// Gets the number of updates sent for the stream.
        /// </summary>
        /// <returns>Number of updates sent so far.</returns>
        public int UpdatesSent() => _nextSequence - 1;

        /// <summary>
        /// Creates a new instance of the <see cref="StreamingResponse"/> class.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation with the user.</param>
        public StreamingResponse(ITurnContext turnContext)
        {
            ArgumentNullException.ThrowIfNull(turnContext);

            _context = turnContext;
            SetDefaults(turnContext);
        }

        /// <summary>
        /// Queues an informative update to be sent to the client.
        /// </summary>
        /// <param name="text">Text of the update to send.</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="TeamsAIException">Throws if the stream has already ended.</exception>
        public async Task QueueInformativeUpdateAsync(string text, CancellationToken cancellationToken = default)
        {
            if (!IsStreamingChannel)
            {
                return;
            }

            Func<IActivity> queueFunc;

            lock (this)
            {
                if (_ended)
                {
                    throw Core.Errors.ExceptionHelper.GenerateException<InvalidOperationException>(ErrorHelper.StreamingResponseEnded, null);
                }

                _informativeSent = true;

                queueFunc = () =>
                {
                    return new Activity
                    {
                        Type = ActivityTypes.Typing,
                        Text = text,
                        Entities = [new StreamInfo()
                            {
                                StreamType = StreamTypes.Informative,
                                StreamSequence = _nextSequence++,
                            }]
                    };
                };

                if (StreamStarted())
                {
                    QueueActivity(queueFunc);
                    return;
                }
            }

            // if we got this far, the stream has been started so just send directly and start.
            await SendActivityAsync(queueFunc(), cancellationToken).ConfigureAwait(false);
            StartStream();
        }

        /// <summary>
        /// Queues a chunk of partial message text to be sent to the client.
        /// </summary>
        /// <param name="text">Partial text of the message to send.</param>
        /// <param name="citations">Citations to include in the message.</param>
        /// <exception cref="InvalidOperationException">Throws if the stream has already ended.</exception>
        public void QueueTextChunk(string text) //, IList<Citation>? citations = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            lock (this)
            {
                if (_ended)
                {
                    throw Core.Errors.ExceptionHelper.GenerateException<InvalidOperationException>(ErrorHelper.StreamingResponseEnded, null);
                }

                if (!_informativeSent && _isTeamsChannel)
                {
                    throw Core.Errors.ExceptionHelper.GenerateException<InvalidOperationException>(ErrorHelper.TeamsRequiresInformativeFirst, null);
                }

                // buffer all chunks
                Message += text;
                _messageUpdated = true;

                StartStream();
            }
        }

        /// <summary>
        /// Ends the stream by sending the final message to the client.
        /// </summary>
        /// <remarks>
        /// Since the messages are sent on an interval, this call will block until all have been sent
        /// before sending the final Message.
        /// </remarks>
        /// <returns>A Task representing the async operation</returns>
        /// <exception cref="InvalidOperationException">Throws if the stream has already ended.</exception>
        public async Task EndStreamAsync(CancellationToken cancellationToken = default)
        {
            if (!IsStreamingChannel)
            {
                lock (this)
                {
                    if (_ended)
                    {
                        throw Core.Errors.ExceptionHelper.GenerateException<InvalidOperationException>(ErrorHelper.StreamingResponseEnded, null);
                    }

                    _ended = true;
                }

                // Timer isn't running for non-streaming channels.  Just send the Message buffer as a message.
                if (!string.IsNullOrWhiteSpace(Message))
                {
                    await _context.SendActivityAsync(new Activity() { Type = ActivityTypes.Message, Text = Message, Attachments = Attachments }, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                lock (this)
                {
                    if (_ended)
                    {
                        throw Core.Errors.ExceptionHelper.GenerateException<InvalidOperationException>(ErrorHelper.StreamingResponseEnded, null);
                    }

                    _ended = true;

                    if (UpdatesSent() == 0)
                    {
                        // nothing was queued.  nothing to "end".
                        return;
                    }
                }

                if (StreamStarted())
                {
                    // Wait for queue items to be sent per Interval
                    _queueEmpty.WaitOne(Timeout);
                }

                if (!string.IsNullOrEmpty(Message) || Attachments != null && Attachments.Count > 0)
                {
                    var activity = new Activity()
                    {
                        Type = ActivityTypes.Message,
                        Text = Message,
                        Entities = [new StreamInfo() { StreamType = StreamTypes.Final }],
                        Attachments = Attachments
                    };

                    await SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Queue an activity to be sent to the client.
        /// </summary>
        /// <param name="factory"></param>
        private void QueueActivity(Func<IActivity> factory)
        {
            if (factory != null)
            {
                _queue.Add(factory);
                _queueEmpty.Reset();
            }
        }

        /// <summary>
        /// Queue the next chunk of text to be sent to the client.
        /// </summary>
        private void QueueNextChunkActivity()
        {
            if (!_messageUpdated)
            {
                return;
            }

            // Queue a chunk of text to be sent. Is done via a Func to create
            // the Activity so that member variables are evaluated at time of 
            // interval send.
            QueueActivity(() =>
            {
                // Send typing activity
                var activity = new Activity
                {
                    Type = ActivityTypes.Typing,
                    Text = Message,
                    Entities = []
                };

                var sequence = _nextSequence++;

                activity.Entities.Add(new StreamInfo()
                {
                    StreamType = StreamTypes.Streaming,
                    StreamSequence = sequence,
                });

                _messageUpdated = false;

                return activity;
            });
        }

        private void SetDefaults(ITurnContext turnContext)
        {
            _isTeamsChannel = string.Equals(Channels.Msteams, turnContext.Activity.ChannelId, StringComparison.OrdinalIgnoreCase);

            if (_isTeamsChannel)
            {
                // Teams MUST use the Activity.Id returned from the first Informative message for
                // subsequent intermediate messages.  Do not set StreamId here.

                Interval = 1000;
                IsStreamingChannel = true;
            }
            else if (string.Equals(turnContext.Activity.ChannelId, Channels.Webchat, StringComparison.OrdinalIgnoreCase))
            {
                Interval = 500;
                IsStreamingChannel = true;

                // WebChat will use whatever StreamId is created.
                StreamId = Guid.NewGuid().ToString();
            }
            else
            {
                IsStreamingChannel = false;
            }
        }

        private bool StreamStarted()
        {
            return _timer != null;
        }

        private void StartStream()
        {
            if (_timer == null && IsStreamingChannel)
            {
                _timer = new Timer(SendIntermediateMessage, null, Interval, System.Threading.Timeout.Infinite);
            }
        }

        private void StopStream()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private async void SendIntermediateMessage(object state)
        {
            // Send one buffered Activity per interval.
            IActivity activity = null;

            lock (this)
            {
                QueueNextChunkActivity();

                if (_queue.Count > 0)
                {
                    activity = _queue[0]();
                    _queue.RemoveAt(0);

                    // Limit intermediate message to the interval
                    _timer.Change(Interval, System.Threading.Timeout.Infinite);
                }
                else if (_ended)
                {
                    _queueEmpty.Set();
                    StopStream();
                }
                else
                {
                    // Nothing is in the queue, and not ending, so chances are
                    // the chunking is slow.  We can speed up the interval to
                    // pick up the next chunk faster.
                    _timer.Change(200, System.Threading.Timeout.Infinite);
                }
            }

            await SendActivityAsync(activity, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task SendActivityAsync(IActivity activity, CancellationToken cancellationToken)
        {
            if (activity != null)
            {
                if (!string.IsNullOrEmpty(StreamId))
                {
                    activity.Id = StreamId;
                    ((StreamInfo)activity.Entities[0]).StreamId = StreamId;
                }

                var response = await _context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(StreamId))
                {
                    StreamId = response.Id;
                }
            }
        }
    }
}
