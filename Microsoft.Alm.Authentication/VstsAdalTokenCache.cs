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
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
{
    internal class VstsAdalTokenCache : IdentityModel.Clients.ActiveDirectory.TokenCache
    {
        private const string AdalCachePath = @"Microsoft\VSCommon\VSAccountManagement";
        private const string AdalCacheFile = @"AdalCache.cache";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public VstsAdalTokenCache()
        {
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directoryPath = Path.Combine(localAppDataPath, AdalCachePath);

            AfterAccess = AfterAccessNotification;
            BeforeAccess = BeforeAccessNotification;

            string filePath = Path.Combine(directoryPath, AdalCacheFile);

            _cacheFilePath = filePath;

            BeforeAccessNotification(null);
        }
        /// <summary>
        /// Constructor receiving state of the cache.
        /// </summary>
        /// <param name="state">Current state of the cache as a blob.</param>
        public VstsAdalTokenCache(byte[] state)
            : this()
        {
            throw new NotSupportedException();
        }

        private readonly string _cacheFilePath;

        private readonly object @lock = new object();

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (@lock)
            {
                if (File.Exists(_cacheFilePath) && this.HasStateChanged)
                {
                    try
                    {
                        byte[] state = this.Serialize();

                        byte[] data = ProtectedData.Protect(state, null, DataProtectionScope.CurrentUser);

                        File.WriteAllBytes(_cacheFilePath, data);

                        this.HasStateChanged = false;
                    }
                    catch (Exception exception)
                    {
                        Git.Trace.WriteLine($"! {exception.Message}");
                    }
                }
            }
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (@lock)
            {
                if (File.Exists(_cacheFilePath))
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(_cacheFilePath);

                        byte[] state = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);

                        this.Deserialize(state);
                    }
                    catch (Exception exception)
                    {
                        Git.Trace.WriteLine($"! {exception.Message}");
                    }
                }
            }
        }
    }
}
