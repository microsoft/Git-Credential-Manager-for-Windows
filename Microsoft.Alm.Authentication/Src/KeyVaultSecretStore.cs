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

using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Helper;

namespace Microsoft.Alm.Authentication
{
    public class KeyVaultSecretStore : ICredentialStore
    {
        private readonly RuntimeContext _context;
        private Secret.UriNameConversionDelegate _getTargetName;
        private readonly string _namespace;
        private readonly ICredentialStore _credentialCache;

        protected Git.ITrace Trace
            => _context.Trace;

        public KeyVaultSecretStore(RuntimeContext context,
            string @namespace,
            string keyVaultUrl,
            bool? useMsi,
            string certAuthStoreType,
            string certAuthThumbprint,
            string certAuthClientId) :
                this (context, @namespace, null, keyVaultUrl, useMsi, certAuthStoreType, certAuthThumbprint, certAuthClientId, null)
        { }

        public KeyVaultSecretStore(RuntimeContext context, 
            string @namespace,
            ICredentialStore credentialCache,
            string keyVaultUrl, 
            bool? useMsi,
            string certAuthStoreType, 
            string certAuthThumbprint, 
            string certAuthClientId, 
            Secret.UriNameConversionDelegate getTargetName)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            _context = context;

            if (@namespace is null)
                throw new ArgumentNullException(nameof(@namespace));

            if (@namespace.IndexOfAny(BaseSecureStore.IllegalCharacters) != -1)
            {
                var inner = new FormatException("Namespace contains illegal characters.");
                throw new ArgumentException(inner.Message, nameof(@namespace), inner);
            }

            _getTargetName = getTargetName ?? Secret.UriToName;

            _namespace = @namespace;
            _credentialCache = credentialCache ?? new SecretCache(context, @namespace, _getTargetName);
            this._getTargetName = getTargetName;

            KeyVaultHelper.Config config = new KeyVaultHelper.Config()
            {
                KeyVaultUrl = keyVaultUrl,
                UseMsi = useMsi,
                CertificateThumbprint = certAuthThumbprint,
                CertificateStoreType = certAuthStoreType,
                ClientId = certAuthClientId
            };

            KeyVaultHelper.Configure(config);
        }
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

        public async Task<bool> DeleteCredentials(TargetUri targetUri)
        {
#if DEBUG
            Trace.WriteLine($"targetUri: '{targetUri}': Key: '{GetKeyVaultKey(targetUri)}'");
#endif

            if (targetUri is null || string.IsNullOrEmpty(targetUri.Host))
                throw new ArgumentNullException(nameof(targetUri));

            string secret = null;
            try
            {
                secret = await KeyVaultHelper.KeyVault.DeleteSecretAsync(GetKeyVaultKey(targetUri));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception deleting the secret from KeyVault:" + ex.Message);
            }

            return string.IsNullOrEmpty(secret) 
                && await _credentialCache.DeleteCredentials(targetUri);
        }

        public async Task<Credential> ReadCredentials(TargetUri targetUri)
        {
#if DEBUG
            Trace.WriteLine($"targetUri: '{targetUri}': Key: '{GetKeyVaultKey(targetUri)}'");
#endif
            if (targetUri is null || string.IsNullOrEmpty(targetUri.Host))
                throw new ArgumentNullException(nameof(targetUri));

            return await _credentialCache.ReadCredentials(targetUri)
            ?? await this.ReadKeyVaultCredentials(targetUri);
        }

        public async Task<bool> WriteCredentials(TargetUri targetUri, Credential credentials)
        {
#if DEBUG
            Trace.WriteLine($"targetUri: '{targetUri}', userName: '{credentials?.Username}' PAT: '{credentials?.Password}',  : Key: '{GetKeyVaultKey(targetUri)}'");
#endif
            if (targetUri is null || string.IsNullOrEmpty (targetUri.Host))
                throw new ArgumentNullException(nameof(targetUri));

            if (credentials is null)
                throw new ArgumentNullException(nameof(credentials));
            return await WriteKeyVaultCredentials(targetUri, credentials)
                && await _credentialCache.WriteCredentials(targetUri, credentials);
        }

        private async Task<Credential> ReadKeyVaultCredentials(TargetUri targetUri)
        {
            string secret = null;
            try
            {
                secret = await KeyVaultHelper.KeyVault.GetSecretAsync(GetKeyVaultKey(targetUri));
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception getting the secret from KeyVault:" + ex.Message);
            }

            if (string.IsNullOrEmpty(secret))
            {
                return null;
            }

            // parse secret from JSon
            try
            {
                Credential credential = JsonConvert.DeserializeObject<Credential>(secret);
                return credential;
            }
            catch (JsonException)
            {
                Trace.WriteLine("Keyvault secret doesn't contain Json value, returning as is");
                return new Credential("PersonalAccessToken", secret);
            }
        }

        private async Task<bool> WriteKeyVaultCredentials(TargetUri targetUri, Credential credentials)
        {
            string secret = "{ \"Username\" : \"" + credentials.Username + "\", \"Password\" : \"" + credentials.Password + "\", \"Message\" : \"\" }";
            try
            {
                await KeyVaultHelper.KeyVault.SetSecretAsync(GetKeyVaultKey(targetUri), secret);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception writing a secret to KeyVault:" + ex.Message);
            }
            return false;
        }

        private static string GetKeyVaultKey (TargetUri targetUri)
        {
            string path = null;

            if (targetUri.HasPath && !string.IsNullOrEmpty(targetUri.AbsolutePath))
                path = targetUri.Host + targetUri.AbsolutePath;
            else if (targetUri.ContainsUserInfo && !string.IsNullOrEmpty(targetUri.UserInfo))
                path = targetUri.UserInfo + "-" + targetUri.Host;
            else
                path = targetUri.Host;

            string key = path.Replace('.', '-').Replace('/', '-').Replace(' ', '-');
            return key;
        }

    }
}