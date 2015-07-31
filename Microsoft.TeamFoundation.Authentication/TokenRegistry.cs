using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Microsoft.TeamFoundation.Authentication
{
    /// <summary>
    /// A token storage object which interacts with thecurrent user's Visual Studio hive in the 
    /// Windows Registry.
    /// </summary>
    public sealed class TokenRegistry : ITokenStore
    {
        private const string RegistryTokenKey = "Token";
        private const string RegistryTypeKey = "Type";
        private const string RegistryUrlKey = "Url";
        private const string RegistryPathFormat = @"Software\Microsoft\VSCommon\{0}\ClientServices\TokenStorage\VisualStudio\VssApp";
        private static readonly string[] Versions = new[] { "14.0" }; // only a single supported version today, latest version should be placed first

        public TokenRegistry()
        { }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="targetUri"></param>
        public void DeleteToken(Uri targetUri)
        {
            // we've decided to not support registry delets until the rules are established
            throw new NotSupportedException("Deletes from the registry are not supported by this library.");
        }
        /// <summary>
        /// Reads a token from the current user's Visual Studio hive in the Windows Registry.
        /// </summary>
        /// <param name="targetUri">Key used to select the token.</param>
        /// <param name="token">If successful, the token from the registry; otherwise `null`.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public bool ReadToken(Uri targetUri, out Token token)
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
                        if (tokenUri.IsBaseOf(targetUri))
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
        public void WriteToken(Uri targetUri, Token token)
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
            Debug.Assert(registryKey != null && !registryKey.Handle.IsInvalid, "The registryKey parameter is null or invalid.");

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
