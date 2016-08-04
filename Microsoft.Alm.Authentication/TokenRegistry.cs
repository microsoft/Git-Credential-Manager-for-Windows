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
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// A token storage object which interacts with the current user's Visual Studio 2015 hive in
    /// the Windows Registry.
    /// </summary>
    public sealed class TokenRegistry : ITokenStore
    {
        private const string RegistryTokenKey = "Token";
        private const string RegistryTypeKey = "Type";
        private const string RegistryUrlKey = "Url";
        private const string RegistryPathFormat = @"Software\Microsoft\VSCommon\{0}\ClientServices\TokenStorage\VisualStudio\VssApp";
        private static readonly string[] Versions = new[] { "14.0" }; // only VS 2015 supported

        public TokenRegistry()
        { }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="targetUri"></param>
        public void DeleteToken(TargetUri targetUri)
        {
            // we've decided to not support registry deletes until the rules are established
            throw new NotSupportedException("Deletes from the registry are not supported by this library.");
        }
        /// <summary>
        /// Reads a token from the current user's Visual Studio hive in the Windows Registry.
        /// </summary>
        /// <param name="targetUri">Key used to select the token.</param>
        /// <param name="token">If successful, the token from the registry; otherwise `null`.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public bool ReadToken(TargetUri targetUri, out Token token)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("TokenRegistry::ReadToken");

            foreach (var key in EnumerateKeys(false))
            {
                if (key == null)
                    continue;

                string url;
                string type;
                string value;

                if (KeyIsValid(key, out url, out type, out value))
                {
                    try
                    {
                        Uri tokenUri = new Uri(url);
                        if (tokenUri.IsBaseOf(targetUri.ActualUri))
                        {
                            byte[] data = Convert.FromBase64String(value);

                            data = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);

                            value = Encoding.UTF8.GetString(data);

                            TokenType tokenType;
                            if (String.Equals(type, "Federated", StringComparison.OrdinalIgnoreCase))
                            {
                                tokenType = TokenType.Federated;
                            }
                            else
                            {
                                throw new InvalidOperationException("Unexpected token type encountered");
                            }

                            token = new Token(value, tokenType);

                            return true;
                        }
                    }
                    catch
                    {
                        Trace.WriteLine("   token read from registry was corrupt");
                    }
                }
            }

            token = null;
            return false;
        }
        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="token"></param>
        public void WriteToken(TargetUri targetUri, Token token)
        {
            // we've decided to not support registry writes until the format is standardized
            throw new NotSupportedException("Writes to the registry are not supported by this library.");
        }

        private IEnumerable<RegistryKey> EnumerateKeys(bool writeable)
        {
            Trace.WriteLine("TokenRegistry::EnumerateKeys");

            foreach (var rootKey in EnumerateRootKeys())
            {
                if (rootKey != null)
                {
                    foreach (var nodeName in rootKey.GetSubKeyNames())
                    {
                        RegistryKey nodeKey = null;
                        try
                        {
                            rootKey.OpenSubKey(nodeName, writeable);
                        }
                        catch
                        {
                            Trace.WriteLine("   failed to open subkey");
                        }

                        if (nodeKey != null)
                        {
                            yield return nodeKey;
                        }
                    }
                }
            }

            yield break;
        }

        private bool KeyIsValid(RegistryKey registryKey, out string url, out string type, out string value)
        {
            if (ReferenceEquals(registryKey, null))
                throw new ArgumentNullException(nameof(registryKey));
            if (ReferenceEquals(registryKey.Handle, null))
                throw new ArgumentNullException(nameof(registryKey.Handle));
            if (registryKey.Handle.IsInvalid)
                throw new ArgumentException(nameof(registryKey));

            url = registryKey.GetValue(RegistryUrlKey, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as String;
            type = registryKey.GetValue(RegistryTypeKey, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as String;
            value = registryKey.GetValue(RegistryTokenKey, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as String;

            return !String.IsNullOrEmpty(url)
                && !String.IsNullOrEmpty(value)
                && Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        private IEnumerable<RegistryKey> EnumerateRootKeys()
        {
            Trace.WriteLine("TokenRegistry::EnumerateRootKeys");

            foreach (string version in Versions)
            {
                RegistryKey result = null;

                try
                {
                    string registryPath = String.Format(RegistryPathFormat, version);

                    result = Registry.CurrentUser.OpenSubKey(registryPath, false);
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception, "Error");
                }

                yield return result;
            }
        }
    }
}
