using System;
using System.Diagnostics;
using System.Net;

namespace Microsoft.TeamFoundation.Authentication
{
    /// <summary>
    /// Base authentication mechanisms for setting, retrieving, and deleting stored credentials.
    /// </summary>
    public abstract class BaseAuthentication : IAuthentication
    {
        /// <summary>
        /// Deletes a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        public abstract void DeleteCredentials(Uri targetUri);
        /// <summary>
        /// Gets a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        /// <param name="credentials">
        /// If successful a <see cref="Credential"/> object from the authentication object, 
        /// authority or storage; otherwise `null`.
        /// </param>
        /// <returns>True if successful; otherwise false.</returns>
        public abstract bool GetCredentials(Uri targetUri, out Credential credentials);
        /// <summary>
        /// Sets a <see cref="Credential"/> in the storage used by the authentication object.otr
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        /// <param name="credentials">The value to be stored.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public abstract bool SetCredentials(Uri targetUri, Credential credentials);

        /// <summary>
        /// Detects the backing authority of the end-point.
        /// </summary>
        /// <param name="targetUri">The resource which the authority protects.</param>
        /// <param name="tenantId">The identity of the authority tenant; <see cref="Guid.Empty"/> otherwise.</param>
        /// <returns>True is the authority is Visual Studio Online; False otherwise</returns>
        public static bool DetectAuthority(Uri targetUri, out Guid tenantId)
        {
            const string VsoBaseUrlHost = "visualstudio.com";
            const string VsoResourceTenantHeader = "X-VSS-ResourceTenant";

            Trace.WriteLine("BaseAuthentication::DetectTenant");

            tenantId = Guid.Empty;

            if (targetUri.DnsSafeHost.EndsWith(VsoBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                Trace.WriteLine("   detected visualstudio.com, checking AAD vs MSA");

                string tenant = null;
                WebResponse response;

                try
                {
                    // build a request that we expect to fail, do not allow redirect to sign in url
                    var request = WebRequest.CreateHttp(targetUri);
                    request.UserAgent = Global.GetUserAgent();
                    request.Method = "HEAD";
                    request.AllowAutoRedirect = false;
                    // get the response from the server
                    response = request.GetResponse();
                }
                catch (WebException exception)
                {
                    response = exception.Response;
                }

                // if the response exists and we have headers, parse them
                if (response != null && response.SupportsHeaders)
                {
                    Trace.WriteLine("   server has responded");

                    // find the VSO resource tenant entry
                    tenant = response.Headers[VsoResourceTenantHeader];

                    return !String.IsNullOrWhiteSpace(tenant) 
                        && Guid.TryParse(tenant, out tenantId);
                }
            }

            Trace.WriteLine("   failed detection");

            // if all else fails, fallback to basic authentication
            return false;
        }
    }
}
