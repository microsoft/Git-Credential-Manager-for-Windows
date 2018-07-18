/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    internal class NestedDictionatyConverter : CustomCreationConverter<Dictionary<string, Dictionary<string, string>>>
    {
        public override bool CanRead
            => true;

        public override bool CanWrite
            => true;

        public override Dictionary<string, Dictionary<string, string>> Create(Type objectType)
        {
            return CanConvert(objectType)
                ? new Dictionary<string, Dictionary<string, string>>(Ordinal)
                : null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<string, Dictionary<string, string>>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!CanConvert(objectType))
                return null;

            if (!(existingValue is Dictionary<string, Dictionary<string, string>> externTable))
            {
                externTable = new Dictionary<string, Dictionary<string, string>>(Ordinal);
                existingValue = externTable;
            }

            var externArray = JToken.ReadFrom(reader);

            foreach (var externObject in externArray.Values())
            {
                var externKey = (externObject as JProperty).Name;
                var innerArray = (externObject as JProperty).Value;

                foreach (var innerObject in innerArray.Values())
                {
                    var innerKey = (innerObject as JProperty).Name;
                    var value = (innerObject as JProperty).Value?.Value<string>();

                    if (!externTable.TryGetValue(externKey, out var innerTable))
                    {
                        innerTable = new Dictionary<string, string>(OrdinalIgnoreCase);
                        externTable.Add(externKey, innerTable);
                    }

                    innerTable[innerKey] = value;
                }
            }

            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Dictionary<string, Dictionary<string, string>> nestedDictionary)
            {
                writer.WriteStartArray();

                foreach (var externKvp in nestedDictionary)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName(externKvp.Key);
                    writer.WriteStartArray();

                    foreach (var internKvp in externKvp.Value)
                    {
                        writer.WriteStartObject();

                        writer.WritePropertyName(internKvp.Key);
                        writer.WriteValue(internKvp.Value);

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }
        }
    }
}
