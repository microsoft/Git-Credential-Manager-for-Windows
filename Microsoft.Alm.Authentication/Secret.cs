using System;
using System.Diagnostics;

namespace Microsoft.Alm.Authentication
{
    public abstract class Secret
    {
        public static string UriToSimpleName(TargetUri targetUri, string @namespace)
        {
            const string TokenNameBaseFormat = "{0}:{1}://{2}";
            const string TokenNamePortFormat = TokenNameBaseFormat + ":{3}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");

            Trace.WriteLine("Secret::UriToName");

            string targetName = null;
            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            Uri actualUri = targetUri.ActualUri;

            if (actualUri.IsDefaultPort)
            {
                targetName = String.Format(TokenNameBaseFormat, @namespace, actualUri.Scheme, trimmedHostUrl);
            }
            else
            {
                targetName = String.Format(TokenNamePortFormat, @namespace, actualUri.Scheme, trimmedHostUrl, actualUri.Port);
            }

            Trace.WriteLine("   target name = " + targetName);

            return targetName;
        }

        public static string UriToPathedName(TargetUri targetUri, string @namespace)
        {
            const string TokenNamePathFormat = "{0}:{1}://{2}{3}";
            const string TokenNamePortFormat = "{0}:{1}://{2}:{3}{4}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");

            Trace.WriteLine("Secret::UriToPathedName");

            string targetName = null;
            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            Uri actualUri = targetUri.ActualUri;

            if (actualUri.IsDefaultPort)
            {
                targetName = String.Format(TokenNamePathFormat, @namespace, actualUri.Scheme, trimmedHostUrl, actualUri.AbsolutePath);
            }
            else
            {
                targetName = String.Format(TokenNamePortFormat, @namespace, actualUri.Scheme, trimmedHostUrl, actualUri.Port, actualUri.AbsolutePath);
            }

            Trace.WriteLine("   target name = " + targetName);

            return targetName;
        }

        public delegate string UriNameConversion(TargetUri targetUri, string @namespace);
    }
}
