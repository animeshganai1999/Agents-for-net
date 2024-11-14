﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Agents.Protocols.Primitives;
using System.Text.Json;

namespace Microsoft.Agents.Protocols.Serializer
{
    internal class AnimationCardConverter : ConnectorConverter<AnimationCard>
    {
        protected override bool TryReadCollectionProperty(ref Utf8JsonReader reader, AnimationCard value, string propertyName, JsonSerializerOptions options)
        {
            if (propertyName.Equals(nameof(value.Buttons)))
            {
                SetCollection(ref reader, value.Buttons, options);
            }
            else if (propertyName.Equals(nameof(value.Media)))
            {
                SetCollection(ref reader, value.Media, options);
            }
            else
            {
                return false;
            }

            return true;
        }

        protected override bool TryReadGenericProperty(ref Utf8JsonReader reader, AnimationCard value, string propertyName, JsonSerializerOptions options)
        {
            if (propertyName.Equals(nameof(value.Value)))
            {
                SetGenericProperty(ref reader, data => value.Value = data, options);
            }
            else
            {
                return false;
            }

            return true;
        }

        protected override void ReadExtensionData(ref Utf8JsonReader reader, AnimationCard value, string propertyName, JsonSerializerOptions options)
        {
        }

        protected override bool TryReadExtensionData(ref Utf8JsonReader reader, AnimationCard value, string propertyName, JsonSerializerOptions options)
        {
            return false;
        }

        protected override bool TryWriteExtensionData(Utf8JsonWriter writer, AnimationCard value, string propertyName)
        {
            return false;
        }
    }
}
