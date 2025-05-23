﻿using Microsoft.Agents.Extensions.Teams.AI.Models;
using Microsoft.Agents.Extensions.Teams.AI.Prompts;
using Microsoft.Agents.Extensions.Teams.AI.Tokenizers;
using Microsoft.Agents.Extensions.Teams.AI.Validators;
using static Microsoft.Agents.Extensions.Teams.AI.Models.IPromptCompletionModelEvents;

namespace Microsoft.Agents.Extensions.Teams.AI.Clients
{
    /// <summary>
    /// Options for an LLMClient instance.
    /// </summary>
    /// <typeparam name="TContent">
    /// Type of message content returned for a 'success' response. The `response.message.content` field will be of type TContent.
    /// </typeparam>
    public class LLMClientOptions<TContent>
    {
        /// <summary>
        /// AI model to use for completing prompts.
        /// </summary>
        public IPromptCompletionModel Model { get; set; }

        /// <summary>
        /// Prompt to use for the conversation.
        /// </summary>
        public PromptTemplate Template { get; set; }

        /// <summary>
        /// Tokenizer used when rendering the prompt or counting tokens.
        /// </summary>
        public ITokenizer Tokenizer { get; set; } = new GPTTokenizer();

        /// <summary>
        /// Response validator used when completing prompts.
        /// </summary>
        public IPromptResponseValidator Validator { get; set; } = new DefaultResponseValidator();

        /// <summary>
        /// Memory variable used for storing conversation history.
        ///
        /// The history will be stored as a `Message[]` and the variable defaults to `conversation.history`.
        /// </summary>
        public string HistoryVariable { get; set; } = "conversation.history";

        /// <summary>
        /// Memory variable used for storing the users input message.
        ///
        /// The users input is expected to be a `string` but it's optional and defaults to `temp.input`.
        /// </summary>
        public string InputVariable { get; set; } = "temp.input";

        /// <summary>
        /// Maximum number of conversation history messages that will be persisted to memory.
        /// </summary>
        public int MaxHistoryMessages { get; set; } = 10;

        /// <summary>
        /// Maximum number of automatic repair attempts that will be made.
        /// </summary>
        public int MaxRepairAttempts { get; set; } = 3;

        /// <summary>
        /// If true, any repair attempts will be logged to the console.
        /// </summary>
        public bool LogRepairs { get; set; } = false;

        /// <summary>
        /// Optional message to send a client at the start of a streaming response.
        /// </summary>
        public string? StartStreamingMessage { get; set; }

        /// <summary>
        /// Optional handler to run when a stream is about to conclude.
        /// </summary>
        public ResponseReceivedHandler? EndStreamHandler;

        /// <summary>
        /// Optional, controls the feedback loop for streaming responses.
        /// </summary>
        public bool? EnableFeedbackLoop { get; set; }

        /// <summary>
        /// Optional. Represents the type of feedback loop. It can be set to one of "default" or "custom".
        /// </summary>
        public string? FeedbackLoopType { get; set; }

        /// <summary>
        /// Creates an instance of `LLMClientOptions`
        /// </summary>
        /// <param name="model">AI model to use for completing prompts.</param>
        /// <param name="template">Prompt to use for the conversation.</param>
        public LLMClientOptions(IPromptCompletionModel model, PromptTemplate template)
        {
            this.Model = model;
            this.Template = template;
        }
    }
}
