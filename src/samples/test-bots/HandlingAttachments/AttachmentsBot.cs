﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.BotBuilder;
using Microsoft.Agents.BotBuilder.App;
using Microsoft.Agents.BotBuilder.State;
using Microsoft.Agents.Connector;
using Microsoft.Agents.Connector.Types;
using Microsoft.Agents.Core.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HandlingAttachmentsBot
{
    // Represents a bot that processes incoming activities.
    // For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    // This is a Transient lifetime service. Transient lifetime services are created
    // each time they're requested. For each Activity received, a new instance of this
    // class is created. Objects that are expensive to construct, or have a lifetime
    // beyond the single turn, should be carefully managed.

    public class AttachmentsBot : AgentApplication
    {
        public AttachmentsBot(AgentApplicationOptions options) : base(options)
        {
        }

        [ConversationUpdateRoute(Event = ConversationUpdateEvents.MembersAdded)]
        protected async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        $"Welcome to AttachmentsBot {member.Name}." +
                        $" This bot will introduce you to Attachments." +
                        $" Please select an option",
                        cancellationToken: cancellationToken);
                    await DisplayOptionsAsync(turnContext, cancellationToken);
                }
            }
        }

        [ActivityRoute(Type = ActivityTypes.Message, Rank = RouteRank.Last)]
        protected async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
        {
            var reply = await ProcessInput(turnContext, turnState, cancellationToken);

            // Respond to the user.
            await turnContext.SendActivityAsync(reply, cancellationToken);
            await DisplayOptionsAsync(turnContext, cancellationToken);
        }

        private static async Task DisplayOptionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Create a HeroCard with options for the user to interact with the bot.
            var card = new HeroCard
            {
                Text = "You can upload an image or select one of the following choices",
                Buttons =
                [
                    // Note that some channels require different values to be used in order to get buttons to display text.
                    // In this code the emulator is accounted for with the 'title' parameter, but in other channels you may
                    // need to provide a value for other parameters like 'text' or 'displayText'.
                    new CardAction(ActionTypes.ImBack, title: "1. Inline Attachment", value: "1"),
                    new CardAction(ActionTypes.ImBack, title: "2. Internet Attachment", value: "2"),
                    new CardAction(ActionTypes.ImBack, title: "3. Uploaded Attachment", value: "3"),
                ],
            };

            var reply = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        // Given the input from the message, create the response.
        private static async Task<IActivity> ProcessInput(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
        {
            IActivity reply;

            if (turnState.Temp.InputFiles.Any())
            {
                reply = MessageFactory.Text($"There are {turnState.Temp.InputFiles.Count} attachments.");
            }
            else
            {
                // Send at attachment to the user.
                reply = await HandleOutgoingAttachment(turnContext, turnContext.Activity, cancellationToken);
            }

            return reply;
        }

        // Returns a reply with the requested Attachment
        private static async Task<IActivity> HandleOutgoingAttachment(ITurnContext turnContext, IActivity activity, CancellationToken cancellationToken)
        {
            // Look at the user input, and figure out what kind of attachment to send.
            IActivity reply;
            if (activity.Text.StartsWith('1'))
            {
                reply = MessageFactory.Text("This is an inline attachment.");
                reply.Attachments = [GetInlineAttachment()];
            }
            else if (activity.Text.StartsWith('2'))
            {
                reply = MessageFactory.Text("This is an attachment from a HTTP URL.");
                reply.Attachments = [GetInternetAttachment()];
            }
            else if (activity.Text.StartsWith('3'))
            {
                reply = MessageFactory.Text("This is an uploaded attachment.");

                // Get the uploaded attachment.
                var uploadedAttachment = await UploadAttachmentAsync(turnContext, activity.ServiceUrl, activity.Conversation.Id, cancellationToken);
                reply.Attachments = [uploadedAttachment];
            }
            else
            {
                // The user did not enter input that this bot was built to handle.
                reply = MessageFactory.Text("Your input was not recognized please try again.");
            }

            return reply;
        }

        // Creates an inline attachment sent from the bot to the user using a base64 string.
        // Using a base64 string to send an attachment will not work on all channels.
        // Additionally, some channels will only allow certain file types to be sent this way.
        // For example a .png file may work but a .pdf file may not on some channels.
        // Please consult the channel documentation for specifics.
        private static Attachment GetInlineAttachment()
        {
            var imagePath = Path.Combine(Environment.CurrentDirectory, @"Resources", "architecture-resize.png");
            var imageData = Convert.ToBase64String(File.ReadAllBytes(imagePath));

            return new Attachment
            {
                Name = @"Resources\architecture-resize.png",
                ContentType = "image/png",
                ContentUrl = $"data:image/png;base64,{imageData}",
            };
        }

        // Creates an "Attachment" to be sent from the bot to the user from an uploaded file.
        private static async Task<Attachment> UploadAttachmentAsync(ITurnContext turnContext, string serviceUrl, string conversationId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                throw new ArgumentNullException(nameof(serviceUrl));
            }

            if (string.IsNullOrWhiteSpace(conversationId))
            {
                throw new ArgumentNullException(nameof(conversationId));
            }

            var imagePath = Path.Combine(Environment.CurrentDirectory, @"Resources", "agents-sdk.png");

            var connector = turnContext.Services.Get<IConnectorClient>();

            // This only supports payloads smaller than 260k
            var response = await connector.Conversations.UploadAttachmentAsync(
                conversationId,
                new AttachmentData
                {
                    Name = @"Resources\agents-sdk.png",
                    OriginalBase64 = File.ReadAllBytes(imagePath),
                    Type = "image/png",
                },
                cancellationToken);

            var attachmentUri = connector.Attachments.GetAttachmentUri(response.Id);

            return new Attachment
            {
                Name = @"Resources\agents-sdk.png",
                ContentType = "image/png",
                ContentUrl = attachmentUri,
            };
        }

        // Creates an <see cref="Attachment"/> to be sent from the bot to the user from a HTTP URL.
        private static Attachment GetInternetAttachment()
        {
            // ContentUrl must be HTTPS.
            return new Attachment
            {
                Name = @"Resources\architecture-resize.png",
                ContentType = "image/png",
                ContentUrl = "https://docs.microsoft.com/en-us/bot-framework/media/how-it-works/architecture-resize.png",
            };
        }
    }
}
