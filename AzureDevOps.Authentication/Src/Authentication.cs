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
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;

using static System.StringComparer;
using static AzureDevOps.Authentication.Authority;

namespace AzureDevOps.Authentication
{
    /// <summary>
    /// Base functionality for performing authentication operations against Azure DevOps.
    /// </summary>
    public abstract class Authentication : BaseAuthentication
    {
        public const string DefaultResource = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string DefaultClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        protected const string AdalRefreshPrefix = "ada";

        internal const string AzureBaseUrlHost = AzureDevOps.Authentication.Authority.AzureBaseUrlHost;
        internal const string VstsBaseUrlHost = AzureDevOps.Authentication.Authority.VstsBaseUrlHost;

        private const char CachePairSeperator = '=';
        private const char CachePairTerminator = '\0';
        private const string CachePathDirectory = "GitCredentialManager";
        private const string CachePathFileName = "tenant.cache";

        protected Authentication(
            RuntimeContext context,
            TokenScope tokenScope,
            ICredentialStore personalAccessTokenStore)
            : base(context)
        {
            if (tokenScope is null)
                throw new ArgumentNullException(nameof(tokenScope));
            if (personalAccessTokenStore is null)
                throw new ArgumentNullException(nameof(personalAccessTokenStore));

            ClientId = DefaultClientId;
            Resource = DefaultResource;
            TokenScope = tokenScope;
            PersonalAccessTokenStore = personalAccessTokenStore;
            Authority = new Authority(context);
        }

        internal Authentication(
            RuntimeContext context,
            ICredentialStore personalAccessTokenStore,
            ITokenStore ideTokenCache,
            IAuthority authority)
            : this(context, TokenScope.ProfileRead, personalAccessTokenStore)
        {
            if (ideTokenCache is null)
                throw new ArgumentNullException(nameof(ideTokenCache));
            if (authority is null)
                throw new ArgumentNullException(nameof(authority));

            IdeTokenCache = ideTokenCache;
            Authority = authority;
        }

        /// <summary>
        /// The application client identity by which access will be requested.
        /// </summary>
        public readonly string ClientId;

        /// <summary>
        /// The Azure resource for which access will be requested.
        /// </summary>
        public readonly string Resource;

        /// <summary>
        /// The desired scope of the authentication token to be requested.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public readonly TokenScope TokenScope;

        /// <summary>
        /// Gets the expected URI name conversion delegate for this authentication type.
        /// </summary>
        public static Secret.UriNameConversionDelegate UriNameConversion
        {
            get { return GetSecretKey; }
        }

        internal ITokenStore IdeTokenCache { get; private set; }

        internal ICredentialStore PersonalAccessTokenStore { get; set; }

        internal IAuthority Authority { get; set; }

        internal Guid TenantId { get; set; }

        /// <summary>
        /// Deletes a `<see cref="Credential"/>` from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        public override async Task<bool> DeleteCredentials(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            await PersonalAccessTokenStore.DeleteCredentials(targetUri);

            // Remove any related entries from the tenant cache because tenant change
            // could the be source of the invalidation, and not purging the cache will
            // trap the user in a limbo state of invalid credentials.

            // Deserialize the cache and remove any matching entry.
            string tenantUrl = GetTargetUrl(targetUri, keepUsername: false);
            var cache = await DeserializeTenantCache(Context);

            // Attempt to remove the URL entry, if successful serialize the cache.
            if (cache.Remove(tenantUrl))
            {
                await SerializeTenantCache(Context, cache);
            }

            return true;
        }

        /// <summary>
        /// Detects the backing authority of the end-point.
        /// <para/>
        /// Returns `<see langword="true"/>` if the authority is Azure DevOps, along with the tenant identity; `<see langword="false"/>` otherwise.
        /// </summary>
        /// <param name="targetUri">The resource which the authority protects.</param>
        /// <param name="tenantId">The identity of the authority tenant; `<see cref="Guid.Empty"/>` otherwise.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0")]
        public static async Task<Guid?> DetectAuthority(RuntimeContext context, TargetUri targetUri)
        {
            const int GuidStringLength = 36;
            const string XvssResourceTenantHeader = "X-VSS-ResourceTenant";

            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            if (IsAzureDevOpsUrl(targetUri))
            {
                // Assume Azure DevOps using Azure "common tenant" (empty GUID).
                var tenantId = Guid.Empty;

                // Compose the request Uri, by default it is the target Uri.
                var requestUri = targetUri;

                // Override the request Uri, when actual Uri exists, with actual Uri.
                if (targetUri.ActualUri != null)
                {
                    requestUri = targetUri.CreateWith(queryUri: targetUri.ActualUri);
                }

                // If the protocol (aka scheme) being used isn't HTTP based, there's no point in
                // querying the server, so skip that work.
                if (OrdinalIgnoreCase.Equals(requestUri.Scheme, Uri.UriSchemeHttp)
                    || OrdinalIgnoreCase.Equals(requestUri.Scheme, Uri.UriSchemeHttps))
                {
                    var requestUrl = GetTargetUrl(requestUri, false);

                    // Read the cache from disk.
                    var cache = await DeserializeTenantCache(context);

                    // Check the cache for an existing value.
                    if (cache.TryGetValue(requestUrl, out tenantId))
                    {
                        context.Trace.WriteLine($"'{requestUrl}' is Azure DevOps, tenant resource is {{{tenantId.ToString("N")}}}.");

                        return tenantId;
                    }

                    // Use the properly formatted URL
                    requestUri = requestUri.CreateWith(queryUrl: requestUrl);

                    var options = new NetworkRequestOptions(false)
                    {
                        Flags = NetworkRequestOptionFlags.UseProxy,
                        Timeout = TimeSpan.FromMilliseconds(Global.RequestTimeout),
                    };

                    try
                    {
                        // Query the host use the response headers to determine if the host is Azure DevOps or not.
                        using (var response = await context.Network.HttpHeadAsync(requestUri, options))
                        {
                            if (response.Headers != null)
                            {
                                // If the "X-VSS-ResourceTenant" was returned, then it is Azure DevOps and we'll need it's value.
                                if (response.Headers.TryGetValues(XvssResourceTenantHeader, out IEnumerable<string> values))
                                {
                                    context.Trace.WriteLine($"detected '{requestUrl}' as Azure DevOps from GET response.");

                                    // The "Www-Authenticate" is a more reliable header, because it indicates the 
                                    // authentication scheme that should be used to access the requested entity.
                                    if (response.Headers.WwwAuthenticate != null)
                                    {
                                        foreach (var header in response.Headers.WwwAuthenticate)
                                        {
                                            const string AuthorizationUriPrefix = "authorization_uri=";

                                            var value = header.Parameter;

                                            if (value?.Length >= AuthorizationUriPrefix.Length + AuthorityHostUrlBase.Length + GuidStringLength)
                                            {
                                                // The header parameter will look something like "authorization_uri=https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47"
                                                // and all we want is the portion after the '=' and before the last '/'.
                                                int index1 = value.IndexOf('=', AuthorizationUriPrefix.Length - 1);
                                                int index2 = value.LastIndexOf('/');

                                                // Parse the header value if the necessary characters exist...
                                                if (index1 > 0 && index2 > index1)
                                                {
                                                    var authorityUrl = value.Substring(index1 + 1, index2 - index1 - 1);
                                                    var guidString = value.Substring(index2 + 1, GuidStringLength);

                                                    // If the authority URL is as expected, attempt to parse the tenant resource identity.
                                                    if (OrdinalIgnoreCase.Equals(authorityUrl, AuthorityHostUrlBase)
                                                        && Guid.TryParse(guidString, out tenantId))
                                                    {
                                                        // Update the cache.
                                                        cache[requestUrl] = tenantId;

                                                        // Write the cache to disk.
                                                        await SerializeTenantCache(context, cache);

                                                        // Since we found a value, break the loop (likely a loop of one item anyways).
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Since there wasn't a "Www-Authenticate" header returned
                                        // iterate through the values, taking the first non-zero value.
                                        foreach (string value in values)
                                        {
                                            // Try to find a value for the resource-tenant identity.
                                            // Given that some projects will return multiple tenant identities, 
                                            if (!string.IsNullOrWhiteSpace(value)
                                                && Guid.TryParse(value, out tenantId))
                                            {
                                                // Update the cache.
                                                cache[requestUrl] = tenantId;

                                                // Write the cache to disk.
                                                await SerializeTenantCache(context, cache);

                                                // Break the loop if a non-zero value has been detected.
                                                if (tenantId != Guid.Empty)
                                                {
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    context.Trace.WriteLine($"tenant resource for '{requestUrl}' is {{{tenantId.ToString("N")}}}.");

                                    // Return the tenant identity to the caller because this is Azure DevOps.
                                    return tenantId;
                                }
                            }
                            else
                            {
                                context.Trace.WriteLine($"unable to get response from '{requestUri}' [{(int)response.StatusCode} {response.StatusCode}].");
                            }
                        }
                    }
                    catch (HttpRequestException exception)
                    {
                        context.Trace.WriteLine($"unable to get response from '{requestUri}', an error occurred before the server could respond.");
                        context.Trace.WriteException(exception);
                    }
                }
                else
                {
                    context.Trace.WriteLine($"detected non-http(s) based protocol: '{requestUri.Scheme}'.");
                }
            }

            // Fallback to basic authentication.
            return null;
        }

        /// <summary>
        /// Creates a new authentication broker based for the specified resource.
        /// <para/>
        /// Returns `<see langword="true"/>` if an authority could be determined; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">The resource for which authentication is being requested.</param>
        /// <param name="scope">The scope of the access being requested.</param>
        /// <param name="personalAccessTokenStore">Storage container for personal access token secrets.</param>
        public static async Task<BaseAuthentication> GetAuthentication(
            RuntimeContext context,
            TargetUri targetUri,
            TokenScope scope,
            ICredentialStore personalAccessTokenStore)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (scope is null)
                throw new ArgumentNullException(nameof(scope));
            if (personalAccessTokenStore is null)
                throw new ArgumentNullException(nameof(personalAccessTokenStore));

            Guid? result = await DetectAuthority(context, targetUri);

            if (!result.HasValue)
                return null;

            // Query for the tenant's identity
            Guid tenantId = result.Value;
            BaseAuthentication authentication = null;

            // empty identity is MSA, anything else is AAD
            if (tenantId == Guid.Empty)
            {
                context.Trace.WriteLine("MSA authority detected.");
                authentication = new MsaAuthentication(context, scope, personalAccessTokenStore);
            }
            else
            {
                context.Trace.WriteLine($"AAD authority for tenant '{tenantId.ToString("N")}' detected.");
                authentication = new AadAuthentication(context, tenantId, scope, personalAccessTokenStore);
                (authentication as AadAuthentication).TenantId = tenantId;
            }

            return authentication;
        }

        /// <summary>
        /// Gets a <see cref="Credential"/> from the storage used by the authentication object.
        /// <para/>
        /// Returns a `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        public override async Task<Credential> GetCredentials(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            Credential credentials = null;

            try
            {
                if ((credentials = await PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
                {
                    Trace.WriteLine($"credentials for '{targetUri}' found.");
                }
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);

                Trace.WriteLine($"failed to read credentials from the secure store: {exception.GetType().Name}.");
            }

            return credentials;
        }

        /// <summary>
        /// Validates that a set of credentials grants access to the target resource.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">The target resource to validate against.</param>
        /// <param name="credentials">The credentials to validate.</param>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            return credentials != null
                && await Authority.ValidateCredentials(targetUri, credentials);
        }

        /// <summary>
        /// Generates a "personal access token" or service specific, usage restricted access token.
        /// <para/>
        /// Returns a "personal access token" for the user if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The target resource for which to acquire the personal access token for.</param>
        /// <param name="accessToken">Azure Directory access token with privileges to grant access to the target resource.</param>
        /// <param name="options">Set of options related to generation of personal access tokens.</param>
        protected async Task<Credential> GeneratePersonalAccessToken(
            TargetUri targetUri,
            Token accessToken,
            PersonalAccessTokenOptions options)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (accessToken is null)
                throw new ArgumentNullException(nameof(accessToken));

            TokenScope requestedScope = TokenScope;

            if (options.TokenScope != null)
            {
                // Take the intersection of the authority scope and the requested scope
                requestedScope &= options.TokenScope;

                // If the result of the intersection is none, then fail
                if (string.IsNullOrWhiteSpace(requestedScope.Value))
                    throw new InvalidOperationException("Invalid scope requested. Requested scope would result in no access privileges.");
            }

            Credential credential = null;

            Token personalAccessToken;
            if ((personalAccessToken = await Authority.GeneratePersonalAccessToken(targetUri, accessToken, requestedScope, options.RequireCompactToken, options.TokenDuration)) != null)
            {
                credential = (Credential)personalAccessToken;

                Trace.WriteLine($"personal access token created for '{targetUri}'.");

                try
                {
                    await PersonalAccessTokenStore.WriteCredentials(targetUri, credential);
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception);

                    Trace.WriteLine($"failed to write credentials to the secure store: {exception.GetType().Name}.");
                }
            }

            return credential;
        }

        /// <summary>
        /// Generates a "personal access token" or service specific, usage restricted access token.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; `<see langword="false"/>` otherwise.
        /// </summary>
        /// <param name="targetUri">The target resource for which to acquire the personal access token for.</param>
        /// <param name="accessToken">Azure Directory access token with privileges to grant access to the target resource.</param>
        /// <param name="requestCompactToken">Generates a compact token if `<see langword="true"/>`; generates a self describing token if `<see langword="false"/>`.</param>
        protected async Task<Credential> GeneratePersonalAccessToken(
            TargetUri targetUri,
            Token accessToken,
            bool requestCompactToken)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (accessToken is null)
                throw new ArgumentNullException(nameof(accessToken));

            Credential credential = null;

            Token personalAccessToken;
            if ((personalAccessToken = await Authority.GeneratePersonalAccessToken(targetUri, accessToken, TokenScope, requestCompactToken)) != null)
            {
                credential = (Credential)personalAccessToken;

                Trace.WriteLine($"personal access token created for '{targetUri}'.");

                try
                {
                    await PersonalAccessTokenStore.WriteCredentials(targetUri, credential);
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception);

                    Trace.WriteLine($"failed to write credentials to the secure store.");
                    Trace.WriteException(exception);
                }
            }

            return credential;
        }

        internal static string GetSecretKey(TargetUri targetUri, string prefix)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(prefix);

            // When the full path is specified, there's no reason to assume the path; otherwise attempt to
            // detect the actual target path information.
            string targetUrl = (IsAzureDevOpsUrl(targetUri) && !targetUri.HasPath)
                ? GetTargetUrl(targetUri, true)
                : targetUri.ToString(true, true, true);

            targetUrl = targetUrl.TrimEnd('/', '\\');

            return $"{prefix}:{targetUrl}";
        }

        private static async Task<Dictionary<string, Guid>> DeserializeTenantCache(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            var encoding = new UTF8Encoding(false);
            var path = GetCachePath(context);

            string data = null;
            var cache = new Dictionary<string, Guid>(OrdinalIgnoreCase);
            Exception exception = null;

            // Attempt up to five times to read from the cache
            for (int i = 0; i < 5; i += 1)
            {
                try
                {
                    // Just open the file from disk, the tenant identities are not secret and
                    // therefore safely left as unencrypted plain text.
                    using (var stream = context.Storage.FileOpen(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read))
                    using (var inflate = new GZipStream(stream, CompressionMode.Decompress))
                    using (var reader = new StreamReader(inflate, encoding))
                    {
                        data = await reader.ReadToEndAsync();
                        break;
                    }
                }
                catch when (i < 5)
                {
                    // Sleep the thread, and wait before trying again using progressive back off
                    System.Threading.Thread.Sleep(i + 1 * 100);
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }

            if (exception != null)
            {
                System.Diagnostics.Debug.WriteLine(exception);

                context.Trace.WriteLine($"failed to deserialize tenant cache.");
                context.Trace.WriteException(exception);

                return cache;
            }

            // Parse the inflated data
            if (data.Length > 0)
            {
                int last = 0;
                int next = -1;

                while ((next = data.IndexOf(CachePairTerminator, last)) > 0)
                {
                    int idx = data.IndexOf(CachePairSeperator, last, next - last);
                    if (idx > 0)
                    {
                        string key = data.Substring(last, idx - last);
                        string val = data.Substring(idx + 1, next - idx - 1);

                        if (Guid.TryParse(val, out Guid id))
                        {
                            cache[key] = id;
                        }

                        last = next + 1;
                    }
                }
            }

            return cache;
        }

        private static string GetCachePath(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            string path = context.Settings.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = Path.Combine(path, CachePathDirectory);

            // Create the directory if necessary
            if (!context.Storage.DirectoryExists(path))
            {
                context.Storage.CreateDirectory(path);
            }

            // Append the file name to the path
            path = Path.Combine(path, CachePathFileName);

            return path;
        }

        private static async Task SerializeTenantCache(RuntimeContext context, Dictionary<string, Guid> cache)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (cache is null)
                throw new ArgumentNullException(nameof(cache));

            var encoding = new UTF8Encoding(false);
            string path = GetCachePath(context);

            var builder = new StringBuilder();
            Exception exception = null;

            // Write each key/value pair as key=value\0
            foreach (var pair in cache)
            {
                builder.Append(pair.Key)
                       .Append('=')
                       .Append(pair.Value.ToString())
                       .Append('\0');
            }

            string data = builder.ToString();

            // Attempt up to five times to write to the cache
            for (int i = 0; i < 5; i += 1)
            {
                try
                {
                    // Just open the file from disk, the tenant identities are not secret and
                    // therefore safely left as unencrypted plain text.
                    using (var stream = context.Storage.FileOpen(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    using (var deflate = new GZipStream(stream, CompressionMode.Compress))
                    using (var writer = new StreamWriter(deflate, encoding))
                    {
                        await writer.WriteAsync(data);
                        break;
                    }
                }
                catch when (i < 5)
                {
                    // Sleep the thread, and wait before trying again using progressive back off
                    System.Threading.Thread.Sleep(i + 1 * 100);
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }

            if (exception != null)
            {
                System.Diagnostics.Debug.WriteLine(exception);

                context.Trace.WriteLine($"failed to serialize tenant cache.");
                context.Trace.WriteException(exception);
            }
        }
    }
}
