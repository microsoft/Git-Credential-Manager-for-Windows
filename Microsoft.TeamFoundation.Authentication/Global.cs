using System;

namespace Microsoft.TeamFoundation.Authentication
{
    internal static class Global
    {
        /// <summary>
        /// Creates the correct user-agent string for HTTP calls.
        /// </summary>
        /// <returns>The `user-agent` string for "git-tools".</returns>
        public static string GetUserAgent()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return String.Format("git/1.0 (git-tools/{0})", version.ToString(3));
        }
    }
}
