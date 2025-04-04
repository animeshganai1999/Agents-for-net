﻿// <copyright file="DialogBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.BotBuilder.Dialogs;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.Extensions.Teams.Compat;
using Microsoft.Agents.BotBuilder.State;
using Microsoft.Agents.BotBuilder;

namespace BotConversationSsoQuickstart.Bots
{
    // This IBot implementation can run any type of Dialog. The use of type parameterization is to allows multiple different bots
    // to be run at different endpoints within the same project. This can be achieved by defining distinct Controller types
    // each with dependency on distinct IBot types, this way ASP Dependency Injection can glue everything together without ambiguity.
    // The ConversationState is used by the Dialog system. The UserState isn't, however, it might have been used in a Dialog implementation,
    // and the requirement is that all BotState objects are saved at the end of a turn.
    public class DialogBot<T>(ConversationState conversationState, T dialog, ILogger<DialogBot<T>> logger) : TeamsActivityHandler where T : Dialog
    {
        protected readonly BotState _conversationState = conversationState;
        protected readonly Dialog _dialog = dialog;
        protected readonly ILogger _logger = logger;

        /// <summary>
        /// Handle when a message is addressed to the bot.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A Task resolving to either a login card or the adaptive card of the Reddit post.</returns>
        /// <remarks>
        /// For more information on bot messaging in Teams, see the documentation
        /// https://docs.microsoft.com/en-us/microsoftteams/platform/bots/how-to/conversations/conversation-basics?tabs=dotnet#receive-a-message .
        /// </remarks>
        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with Message Activity.");
            await _dialog.RunAsync(turnContext, _conversationState, cancellationToken);
        }
    }
}