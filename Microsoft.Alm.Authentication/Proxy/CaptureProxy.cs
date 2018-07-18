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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Microsoft.Alm.Authentication.Test
{
    public class CaptureProxy : Proxy, IProxy
    {
        private const string PathSeparator = "\\";

        public CaptureProxy(RuntimeContext context, ProxyOptions options)
            : base(context, options)
        {
            if (options.Mode != ProxyMode.DataCapture)
                throw new ArgumentException($"`{GetType().Name}` requires `{nameof(ProxyOptions.Mode)} = {ProxyMode.DataCapture}`");

            SetService<INetwork>(new CaptureNetwork(context));
            SetService<ISettings>(new CaptureSettings(context, NormalizePath));
            SetService<IStorage>(new CaptureStorage(context, NormalizePath));
        }

        public override void Initialize(string repositoryPath)
        {
            var repositoryName = Path.GetFileName(repositoryPath);
            var fauxRepositoryPath = _options.FauxResultPath + '/' + repositoryName;

            AddFilter(repositoryPath, fauxRepositoryPath);
        }

        public override void WriteTestData(Stream writableStream)
        {
            if (writableStream is null)
                throw new ArgumentNullException(nameof(writableStream));
            if (!writableStream.CanWrite)
            {
                var inner = new InvalidDataException($"Method `{nameof(WriteTestData)}` requires `{nameof(writableStream)}` to be writable.");
                throw new ArgumentException(inner.Message, nameof(writableStream), inner);
            }

            if (_data is null)
                return;

            using (var writer = new StreamWriter(writableStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                string[] HeaderComment = new[]
                {
                    @"/**** Git Process Management Library ****"                                            ,
                    @" *"                                                                                   ,
                    @" * Copyright (c) Microsoft Corporation"                                               ,
                    @" * All rights reserved."                                                              ,
                    @" *"                                                                                   ,
                    @" * MIT License"                                                                       ,
                    @" *"                                                                                   ,
                    @" * Permission is hereby granted, free of charge, to any person obtaining a copy"      ,
                    @" * of this software and associated documentation files (the ""Software""), to deal"   ,
                    @" * in the Software without restriction, including without limitation the rights to"   ,
                    @" * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of"  ,
                    @" * the Software, and to permit persons to whom the Software is furnished to do so,"   ,
                    @" * subject to the following conditions:"                                              ,
                    @" *"                                                                                   ,
                    @" * The above copyright notice and this permission notice shall be included in all"    ,
                    @" * copies or substantial portions of the Software."                                   ,
                    @" *"                                                                                   ,
                    @" * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR"        ,
                    @" * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS"  ,
                    @" * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR"    ,
                    @" * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN" ,
                    @" * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION"   ,
                    @" * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."                   ,
                    @"**/"                                                                                  ,
                    @""                                                                                     ,
                    @"// Use `Formatting.Indented` to ease review readability."                             ,
                };

                for (int i = 0; i < HeaderComment.Length; i += 1)
                {
                    writer.WriteLine(HeaderComment[i]);
                }

                var data = _data;
                var dataFilter = new CapturedDataFilter(typeof(ICaptureService));

                foreach (var kvp in _storageFilters)
                {
                    Regex filter = BuildFilter(kvp.Key);

                    dataFilter.AddFilter(filter, kvp.Value);
                }

                var separaterFilter = new Regex(@"[/\\]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

                var testResultPath = separaterFilter.Replace(data.ResultPath, PathSeparator);
                var testResultFilter = BuildFilter(testResultPath);
                dataFilter.AddFilter(testResultFilter, _options.FauxResultPath);

                var slnRootPath = _options.SolutionDirectory;
                slnRootPath = separaterFilter.Replace(slnRootPath, PathSeparator);
                var slnRootFilter = BuildFilter(slnRootPath);
                dataFilter.AddFilter(slnRootFilter, _options.FauxPrefixPath);

                var testRootPath = _options.SolutionDirectory;
                var testRootFilter = BuildFilter(testRootPath);
                dataFilter.AddFilter(testRootFilter, _options.FauxPrefixPath);

                var userHomePath = _context.Settings.GetEnvironmentVariable("HOME")
                                ?? _context.Settings.GetEnvironmentVariable("USERPROFILE");
                var userHomeFilter = BuildFilter(userHomePath);
                dataFilter.AddFilter(userHomeFilter, _options.FauxHomePath);

                foreach(var runtimeService in EnumeratorServices())
                {
                    if (runtimeService is ICaptureService captureService)
                    {
                        ICapturedDataFilter filter = dataFilter.SupportsService(runtimeService.GetType())
                            ? dataFilter
                            : CapturedDataFilter.Null;

                        if (captureService.GetCapturedData(dataFilter, out object capturedData))
                        {
                            data.Services.Add(captureService.ServiceName, capturedData);
                        }
                    }
                }

                var jsonSettings = new JsonSerializerSettings()
                {
                    Converters = CustomJsonConverters,
                    Culture = System.Globalization.CultureInfo.InvariantCulture,
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    StringEscapeHandling = StringEscapeHandling.Default,
                };

                string serializedData = JsonConvert.SerializeObject(data, jsonSettings);

                writer.Write(serializedData);
            }
        }
    }
}
