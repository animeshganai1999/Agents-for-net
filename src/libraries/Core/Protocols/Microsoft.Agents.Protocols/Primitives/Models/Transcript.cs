// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.Agents.Protocols.Primitives
{
    /// <summary> Transcript. </summary>
    public class Transcript
    {
        public Transcript()
        {
            Activities = new List<Activity>();
        }

        /// <summary> Initializes a new instance of Transcript. </summary>
        public Transcript(IList<Activity> activities = default)
        {
            Activities = activities ?? new List<Activity>();
        }

        /// <summary> A collection of Activities that conforms to the Transcript schema. </summary>
        public IList<Activity> Activities { get; }
    }
}
