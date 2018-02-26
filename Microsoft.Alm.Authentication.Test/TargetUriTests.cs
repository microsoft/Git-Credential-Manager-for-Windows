using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class TargetUriTests
    {
        public static object[][] UrlData
        {
            get
            {
                List<object[]> data = new List<object[]>()
                {
                    new object[] { "https://microsoft.visualstudio.com", "https://github.com" },
                    new object[] { "https://my-host.com", "http://proxy-local:8080" },
                    new object[] { "https://random-host.com", null },
                    new object[] { "https://random-host.com", "https://q.random.com" },
                    new object[] { "https://random-host.com", "http://proxy-local:8080" },
                    new object[] { "http://random-host.com", "http://q.random.com" },
                };

                return data.ToArray();
            }
        }

        [Theory]
        [MemberData(nameof(UrlData), DisableDiscoveryEnumeration = true)]
        public void TargetUri_Basics(string queryUrl, string proxyUrl)
        {
            TargetUri targetUri;
            Uri queryUri;
            Uri proxyUri;

            queryUri = new TargetUri(queryUrl);

            targetUri = new TargetUri(queryUri);
            Assert.NotNull(targetUri);

            Assert.Equal(queryUri.AbsolutePath, targetUri.AbsolutePath);
            Assert.Equal(queryUri.DnsSafeHost, targetUri.DnsSafeHost);
            Assert.Equal(queryUri.Host, targetUri.Host);
            Assert.Equal(queryUri.IsAbsoluteUri, targetUri.IsAbsoluteUri);
            Assert.Equal(queryUri.IsDefaultPort, targetUri.IsDefaultPort);
            Assert.Equal(queryUri.Port, targetUri.Port);
            Assert.Equal(queryUri.Scheme, targetUri.Scheme);
            Assert.Equal(queryUri.UserInfo, targetUri.TargetUriUsername);

            queryUri = queryUrl is null ? null : new Uri(queryUrl);
            proxyUri = proxyUrl is null ? null : new Uri(proxyUrl);

            targetUri = new TargetUri(queryUrl, proxyUrl);
            Assert.NotNull(targetUri);

            // Since the actual Uri will substitute for a null query Uri, test the correct value.
            var uri = queryUri;

            Assert.Equal(uri.AbsolutePath, targetUri.AbsolutePath);
            Assert.Equal(uri.DnsSafeHost, targetUri.DnsSafeHost);
            Assert.Equal(uri.Host, targetUri.Host);
            Assert.Equal(uri.IsAbsoluteUri, targetUri.IsAbsoluteUri);
            Assert.Equal(uri.IsDefaultPort, targetUri.IsDefaultPort);
            Assert.Equal(uri.Port, targetUri.Port);
            Assert.Equal(uri.Scheme, targetUri.Scheme);

            Assert.Equal(uri.UserInfo, targetUri.TargetUriUsername);

            Assert.Equal(uri, targetUri.QueryUri);
            Assert.Equal(proxyUri, targetUri.ProxyUri);

            targetUri = new TargetUri(queryUri, proxyUri);
            Assert.NotNull(targetUri);
            Assert.Equal(uri.AbsolutePath, targetUri.AbsolutePath);
            Assert.Equal(uri.DnsSafeHost, targetUri.DnsSafeHost);
            Assert.Equal(uri.Host, targetUri.Host);
            Assert.Equal(uri.IsAbsoluteUri, targetUri.IsAbsoluteUri);
            Assert.Equal(uri.IsDefaultPort, targetUri.IsDefaultPort);
            Assert.Equal(uri.Port, targetUri.Port);
            Assert.Equal(uri.Scheme, targetUri.Scheme);

            Assert.Equal(uri.UserInfo, targetUri.TargetUriUsername);

            Assert.Equal(queryUri, targetUri.QueryUri);
            Assert.Equal(uri, targetUri.QueryUri);
            Assert.Equal(proxyUri, targetUri.ProxyUri);
        }
    }
}
