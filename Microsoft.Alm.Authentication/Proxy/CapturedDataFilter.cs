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
using System.Text.RegularExpressions;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public interface ICapturedDataFilter
    {
        IEnumerable<(Regex Regex, string Replacement)> Filters { get; }

        IEnumerable<Type> SupportedServices { get; }

        void AddFilter(Regex regex, string replacement);

        string ApplyFilter(string content);
    }

    internal class CapturedDataFilter : ICapturedDataFilter
    {
        public static readonly CapturedDataFilter Null = new CapturedDataFilter();

        public CapturedDataFilter(params Type[ ] supportedServiceTypes)
        {
            if (supportedServiceTypes is null)
                throw new ArgumentNullException(nameof(supportedServiceTypes));

            var enumerable = System.Linq.Enumerable.Where(supportedServiceTypes, x => x.GetType().IsAssignableFrom(typeof(IServiceProvider)));
            supportedServiceTypes = System.Linq.Enumerable.ToArray(enumerable);

            if (supportedServiceTypes.Length < 1)
                new ArgumentException("At least one service type must be specified.", nameof(supportedServiceTypes));

            _filters = new List<(Regex, string)>();
            _supportedServices = new HashSet<Type>(supportedServiceTypes);
            _syncpoint = new object();
        }

        public CapturedDataFilter(ICapturedDataFilter dataFilter)
        {
            _filters = new List<(Regex Regex, string Value)>(dataFilter.Filters);
            _supportedServices = new HashSet<Type>(dataFilter.SupportedServices);
            _syncpoint = new object();
        }

        private CapturedDataFilter()
        {
            _isNull = true;
        }

        private readonly List<(Regex Regex, string Value)> _filters;
        private readonly bool _isNull;
        private readonly HashSet<Type> _supportedServices;
        private readonly object _syncpoint;

        public IEnumerable<(Regex Regex, string Replacement)> Filters
        {
            get
            {
                (Regex Regex, string Value)[] filters;

                lock (_syncpoint)
                {
                    filters = System.Linq.Enumerable.ToArray(_filters);
                }

                return filters;
            }
        }

        public IEnumerable<Type> SupportedServices
        {
            get
            {
                if (_isNull)
                    return System.Linq.Enumerable.Empty<Type>();

                Type[] types;

                lock (_syncpoint)
                {
                    types = System.Linq.Enumerable.ToArray(_supportedServices);
                }

                return types;
            }
        }

        public void AddFilter(Regex regex, string replacement)
        {
            if (_isNull)
                return;

            if (regex is null)
                throw new ArgumentNullException(nameof(regex));
            if (replacement is null)
                throw new ArgumentNullException(nameof(replacement));

            lock (_syncpoint)
            {
                _filters.Add((regex, replacement));
            }
        }

        public string ApplyFilter(string content)
        {
            if (_isNull || content is null || content.Length == 0)
                return content;

            string filtered = content;

            lock (_syncpoint)
            {
                foreach (var filter in _filters)
                {
                    filtered = filter.Regex.Replace(filtered, filter.Value);
                }
            }

            return Ordinal.Equals(filtered, content)
                ? content
                : filtered;
        }

        public bool SupportsService(Type type)
        {
            if (_isNull)
                return false;

            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (!typeof(ICaptureService).IsAssignableFrom(type))
                throw new ArgumentException($"Only types of `{nameof(ICaptureService)}` are supported.");

            lock (_syncpoint)
            {
                return _supportedServices.Contains(type);
            }
        }
    }
}
