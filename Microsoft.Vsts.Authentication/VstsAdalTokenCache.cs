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
using System.Security.Cryptography;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
{
    internal class VstsAdalTokenCache : IdentityModel.Clients.ActiveDirectory.TokenCache
    {
        private static readonly IReadOnlyList<IReadOnlyList<string>> AdalCachePaths = new string[][]
        {
            new [] { @".IdentityService", @"IdentityServiceAdalCache.cache", },          // VS2017 ADAL v3 cache
            new [] { @"Microsoft\VSCommon\VSAccountManagement", @"AdalCache.cache", },   // VS2015 ADAL v2 cache
            new [] { @"Microsoft\VSCommon\VSAccountManagement", @"AdalCacheV2.cache", }, // VS2017 ADAL v2 cache
        };

        /// <summary>
        /// Default constructor.
        /// </summary>
        public VstsAdalTokenCache(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _context = context;

            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            AfterAccess = AfterAccessNotification;
            BeforeAccess = BeforeAccessNotification;

            for (int i = 0; i < AdalCachePaths.Count; i += 1)
            {
                string directoryPath = Path.Combine(localAppDataPath, AdalCachePaths[i][0]);
                string filePath = Path.Combine(directoryPath, AdalCachePaths[i][1]);

                if (File.Exists(filePath))
                {
                    _cacheFilePath = filePath;
                    break;
                }
            }

            BeforeAccessNotification(null);
        }

        private readonly string _cacheFilePath;
        private readonly RuntimeContext _context;
        private readonly object _syncpoint = new object();

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (_syncpoint)
            {
                if (File.Exists(_cacheFilePath) && HasStateChanged)
                {
                    try
                    {
                        byte[] state = Serialize();

                        byte[] data = ProtectedData.Protect(state, null, DataProtectionScope.CurrentUser);

                        File.WriteAllBytes(_cacheFilePath, data);

                        HasStateChanged = false;
                    }
                    catch (Exception exception)
                    {
                        _context.Trace.WriteLine($"error: {nameof(VstsAdalTokenCache)} \"{_cacheFilePath}\": {exception.Message}");
                    }
                }
            }
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (_syncpoint)
            {
                if (File.Exists(_cacheFilePath))
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(_cacheFilePath);

                        byte[] state = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);

                        Deserialize(state);
                    }
                    catch (Exception exception)
                    {
                        _context.Trace.WriteLine($"error: {nameof(VstsAdalTokenCache)} \"{_cacheFilePath}\": {exception.Message}");
                    }
                }
            }
        }
    }
}
