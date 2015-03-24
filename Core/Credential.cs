using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// Credentials for user authentication.
    /// </summary>
    public sealed class Credential
    {
        public Credential()
        {
            this.Password = String.Empty;
            this.Username = String.Empty;
        }
        public Credential(string username)
            : this()
        {
            this.Username = username;
        }
        public Credential(string username, string password)
            : this()
        {
            this.Username = username;
            this.Password = password;
        }

        /// <summary>
        /// Secret related to the username.
        /// </summary>
        public readonly string Password;
        /// <summary>
        /// Unique identitfier of the user.
        /// </summary>
        public readonly string Username;
    }
}
