﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Agents.Extensions.SharePoint.Models.Actions
{
    /// <summary>
    /// SharePoint focus parameters.
    /// </summary>
    public class FocusParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FocusParameters"/> class.
        /// </summary>
        public FocusParameters()
        {
            // Do nothing
        }

        /// <summary>
        /// This enum contains the different types of aria live options available in the SPFx framework.
        /// </summary>
        public enum AriaLiveOption
        {
            /// <summary>
            /// Polite
            /// </summary>
            [EnumMember(Value = "polite")]
            Polite,

            /// <summary>
            /// Assertive
            /// </summary>
            [EnumMember(Value = "assertive")]
            Assertive,

            /// <summary>
            /// Off
            /// </summary>
            [EnumMember(Value = "off")]
            Off
        }

        /// <summary>
        /// Gets or Sets the focus target of type <see cref="string"/>.
        /// </summary>
        /// <value>This value is the focus target.</value>
        public string FocusTarget { get; set; }

        /// <summary>
        /// Gets or Sets the aria live property of type <see cref="AriaLiveOption"/>.
        /// </summary>
        /// <value>This value sets the accessibility reading of the contents within the focus target.</value>
        public AriaLiveOption AriaLive { get; set; }
    }
}
