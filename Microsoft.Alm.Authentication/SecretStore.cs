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

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Interface to secure secrets storage which indexes values by target and utilizes the operating
    /// system keychain / secrets vault.
    /// </summary>
    public sealed class SecretStore : BaseSecureStore, ICredentialStore, ITokenStore
    {
        /// <summary>
        /// Creates a new <see cref="SecretStore"/> backed by the operating system keychain / secrets vault.
        /// </summary>
        /// <param name="namespace">The namespace of the secrets written and read by this store.</param>
        /// <param name="credentialCache">
        /// Write-through, read-first cache. Default cache is used if a custom cache is not provided.
        /// </param>
        /// <param name="tokenCache">
        /// Write-through, read-first cache. Default cache is used if a custom cache is not provided.
        /// </param>
        public SecretStore(string @namespace, ICredentialStore credentialCache, ITokenStore tokenCache, Secret.UriNameConversion getTargetName)
        {
            if (string.IsNullOrWhiteSpace(@namespace))
                throw new ArgumentNullException(nameof(@namespace));
            if (@namespace.IndexOfAny(IllegalCharacters) != -1)
                throw new ArgumentException("Namespace contains illegal characters.", nameof(@namespace));

            _getTargetName = getTargetName ?? Secret.UriToName;

            _namespace = @namespace;
            _credentialCache = credentialCache ?? new SecretCache(@namespace, _getTargetName);
            _tokenCache = tokenCache ?? new SecretCache(@namespace, _getTargetName);
        }

        public SecretStore(string @namespace, Secret.UriNameConversion getTargetName)
            : this(@namespace, null, null, getTargetName)
        { }

        public SecretStore(string @namespace)
            : this(@namespace, null, null, null)
        { }

        public string Namespace
        {
            get { return _namespace; }
        }

        public Secret.UriNameConversion UriNameConversion
        {
            get { return _getTargetName; }
        }

        private string _namespace;
        private ICredentialStore _credentialCache;
        private ITokenStore _tokenCache;

        private readonly Secret.UriNameConversion _getTargetName;

        /// <summary>
        /// Deletes credentials for target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being deleted</param>
        public bool DeleteCredentials(TargetUri targetUri)
        {
            ValidateTargetUri(targetUri);

            string targetName = GetTargetName(targetUri);

            return Delete(targetName)
                && _credentialCache.DeleteCredentials(targetUri);
        }

        /// <summary>
        /// Deletes the token for target URI from the token store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which the token is being deleted</param>
        public bool DeleteToken(TargetUri targetUri)
        {
            ValidateTargetUri(targetUri);

            string targetName = GetTargetName(targetUri);

            return Delete(targetName)
                && _tokenCache.DeleteToken(targetUri);
        }

        /// <summary>
        /// Purges all credentials from the store.
        /// </summary>
        public void PurgeCredentials()
        {
            PurgeCredentials(_namespace);
        }

        /// <summary>
        /// Reads credentials for a target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being read</param>
        /// <param name="credentials"></param>
        /// <returns>A <see cref="Credential"/> from the store is successful; otherwise <see langword="null"/>.</returns>
        public Credential ReadCredentials(TargetUri targetUri)
        {
            ValidateTargetUri(targetUri);

            string targetName = GetTargetName(targetUri);

            return _credentialCache.ReadCredentials(targetUri)
                ?? ReadCredentials(targetName);
        }

        /// <summary>
        /// Reads a token for a target URI from the token store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which a token is being read</param>
        /// <returns>A <see cref="Token"/> from the store is successful; otherwise <see langword="null"/>.</returns>
        public Token ReadToken(TargetUri targetUri)
        {
            ValidateTargetUri(targetUri);

            string targetName = GetTargetName(targetUri);

            return _tokenCache.ReadToken(targetUri)
                ?? ReadToken(targetName);
        }

        /// <summary>
        /// Writes credentials for a target URI to the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being stored</param>
        /// <param name="credentials">The credentials to be stored</param>
        public bool WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            string targetName = GetTargetName(targetUri);

            return WriteCredential(targetName, credentials)
                && _credentialCache.WriteCredentials(targetUri, credentials);
        }

        /// <summary>
        /// Writes a token for a target URI to the token store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which a token is being stored</param>
        /// <param name="token">The token to be stored</param>
        public bool WriteToken(TargetUri targetUri, Token token)
        {
            ValidateTargetUri(targetUri);
            Token.Validate(token);

            string targetName = GetTargetName(targetUri);

            return WriteToken(targetName, token)
                && _tokenCache.WriteToken(targetUri, token);
        }

        /// <summary>
        /// Formats a TargetName string based on the TargetUri base on the format started by git-credential-winstore
        /// </summary>
        /// <param name="targetUri">Uri of the target</param>
        /// <returns>Properly formatted TargetName string</returns>
        protected override string GetTargetName(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            return _getTargetName(targetUri, _namespace);
        }
    }
}
