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
using System.IO;
using Newtonsoft.Json;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public class ReplayProxy : Proxy, IProxy
    {
        public ReplayProxy(RuntimeContext context, ProxyOptions options)
            : base(context, options)
        {
            if (options.Mode != ProxyMode.DataReplay)
                throw new ArgumentException($"`{GetType().Name}` requires `{nameof(ProxyOptions.Mode)} = {ProxyMode.DataReplay}`");

            SetService<INetwork>(new ReplayNetwork(context));
            SetService<ISettings>(new ReplaySettings(context, NormalizePath));
            SetService<IStorage>(new ReplayStorage(context, NormalizePath));
        }

        public override void ReadTestData(Stream readableStream)
        {
            if (readableStream is null)
                throw new ArgumentNullException(nameof(readableStream));
            if (!readableStream.CanRead)
            {
                var inner = new InvalidDataException($"Method `{nameof(ReadTestData)}` requires `{nameof(readableStream)}` to be readable.");
                throw new ArgumentException(inner.Message, nameof(readableStream), inner);
            }

            using (var reader = new StreamReader(readableStream, false))
            {
                var metajson = reader.ReadToEnd();

                var settings = new JsonSerializerSettings()
                {
                    Converters = CustomJsonConverters,
                    Culture = System.Globalization.CultureInfo.InvariantCulture,
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    StringEscapeHandling = StringEscapeHandling.Default,
                };

                ProxyData data = JsonConvert.DeserializeObject<ProxyData>(metajson, settings);

                if (data.Services is null)
                    throw new ReplayNotFoundException("Proxy data contained no captured replay data.");

                var serviceLookup = new Dictionary<string, IReplayService>(Ordinal);

                foreach (var runtimeService in EnumeratorServices())
                {
                    if (runtimeService is IReplayService replayService)
                    {
                        serviceLookup.Add(replayService.ServiceName, replayService);
                    }
                }

                foreach (var serviceData in data.Services)
                {
                    if (!serviceLookup.TryGetValue(serviceData.Key, out IReplayService replayService))
                        throw new InvalidOperationException("Expected replay service is unavailable");

                    replayService.SetReplayData(serviceData.Value);
                }

                _data = data;
            }
        }
    }
}
