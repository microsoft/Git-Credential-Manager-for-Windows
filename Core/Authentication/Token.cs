using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class Token
    {
        internal Token(string value, TokenType type)
        {
            this.Type = type;
            this.Value = value;
        }
        internal Token(IdentityModel.Clients.ActiveDirectory.AuthenticationResult authResult, TokenType type)
        {
            switch (type)
            {
                case TokenType.Access:
                    this.Value = authResult.AccessToken;
                    break;

                case TokenType.Refresh:
                    this.Value = authResult.RefreshToken;
                    break;

                default:
                    throw new ArgumentException("Unexpected token type encountered", "type");
            }

            this.Type = type;
        }

        public readonly TokenType Type;
        public readonly string Value;

        public override string ToString()
        {
            System.ComponentModel.DescriptionAttribute attribute = Type.GetType()
                                                                       .GetField(Type.ToString())
                                                                       .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                                                                       .SingleOrDefault() as System.ComponentModel.DescriptionAttribute;
            return attribute == null ? Type.ToString() : attribute.Description;
        }

        internal static unsafe bool Deserialize(byte[] bytes, out Token token)
        {
            Debug.Assert(bytes != null, "The bytes parameter is null");
            Debug.Assert(bytes.Length > sizeof(DateTimeOffset), "The bytes parameter is too short");

            token = null;

            TokenType type;
            fixed (byte* p = bytes)
            {
                type = (TokenType)((int*)p)[0];
            }

            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The value of type is not a known value of TokenType");

            string value = Encoding.UTF8.GetString(bytes, sizeof(int), bytes.Length - sizeof(int));
            token = new Token(value, type);

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
                bytes = new byte[encoded.Length + sizeof(int)];

                fixed (byte* p = bytes)
                {
                    ((int*)p)[0] = (int)token.Type;
                }

                Array.Copy(encoded, 0, bytes, sizeof(int), encoded.Length);
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

        public static explicit operator Credential(Token token)
        {
            if (token.Type != TokenType.VsoPat)
                throw new InvalidCastException("Cannot cast " + token + " to credentials");

            return new Credential(token.ToString(), token.Value);
        }
    }
}
