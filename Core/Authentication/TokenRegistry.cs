using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class TokenRegistry : ITokenStore
    {
        private const string RegistryTokenKey = "Token";
        private const string RegistryTypeKey = "Type";
        private const string RegistryUrlKey = "Url";
        private const string RegistryTypeValid = "Federated";
        private const string RegistryPathFormat = @"Software\Microsoft\VSCommon\{0}\ClientServices\TokenStorage\VisualStudio";
        private static readonly string[] Versions = new[] { "14.0" };

        public TokenRegistry()
        { }

        public void DeleteToken(Uri targetUri)
        {
            throw new NotSupportedException();
        }

        public bool ReadToken(Uri targetUri, out Token token)
        {
            foreach (var key in EnumerateKeys(false))
            {
                string url;
                string type;
                string value;

                if (KeyIsValid(key, out url, out type, out value))
                {
                    Uri tokenUri = new Uri(url);
                    if (tokenUri.IsBaseOf(targetUri))
                    {
                        byte[] data = Convert.FromBase64String(value);

                        data = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);

                        value = Encoding.UTF8.GetString(data);

                        token = new Token(value, TokenType.Refresh);

                        return true;
                    }
                }
            }

            token = null;
            return false;
        }

        public void WriteToken(Uri targetUri, Token token)
        {
            throw new NotSupportedException();
        }

        private IEnumerable<RegistryKey> EnumerateKeys(bool writeable)
        {
            foreach (string version in Versions)
            {
                string registryPath = String.Format(RegistryPathFormat, version);

                RegistryKey rootKey = Registry.CurrentUser.OpenSubKey(registryPath, false);

                if (rootKey != null)
                {
                    foreach (var nodeName in rootKey.GetSubKeyNames())
                    {
                        var nodeKey = rootKey.OpenSubKey(nodeName, false);

                        if (nodeKey != null)
                        {
                            foreach (var leafName in nodeKey.GetSubKeyNames())
                            {
                                var leafKey = nodeKey.OpenSubKey(leafName, writeable);

                                if (leafKey != null)
                                {
                                    yield return leafKey;
                                }
                            }
                        }
                    }
                }
            }

            yield break;
        }

        private bool KeyIsValid(RegistryKey registryKey, out string url, out string type, out string value)
        {
            url = registryKey.GetValue(RegistryUrlKey, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as String;
            type = registryKey.GetValue(RegistryTypeKey, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as String;
            value = registryKey.GetValue(RegistryTokenKey, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as String;

            return !String.IsNullOrEmpty(url)
                && !String.IsNullOrEmpty(value)
                && String.Equals(type, RegistryTypeValid, StringComparison.OrdinalIgnoreCase)
                && Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }
    }
}
