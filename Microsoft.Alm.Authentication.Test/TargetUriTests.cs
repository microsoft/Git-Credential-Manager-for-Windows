using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class TargetUriTests
    {
        public static object[] UrlData
        {
            get
            {
                List<object[]> data = new List<object[]>()
                {
                    new object[] { "https://microsoft.visualstudio.com", "https://github.com", "https://bitbucket.org" },
                    new object[] { "https://my-host.com", null, "http://proxy-local:8080" },
                    new object[] { "https://random-host.com", null, null },
                    new object[] { "https://random-host.com", "https://q.random.com", null },
                    new object[] { "https://random-host.com", "https://q.random.com", "http://proxy-local:8080" },
                    new object[] { "http://random-host.com", "http://q.random.com", "https://proxy-local:8080" },
                };

                return data.ToArray();
            }
        }

        [Theory]
        [MemberData(nameof(UrlData))]
        public void TargetUri_Basics(string actualUrl, string queryUrl, string proxyUrl)
        {
            TargetUri targetUri;
            Uri actualUri;
            Uri queryUri;
            Uri proxyUri;

            actualUri = new TargetUri(actualUrl);

            targetUri = new TargetUri(actualUrl);
            Assert.NotNull(targetUri);

            Assert.Equal(actualUri.AbsolutePath, targetUri.AbsolutePath);
            Assert.Equal(actualUri.DnsSafeHost, targetUri.DnsSafeHost);
            Assert.Equal(actualUri.Host, targetUri.Host);
            Assert.Equal(actualUri.IsAbsoluteUri, targetUri.IsAbsoluteUri);
            Assert.Equal(actualUri.IsDefaultPort, targetUri.IsDefaultPort);
            Assert.Equal(actualUri.Port, targetUri.Port);
            Assert.Equal(actualUri.Scheme, targetUri.Scheme);
            Assert.Equal(actualUri.UserInfo, targetUri.TargetUriUsername);

            actualUri = actualUrl is null ? null : new Uri(actualUrl);
            queryUri = queryUrl is null ? null : new Uri(queryUrl);
            proxyUri = proxyUrl is null ? null : new Uri(proxyUrl);

            targetUri = new TargetUri(actualUrl, queryUrl, proxyUrl);
            Assert.NotNull(targetUri);

            // Since the actual Uri will substitute for a null query Uri, test the correct value.
            var uri = queryUri ?? actualUri;

            Assert.Equal(uri.AbsolutePath, targetUri.AbsolutePath);
            Assert.Equal(uri.DnsSafeHost, targetUri.DnsSafeHost);
            Assert.Equal(uri.Host, targetUri.Host);
            Assert.Equal(uri.IsAbsoluteUri, targetUri.IsAbsoluteUri);
            Assert.Equal(uri.IsDefaultPort, targetUri.IsDefaultPort);
            Assert.Equal(uri.Port, targetUri.Port);
            Assert.Equal(uri.Scheme, targetUri.Scheme);

            Assert.Equal(uri.UserInfo, targetUri.TargetUriUsername);

            Assert.Equal(actualUri, targetUri.ActualUri);
            Assert.Equal(uri, targetUri.QueryUri);
            Assert.Equal(proxyUri, targetUri.ProxyUri);

            targetUri = new TargetUri(actualUri, queryUri, proxyUri);
            Assert.NotNull(targetUri);
            Assert.Equal(uri.AbsolutePath, targetUri.AbsolutePath);
            Assert.Equal(uri.DnsSafeHost, targetUri.DnsSafeHost);
            Assert.Equal(uri.Host, targetUri.Host);
            Assert.Equal(uri.IsAbsoluteUri, targetUri.IsAbsoluteUri);
            Assert.Equal(uri.IsDefaultPort, targetUri.IsDefaultPort);
            Assert.Equal(uri.Port, targetUri.Port);
            Assert.Equal(uri.Scheme, targetUri.Scheme);

            Assert.Equal(uri.UserInfo, targetUri.TargetUriUsername);

            Assert.Equal(actualUri, targetUri.ActualUri);
            Assert.Equal(uri, targetUri.QueryUri);
            Assert.Equal(proxyUri, targetUri.ProxyUri);
        }

        [Theory]
        [MemberData(nameof(UrlData))]
        public void TargetUri_WebProxy(string actualUrl, string queryUrl, string proxyUrl)
        {
            var targetUri = new TargetUri(actualUrl, queryUrl, proxyUrl);

            var proxy = targetUri.WebProxy;
            Assert.NotNull(proxy);

            if (!(proxyUrl is null))
            {
                Assert.Equal(new Uri(proxyUrl), proxy.Address);
            }
            else
            {
                Assert.Null(proxy.Address);
            }
        }

        [Theory]
        [MemberData(nameof(UrlData))]
        public void TargetUri_HttpClientHandler(string actualUrl, string queryUrl, string proxyUrl)
        {
            var targetUri = new TargetUri(actualUrl, queryUrl, proxyUrl);

            var client = targetUri.HttpClientHandler;
            Assert.NotNull(client);

            if (!(proxyUrl is null))
            {
                Assert.True(client.UseProxy);
                Assert.NotNull(client.Proxy);

                if (client.Proxy is System.Net.WebProxy proxy)
                {
                    Assert.Equal(new Uri(proxyUrl), proxy.Address);
                }
            }
            else
            {
                Assert.Null(client.Proxy);
                Assert.False(client.UseProxy);
            }
        }
    }
}
