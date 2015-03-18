using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class Credentials
    {
        public Credentials()
        {
            this.Password = String.Empty;
            this.Username = String.Empty;
        }
        public Credentials(string username)
            : this()
        {
            this.Username = username;
        }
        public Credentials(string username, string password)
            : this()
        {
            this.Username = username;
            this.Password = password;
        }


        public readonly string Password;
        public readonly string Username;
    }
}
