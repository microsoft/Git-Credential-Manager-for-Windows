using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
{
    internal class VsoAdalTokenCache : IdentityModel.Clients.ActiveDirectory.TokenCache
    {
        private const string AdalCachePath = @"Microsoft\VSCommon\VSAccountManagement";
        private const string AdalCacheFile = @"AdalCache.cache";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public VsoAdalTokenCache()
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
        public VsoAdalTokenCache(byte[] state)
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
                    Trace.WriteLine("VsoAdalTokenCache::AfterAccessNotification");

                    try
                    {
                        byte[] state = this.Serialize();

                        byte[] data = ProtectedData.Protect(state, null, DataProtectionScope.CurrentUser);

                        File.WriteAllBytes(_cacheFilePath, data);

                        this.HasStateChanged = false;
                    }
                    catch (Exception exception)
                    {
                        Trace.WriteLine(exception, "Error");
                    }
                }
            }
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (@lock)
            {
                Trace.WriteLine("VsoAdalTokenCache::BeforeAccessNotification");

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
                        Trace.WriteLine(exception, "Error");
                    }
                }
            }
        }
    }
}
