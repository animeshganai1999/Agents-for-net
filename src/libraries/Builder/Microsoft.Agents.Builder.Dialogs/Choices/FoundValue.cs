﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Agents.Builder.Dialogs.Choices
{
    /// <summary>This class is internal and should not be used.</summary>
    /// <remarks>Please use <see cref="FoundChoice"/> instead.</remarks>
    public class FoundValue
    {
        /// <summary>
        /// Gets or sets the value that was matched.
        /// </summary>
        /// <value>
        /// The value that was matched.
        /// </value>
        [JsonPropertyName("value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the index of the value that was matched.
        /// </summary>
        /// <value>
        /// The index of the value that was matched.
        /// </value>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the accuracy with which the value matched the specified portion of the utterance. A
        /// value of 1.0 would indicate a perfect match.
        /// </summary>
        /// <value>
        /// The accuracy with which the value matched the specified portion of the utterance. A
        /// value of 1.0 would indicate a perfect match.
        /// </value>
        [JsonPropertyName("score")]
        public float Score { get; set; }
    }
}
