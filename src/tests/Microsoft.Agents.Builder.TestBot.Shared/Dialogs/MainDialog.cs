﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.Builder.Dialogs;
using Microsoft.Agents.Builder.Dialogs.Prompts;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.Logging;

namespace Microsoft.Agents.Builder.TestBot.Shared.Dialogs
{
    // A root dialog responsible of understanding user intents and dispatching them sub tasks.
    public class MainDialog : ComponentDialog
    {
        private readonly ILogger _logger;

        public MainDialog(ILogger<MainDialog> logger, BookingDialog bookingDialog)
            : base(nameof(MainDialog))
        {
            _logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));

            // Add bookingDialog intents
            AddDialog(bookingDialog);

            // Create and add waterfall for main conversation loop
            // NOTE: we use a different task step if LUIS is not configured.
            WaterfallStep[] steps;
            steps = new WaterfallStep[]
            {
                PromptForTaskActionAsync,
                InvokeTaskActionAsyncNoLuis,
                ResumeMainLoopActionAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), steps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptForTaskActionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Use the text provided in ResumeMainLoopActionAsync or the default if it is the first time.
            var promptText = stepContext.Options?.ToString() ?? "What can I help you with today?";

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(promptText) }, cancellationToken);
        }

        private async Task<DialogTurnResult> InvokeTaskActionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            /*
            var luisResult = await _luisRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);

            switch (luisResult.TopIntent().intent)
            {
                case FlightBooking.Intent.BookFlight:
                    // Initialize BookingDetails with any entities we may have found in the response.
                    var bookingDetails = new BookingDetails()
                    {
                        // Get destination and origin from the composite entities arrays.
                        Destination = luisResult.Entities.To?.FirstOrDefault()?.Airport?.FirstOrDefault()?.FirstOrDefault(),
                        Origin = luisResult.Entities.From?.FirstOrDefault()?.Airport?.FirstOrDefault()?.FirstOrDefault(),

                        // This value will be a TIMEX. And we are only interested in a Date so grab the first result and drop the Time part.
                        // TIMEX is a format that represents DateTime expressions that include some ambiguity. e.g. missing a Year.
                        TravelDate = luisResult.Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault()?.Split('T')[0],
                    };

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetails, cancellationToken);

                case FlightBooking.Intent.GetWeather:
                    // We haven't implemented the GetWeatherDialog so we just display a message.
                    await stepContext.Context.SendActivityAsync("get weather flow here", cancellationToken: cancellationToken);
                    break;

                default:
                    // Catch all for unhandled intents
                    await stepContext.Context.SendActivityAsync($"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})", cancellationToken: cancellationToken);
                    break;
            }
            */

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> InvokeTaskActionAsyncNoLuis(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // We only handle book a flight if LUIS is not configured
            return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
        }

        private async Task<DialogTurnResult> ResumeMainLoopActionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // We have completed the task (or the user cancelled), we restart main dialog with a different prompt text.
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(Id, promptMessage, cancellationToken);
        }
    }
}
