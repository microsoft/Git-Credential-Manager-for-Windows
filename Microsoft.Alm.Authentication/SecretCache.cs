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
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    public sealed class SecretCache : Base, ICredentialStore, ITokenStore
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

        public SecretCache(RuntimeContext context, string @namespace, Secret.UriNameConversionDelegate getTargetName)
            : this(context)
        {
            if (string.IsNullOrWhiteSpace(@namespace))
                throw new ArgumentNullException(@namespace);

            _namespace = @namespace;
            _getTargetName = getTargetName ?? Secret.UriToName;
        }

        public SecretCache(RuntimeContext context, string @namespace)
            : this(context, @namespace, null)
        { }

        internal SecretCache(RuntimeContext context, ICredentialStore credentialStore)
            : this(context)
        {
            if (credentialStore == null)
                throw new ArgumentNullException(nameof(credentialStore));

            _namespace = credentialStore.Namespace;
            _getTargetName = credentialStore.UriNameConversion;
        }

        private SecretCache(RuntimeContext context)
            : base(context)
        {
            _cache = new Dictionary<string, Secret>(KeyComparer);
        }

        private Dictionary<string, Secret> _cache;
        private string _namespace;
        private Secret.UriNameConversionDelegate _getTargetName;

        public string Namespace
        {
            get { return _namespace; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public Secret.UriNameConversionDelegate UriNameConversion
        {
            get { return _getTargetName; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(UriNameConversion));

                _getTargetName = value;
            }
        }

        /// <summary>
        /// Deletes a credential from the cache.
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being deleted</param>
        public Task<bool> DeleteCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            string targetName = GetTargetName(targetUri);
            bool result = false;

            lock (_cache)
            {
                result = _cache.ContainsKey(targetName)
                    && _cache[targetName] is Credential
                    && _cache.Remove(targetName);
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Deletes a token from the cache.
        /// </summary>
        /// <param name="targetUri">The key which to find and delete the token with.</param>
        public Task<bool> DeleteToken(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            string targetName = GetTargetName(targetUri);
            bool result = false;

            lock (_cache)
            {
                result = _cache.ContainsKey(targetName)
                    && _cache[targetName] is Token
                    && _cache.Remove(targetName);
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Returns an enumerable of all the secrets contained in the store.
        /// </summary>
        public IEnumerable<NamedSecret> EnumerateSecrets()
        {
            List<NamedSecret> array = null;

            lock (_cache)
            {
                array = new List<NamedSecret>(_cache.Count);

                foreach(var kvp in _cache)
                {
                    array.Add(new NamedSecret(kvp.Key, kvp.Value));
                }
            }

            return array;
        }

        /// <summary>
        /// Reads credentials for a target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being read</param>
        /// <returns>A <see cref="Credential"/> from the store; <see langword="null"/> if failure.</returns>
        public Task<Credential> ReadCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Credential credentials = null;
            string targetName = GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName) && _cache[targetName] is Credential)
                {
                    credentials = _cache[targetName] as Credential;
                }
                else
                {
                    credentials = null;
                }
            }

            return Task.FromResult(credentials);
        }

        /// <summary>
        /// Gets a token from the cache.
        /// </summary>
        /// <param name="targetUri">The key which to find the token.</param>
        /// <returns>A <see cref="Token"/> if successful; otherwise <see langword="null"/>.</returns>
        public Task<Token> ReadToken(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Token token = null;
            string targetName = GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName) && _cache[targetName] is Token)
                {
                    token = _cache[targetName] as Token;
                }
                else
                {
                    token = null;
                }
            }

            return Task.FromResult(token);
        }

        /// <summary>
        /// Writes credentials for a target URI to the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being stored</param>
        /// <param name="credentials">The credentials to be stored</param>
        public Task<bool> WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            string targetName = GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName))
                {
                    _cache[targetName] = credentials;
                }
                else
                {
                    _cache.Add(targetName, credentials);
                }
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Writes a token to the cache.
        /// </summary>
        /// <param name="targetUri">The key which to index the token by.</param>
        /// <param name="token">The token to write to the cache.</param>
        public Task<bool> WriteToken(TargetUri targetUri, Token token)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Token.Validate(token);

            string targetName = GetTargetName(targetUri);

            lock (_cache)
            {
                if (_cache.ContainsKey(targetName))
                {
                    _cache[targetName] = token;
                }
                else
                {
                    _cache.Add(targetName, token);
                }
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Formats a TargetName string based on the TargetUri base on the format started by git-credential-winstore
        /// </summary>
        /// <param name="targetUri">Uri of the target</param>
        /// <returns>Properly formatted TargetName string</returns>
        private string GetTargetName(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            return _getTargetName(targetUri, _namespace);
        }

        public struct NamedSecret
        {
            public NamedSecret(string name, Secret secret)
            {
                _name = name;
                _secret = secret;
            }

            private readonly string _name;
            private readonly Secret _secret;

            public string Name
            {
                get { return _name; }
            }

            public Secret Secret
            {
                get { return _secret; }
            }
        }
    }
}
