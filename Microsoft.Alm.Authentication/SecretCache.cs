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
        public static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Creates a new instance of `<see cref="SecretCache"/>`.
        /// </summary>
        /// <param name="namespace">The namespace used to when reading, writing, or deleting secrets from the cache.</param>
        /// <param name="getTargetName">Delegate used to generate key names when reading, writing, or deleting secrets.</param>
        public SecretCache(RuntimeContext context, string @namespace, Secret.UriNameConversionDelegate getTargetName)
            : this(context)
        {
            if (string.IsNullOrWhiteSpace(@namespace))
                throw new ArgumentNullException(@namespace);

            _namespace = @namespace;
            _getTargetName = getTargetName ?? Secret.UriToName;
        }

        /// <summary>
        /// Creates a new instance of `<see cref="SecretCache"/>`, using the default key name generation scheme.
        /// </summary>
        /// <param name="namespace">The namespace used to when reading, writing, or deleting secrets from the cache.</param>
        public SecretCache(RuntimeContext context, string @namespace)
            : this(context, @namespace, null)
        { }

        internal SecretCache(RuntimeContext context, ICredentialStore credentialStore)
            : this(context)
        {
            if (credentialStore is null)
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
        private Secret.UriNameConversionDelegate _getTargetName;
        private readonly string _namespace;

        public string Namespace
        {
            get { return _namespace; }
        }

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

        public Task<bool> DeleteCredentials(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

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

        public Task<bool> DeleteToken(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

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

        public Task<Credential> ReadCredentials(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

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

        public Task<Token> ReadToken(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

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

        public Task<bool> WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (credentials is null)
                throw new ArgumentNullException(nameof(credentials));

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

        public Task<bool> WriteToken(TargetUri targetUri, Token token)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (token is null)
                throw new ArgumentNullException(nameof(token));

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

        private string GetTargetName(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

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

            public override bool Equals(object obj)
            {
                return (obj is NamedSecret other
                        && StringComparer.OrdinalIgnoreCase.Equals(_name, other._name)
                        && ((_secret is Token token
                                && other._secret is Token otherToken
                                && token.Equals(otherToken))
                            || (_secret is Credential credential
                                && other._secret is Credential otherCredential
                                && credential.Equals(otherCredential))))
                    || base.Equals(obj);
            }

            public override int GetHashCode()
            {
                if (_secret is Credential credential)
                    return credential.GetHashCode();

                if (_secret is Token token)
                    return token.GetHashCode();

                return base.GetHashCode();
            }

            public static bool operator ==(NamedSecret lhs, NamedSecret rhs)
                => lhs.Equals(rhs);

            public static bool operator !=(NamedSecret lhs, NamedSecret rhs)
                => !lhs.Equals(rhs);
        }
    }
}
