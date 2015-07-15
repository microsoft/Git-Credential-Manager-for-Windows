using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal class VsoAdalTokenCache : Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache
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

            DirectoryInfo cacheDirectory = new DirectoryInfo(directoryPath);
            if (!cacheDirectory.Exists)
            {
                cacheDirectory.Create();
            }

            string filePath = Path.Combine(directoryPath, AdalCacheFile);

            _cacheFilePath = filePath;

            lock (@lock)
            {
                byte[] data = File.Exists(_cacheFilePath)
                            ? File.ReadAllBytes(_cacheFilePath)
                            : null;

                byte[] state = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);

                this.Deserialize(state);
            }
        }
        /// <summary>
        /// Constructor receiving state of the cache.
        /// </summary>
        /// <param name="state">Current state of the cache as a blob.</param>
        public VsoAdalTokenCache(byte[] state)
            : this()
        {
            this.Deserialize(state);
        }

        private readonly string _cacheFilePath;

        private readonly object @lock = new object();

        public void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (@lock)
            {
                if (this.HasStateChanged)
                {
                    byte[] state = this.Serialize();

                    byte[] data = ProtectedData.Protect(state, null, DataProtectionScope.CurrentUser);

                    File.WriteAllBytes(_cacheFilePath, data);

                    this.HasStateChanged = false;
                }
            }
        }

        public void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (@lock)
            {
                byte[] data = File.Exists(_cacheFilePath)
                            ? File.ReadAllBytes(_cacheFilePath)
                            : null;

                byte[] state = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);

                this.Deserialize(state);
            }
        }
    }
}
