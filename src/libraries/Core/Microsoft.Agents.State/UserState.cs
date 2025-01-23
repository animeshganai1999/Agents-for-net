﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Core.Interfaces;
using Microsoft.Agents.Storage;
using System;

namespace Microsoft.Agents.State
{
    /// <summary>
    /// Defines a state keyed to a user.
    /// </summary>
    /// <remarks>
    /// Conversation state is available in any turn in a specific conversation, regardless of user,
    /// such as in a group conversation.
    /// 
    /// This implementation should NOT be used as a singleton.  This includes registering as singleton
    /// in DI.
    /// </remarks>
    /// <param name="storage">The storage layer to use.</param>
    public class UserState(IStorage storage) : BotState(storage, ScopeName)
    {
        public static readonly string ScopeName = "user";

        /// <summary>
        /// Gets the key to use when reading and writing state to and from storage.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <returns>The storage key.</returns>
        /// <remarks>
        /// User state includes the channel ID and user ID as part of its storage key.
        /// </remarks>
        protected override string GetStorageKey(ITurnContext turnContext)
        {
            var channelId = turnContext.Activity.ChannelId ?? throw new InvalidOperationException("invalid activity-missing channelId");
            var userId = turnContext.Activity.From?.Id ?? throw new InvalidOperationException("invalid activity-missing From.Id");
            return $"{channelId}/users/{userId}";
        }
    }
}
