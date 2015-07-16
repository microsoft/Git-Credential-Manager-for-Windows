using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// Credentials for user authentication.
    /// </summary>
    public sealed class Credential
    {
        public static readonly Credential Empty = new Credential();

        private Credential()
        {
            this.Password = String.Empty;
            this.Username = String.Empty;
        }
        public Credential(string username)
            : this()
        {
            this.Username = username ?? String.Empty;
        }
        public Credential(string username, string password)
            : this()
        {
            this.Username = username ?? String.Empty;
            this.Password = password ?? String.Empty;
        }

        /// <summary>
        /// Secret related to the username.
        /// </summary>
        public readonly string Password;
        /// <summary>
        /// Unique identitfier of the user.
        /// </summary>
        public readonly string Username;

        internal static void Validate(Credential credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException("credentials", "The Credentials object cannot be null");
            if (credentials.Password.Length > NativeMethods.CREDENTIAL_PASSWORD_MAXLEN)
                throw new ArgumentOutOfRangeException("credentials", string.Format("The Password field of the Credentials object cannot be longer than {0} characters", NativeMethods.CREDENTIAL_USERNAME_MAXLEN));
            if (credentials.Username.Length > NativeMethods.CREDENTIAL_USERNAME_MAXLEN)
                throw new ArgumentOutOfRangeException("credentials", string.Format("The Username field of the Credentials object cannot be longer than {0} characters", NativeMethods.CREDENTIAL_USERNAME_MAXLEN));
        }
    }
}
