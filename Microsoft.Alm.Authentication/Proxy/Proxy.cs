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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public enum ProxyMode
    {
        DataCapture,
        DataPassthrough,
        DataReplay,
    }

    public interface IProxy
    {
        /// <summary>
        /// Gets the output data for `<see cref="CaptureProxy"/>`, the input data for `<see cref="ReplayProxy"/>`, and is otherwise unused.
        /// </summary>
        ProxyData Data { get; }

        /// <summary>
        /// Gets the `<seealso cref="ProxyMode"/>` the proxy was initialized with.
        /// </summary>
        ProxyMode Mode { get; }

        /// <summary>
        /// Gets the options used to initialize the proxy, can be used to update settings after initialization.
        /// </summary>
        ProxyOptions Options { get; }

        /// <summary>
        /// Returns an enumeration of all services known to the proxy.
        /// </summary>
        IEnumerable<IRuntimeService> EnumerateServices();

        /// <summary>
        /// Returns the service associated with `<typeparamref name="T"/>` from the proxy's collection of known services; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <typeparam name="T">The `<seealso cref="IRuntimeService"/>` based type of the service to be returned.</typeparam>
        T GetService<T>() where T : class, IRuntimeService;

        /// <summary>
        /// Initializes this instance of `<see cref="IProxy"/>` with `<paramref name="currentDirectory"/>` as the current working directory.
        /// </summary>
        /// <param name="currentDirectory">The directory this instance of `<see cref="Proxy"/>` should report as the current working directory.</param>
        void Initialize(string currentDirectory);

        /// <summary>
        /// Reads replay test data from `<paramref name="readableStream"/>`.
        /// <para/>
        /// Only supported when `<see cref="Mode"/>` returns `<seealso cref="ProxyMode.DataReplay"/>`.
        /// </summary>
        /// <param name="readableStream">The readable stream from which to read the proxy's replay test data.</param>
        void ReadTestData(Stream readableStream);

        /// <summary>
        /// Sets the service associated with `<typeparamref name="T"/>` in the proxy's collection of known services.
        /// </summary>
        /// <typeparam name="T">The `<seealso cref="IRuntimeService"/>` based type of the service to be added or updated.</typeparam>
        /// <param name="service">The service to be added or updated.</param>
        /// <exception cref="ArgumentNullException">`<paramref name="service"/>` is `<see langword="null"/>`.</exception>
        void SetService<T>(T service) where T : class, IRuntimeService;

        /// <summary>
        /// Writes captured test data to `<paramref name="writableStream"/>`.
        /// <para/>
        /// Only supported when `<see cref="Mode"/>` returns `<seealso cref="ProxyMode.DataCapture"/>`.
        /// </summary>
        /// <param name="writableStream">The writable stream which to write captured tests data.</param>
        void WriteTestData(Stream writableStream);
    }

    public abstract class Proxy : IProxy
    {
        protected static readonly IList<JsonConverter> CustomJsonConverters  = new JsonConverter[] { new NestedDictionatyConverter(), };

        private static readonly Regex NormalizePathRegex = new Regex(@"[\\/]+", RegexOptions.CultureInvariant);

        protected Proxy(RuntimeContext context, ProxyOptions options)
        {
            _context = context;

            _data = new ProxyData();
            _mode = options.Mode;
            _options = options;
            _services = new ConcurrentDictionary<Type, object>();
            _storageFilters = new ConcurrentDictionary<string, string>(Ordinal);

            SetService(_context.Network);
            SetService(_context.Settings);
            SetService(_context.Storage);
        }

        protected readonly RuntimeContext _context;
        protected ProxyData _data;
        protected readonly ConcurrentDictionary<string, string> _storageFilters;
        protected readonly ProxyMode _mode;
        protected readonly ProxyOptions _options;

        private readonly ConcurrentDictionary<Type, object> _services;

        public virtual ProxyData Data
        {
            get { return _data; }
        }

        public ProxyMode Mode
        {
            get { return _mode; }
        }

        public ProxyOptions Options
        {
            get { return _options; }
        }

        public void AddFilter(string pattern, string replacement)
        {
            _storageFilters.TryAdd(pattern, replacement);
        }

        public static IProxy Create(RuntimeContext context, ProxyOptions options)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            switch (options.Mode)
            {
                case ProxyMode.DataCapture:
                    return new CaptureProxy(context, options);

                case ProxyMode.DataReplay:
                    return new ReplayProxy(context, options);

                case ProxyMode.DataPassthrough:
                    return new NullProxy(context, options);

                default:
                    throw new ArgumentOutOfRangeException(nameof(options.Mode));
            }
        }

        public virtual void Initialize(string currentDirectory)
        { }

        public virtual void ReadTestData(Stream readableStream)
        { }

        public virtual void WriteTestData(Stream writableStream)
        { }

        protected static Regex BuildFilter(string pattern)
        {
            var dangerousChars = new char[] { '.', '[', ']', '(', ')', '+', '*' };
            var regexOptions = RegexOptions.Compiled
                             | RegexOptions.CultureInvariant
                             | RegexOptions.IgnoreCase;

            var buffer = new StringBuilder(pattern);

            buffer.Replace('\\', '/');

            foreach (var dchar in dangerousChars)
            {
                buffer.Replace(dchar.ToString(), "\\" + dchar);
            }

            buffer.Replace("/", @"[/\\]");

            var filterPattern = buffer.ToString();
            var filter = new Regex(filterPattern, regexOptions);

            return filter;
        }

        protected IEnumerable<IRuntimeService> EnumeratorServices()
            => _context.EnumerateServices();

        protected T GetService<T>() where T : class, IRuntimeService
            => _context.GetService<T>();

        protected void SetService<T>(T service) where T : class, IRuntimeService
        {
            _context.SetService(service);
        }

        protected virtual string NormalizePath(string path)
        {
            if (path is null || path.Length == 0)
                return string.Empty;

            string normalizedPath = NormalizePathRegex.Replace(path, @"\");

            if (normalizedPath[normalizedPath.Length - 1] == '\\')
            {
                normalizedPath = normalizedPath.Remove(normalizedPath.Length - 1, 1);
            }

            return normalizedPath;
        }

        IEnumerable<IRuntimeService> IProxy.EnumerateServices()
            => EnumeratorServices();

        T IProxy.GetService<T>()
            => GetService<T>();

        void IProxy.SetService<T>(T service)
            => SetService(service);
    }
}
