﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.BotBuilder.App;
using Microsoft.Agents.Core.Models;
using Microsoft.Agents.Core.Serialization;
using Microsoft.Agents.Extensions.Teams.Models;
using System;
using System.Threading.Tasks;

namespace Microsoft.Agents.Extensions.Teams.App.Meetings
{
    /// <summary>
    /// Meetings class to enable fluent style registration of handlers related to Microsoft Teams Meetings.
    /// </summary>
    public class MeetingsFeature
    {
        private readonly Application _app;

        /// <summary>
        /// Creates a new instance of the Meetings class.
        /// </summary>
        /// <param name="app"></param> The top level application class to register handlers with.
        public MeetingsFeature(Application app)
        {
            this._app = app;
        }

        /// <summary>
        /// Handles Microsoft Teams meeting start events.
        /// </summary>
        /// <param name="handler">Function to call when a Microsoft Teams meeting start event activity is received from the connector.</param>
        /// <returns>The application instance for chaining purposes.</returns>
        public Application OnStart(MeetingStartHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            RouteSelectorAsync routeSelector = (context, _) => Task.FromResult
            (
                string.Equals(context.Activity?.Type, ActivityTypes.Event, StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Activity?.ChannelId, Channels.Msteams)
                && string.Equals(context.Activity?.Name, "application/vnd.microsoft.meetingStart")
            );
            RouteHandler routeHandler = async (turnContext, turnState, cancellationToken) =>
            {
                MeetingStartEventDetails meeting = ProtocolJsonSerializer.ToObject<MeetingStartEventDetails>(turnContext.Activity.Value, () => new());
                await handler(turnContext, turnState, meeting, cancellationToken);
            };
            _app.AddRoute(routeSelector, routeHandler, isInvokeRoute: false);
            return _app;
        }

        /// <summary>
        /// Handles Microsoft Teams meeting end events.
        /// </summary>
        /// <param name="handler">Function to call when a Microsoft Teams meeting end event activity is received from the connector.</param>
        /// <returns>The application instance for chaining purposes.</returns>
        public Application OnEnd(MeetingEndHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            RouteSelectorAsync routeSelector = (context, _) => Task.FromResult
            (
                string.Equals(context.Activity?.Type, ActivityTypes.Event, StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Activity?.ChannelId, Channels.Msteams)
                && string.Equals(context.Activity?.Name, "application/vnd.microsoft.meetingEnd")
            );
            RouteHandler routeHandler = async (turnContext, turnState, cancellationToken) =>
            {
                MeetingEndEventDetails meeting = ProtocolJsonSerializer.ToObject<MeetingEndEventDetails>(turnContext.Activity.Value, () => new());
                await handler(turnContext, turnState, meeting, cancellationToken);
            };
            _app.AddRoute(routeSelector, routeHandler, isInvokeRoute: false);
            return _app;
        }

        /// <summary>
        /// Handles Microsoft Teams meeting participants join events.
        /// </summary>
        /// <param name="handler">Function to call when a Microsoft Teams meeting participants join event activity is received from the connector.</param>
        /// <returns>The application instance for chaining purposes.</returns>
        public Application OnParticipantsJoin(MeetingParticipantsEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            RouteSelectorAsync routeSelector = (context, _) => Task.FromResult
            (
                string.Equals(context.Activity?.Type, ActivityTypes.Event, StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Activity?.ChannelId, Channels.Msteams)
                && string.Equals(context.Activity?.Name, "application/vnd.microsoft.meetingParticipantJoin")
            );
            RouteHandler routeHandler = async (turnContext, turnState, cancellationToken) =>
            {
                MeetingParticipantsEventDetails meeting = ProtocolJsonSerializer.ToObject<MeetingParticipantsEventDetails>(turnContext.Activity.Value, () => new());
                await handler(turnContext, turnState, meeting, cancellationToken);
            };
            _app.AddRoute(routeSelector, routeHandler, isInvokeRoute: false);
            return _app;
        }

        /// <summary>
        /// Handles Microsoft Teams meeting participants leave events.
        /// </summary>
        /// <param name="handler">Function to call when a Microsoft Teams meeting participants leave event activity is received from the connector.</param>
        /// <returns>The application instance for chaining purposes.</returns>
        public Application OnParticipantsLeave(MeetingParticipantsEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            RouteSelectorAsync routeSelector = (context, _) => Task.FromResult
            (
                string.Equals(context.Activity?.Type, ActivityTypes.Event, StringComparison.OrdinalIgnoreCase)
                && string.Equals(context.Activity?.ChannelId, Channels.Msteams)
                && string.Equals(context.Activity?.Name, "application/vnd.microsoft.meetingParticipantLeave")
            );
            RouteHandler routeHandler = async (turnContext, turnState, cancellationToken) =>
            {
                MeetingParticipantsEventDetails meeting = ProtocolJsonSerializer.ToObject<MeetingParticipantsEventDetails>(turnContext.Activity.Value, () => new());
                await handler(turnContext, turnState, meeting, cancellationToken);
            };
            _app.AddRoute(routeSelector, routeHandler, isInvokeRoute: false);
            return _app;
        }
    }
}
