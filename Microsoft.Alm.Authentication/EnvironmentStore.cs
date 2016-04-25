using System;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Interface to secure secrets storage which indexes values by target and utilizes
    /// process scoped environment variables as the storage mechanism.
    /// </summary>
    public sealed class EnvironmentStore : ICredentialStore, ITokenStore
    {
        public EnvironmentStore(string credentialKey)
        {
            if (String.IsNullOrWhiteSpace(credentialKey))
                throw new ArgumentNullException("credentialKey");

            _environmentVariableName = credentialKey;
        }

        private readonly string _environmentVariableName;

        /// <summary>
        /// Deletes credentials for target URI from the credential store
        /// </summary>
        /// <param name="targetUri">(ignored) The URI of the target for which credentials are being deleted</param>
        public void DeleteCredentials(TargetUri targetUri)
        {
            Environment.SetEnvironmentVariable(_environmentVariableName, null, EnvironmentVariableTarget.Process);
        }

        /// <summary>
        /// Deletes the token for target URI from the token store
        /// </summary>
        /// <param name="targetUri">(ignored) The URI of the target for which the token is being deleted</param>
        public void DeleteToken(TargetUri targetUri)
        {
            Environment.SetEnvironmentVariable(_environmentVariableName, null, EnvironmentVariableTarget.Process);
        }

        /// <summary>
        /// Reads credentials for a target URI from the credential store
        /// </summary>
        /// <param name="targetUri">(ignored) The URI of the target for which credentials are being read</param>
        /// <param name="credentials">The credentials from the store; <see langword="null"/> if failure</param>
        /// <returns><see langword="true"/> if success; <see langword="false"/> if failure</returns>
        public bool ReadCredentials(TargetUri targetUri, out Credential credentials)
        {
            credentials = null;

            string value;
            if ((value = Environment.GetEnvironmentVariable(_environmentVariableName, EnvironmentVariableTarget.Process)) != null)
            {
                int index = value.IndexOf(':');
                if (index < 0)
                {
                    credentials = new Credential("username", value);
                }
                else
                {
                    string username = value.Substring(0, index - 1);
                    string password = value.Substring(index + 1);

                    credentials = new Credential(username, password);
                }
            }

            return credentials != null;
        }

        /// <summary>
        /// Reads a token for a target URI from the token store
        /// </summary>
        /// <param name="targetUri">(ignored) The URI of the target for which a token is being read</param>
        /// <param name="token">The token from the store; <see langword="null"/> if failure</param>
        /// <returns><see langword="true"/> if success; <see langword="false"/> if failure</returns>
        public bool ReadToken(TargetUri targetUri, out Token token)
        {
            token = null;

            string value;
            if ((value = Environment.GetEnvironmentVariable(_environmentVariableName, EnvironmentVariableTarget.Process)) != null)
            {
                token = new Token(value, TokenType.Personal);
            }

            return token != null;
        }

        /// <summary>
        /// Writes credentials for a target URI to the credential store
        /// </summary>
        /// <param name="targetUri">(ignored) The URI of the target for which credentials are being stored</param>
        /// <param name="credentials">The credentials to be stored</param>
        public void WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            Environment.SetEnvironmentVariable(_environmentVariableName, String.Format("{0}:{1}", credentials.Username, credentials.Password), EnvironmentVariableTarget.Process);
        }

        /// <summary>
        /// Writes a token for a target URI to the token store
        /// </summary>
        /// <param name="targetUri">(ignored) The URI of the target for which a token is being stored</param>
        /// <param name="token">The token to be stored</param>
        public void WriteToken(TargetUri targetUri, Token token)
        {
            Environment.SetEnvironmentVariable(_environmentVariableName, token.Value, EnvironmentVariableTarget.Process);
        }
    }
}
