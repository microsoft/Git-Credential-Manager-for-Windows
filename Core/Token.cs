using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class Token
    {
        public Token(string value)
        {
            this.Value = value;
        }

        public readonly string Value;

        internal static void Validate(Token token)
        {
            if (token == null)
                throw new ArgumentNullException("token");
            if (String.IsNullOrWhiteSpace(token.Value))
                throw new ArgumentException("The value of the token cannot be null or empty", "token");
            if (token.Value.Length > NativeMethods.CREDENTIAL_PASSWORD_MAXLEN)
                throw new ArgumentOutOfRangeException("token", String.Format("The value of the token cannot be longer than {0} characters", NativeMethods.CREDENTIAL_PASSWORD_MAXLEN));
        }
    }
}
