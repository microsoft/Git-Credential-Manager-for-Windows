using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class Token
    {
        public Token(string value, DateTimeOffset expires)
        {
            this.Expires = expires;
            this.Value = value;
        }

        public readonly DateTimeOffset Expires;
        public readonly string Value;

        internal static unsafe bool Deserialize(byte[] bytes, out Token token)
        {
            Debug.Assert(bytes != null, "The bytes parameter is null");
            Debug.Assert(bytes.Length > sizeof(DateTimeOffset), "The bytes parameter is too short");

            token = null;

            DateTimeOffset expires;
            fixed (byte* p = bytes)
            {
                expires = *((DateTimeOffset*)p);
            }

            string value = Encoding.UTF8.GetString(bytes, sizeof(DateTimeOffset), bytes.Length - sizeof(DateTimeOffset));
            token = new Token(value, expires);

            return token != null;
        }

        internal static unsafe bool Serialize(Token token, out byte[] bytes)
        {
            Debug.Assert(token != null, "The token parameter is null");
            Debug.Assert(!String.IsNullOrWhiteSpace(token.Value), "The token.Value is invalid");

            bytes = null;
            try
            {
                byte[] encoded = Encoding.UTF8.GetBytes(token.Value);
                DateTimeOffset expires = token.Expires;

                bytes = new byte[sizeof(DateTimeOffset) + encoded.Length];

                fixed (byte* p = bytes)
                {
                    *(DateTimeOffset*)p = *(&expires);
                }

                Array.Copy(encoded, 0, bytes, sizeof(DateTimeOffset), encoded.Length);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                bytes = null;
            }

            return bytes != null;
        }

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
