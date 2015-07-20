using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// A security token, usually aquired by some authentication and identity services.
    /// </summary>
    public class Token : Secret, IEquatable<Token>
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

        /// <summary>
        /// The type of the secuity token.
        /// </summary>
        public readonly TokenType Type;
        /// <summary>
        /// The raw contents of the token.
        /// </summary>
        public readonly string Value;

        /// <summary>
        /// Compares an object to this <see cref="Token"/> for equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True is equal; false otherwise.</returns>
        public override bool Equals(Object obj)
        {
            return this.Equals(obj as Token);
        }
        /// <summary>
        /// Compares a <see cref="Token"/> to this Token for equality.
        /// </summary>
        /// <param name="other">The token to compare.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public bool Equals(Token other)
        {
            return this == other;
        }
        /// <summary>
        /// Gets a hash code based on the contents of the token.
        /// </summary>
        /// <returns>32-bit hash code.</returns>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                return ((int)Type) * Value.GetHashCode();
            }
        }
        /// <summary>
        /// Converts the token to a human friendly string.
        /// </summary>
        /// <returns>Humanish name of the token.</returns>
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
                throw new ArgumentNullException("token", "The `token` parameter is null or invalid.");
            if (String.IsNullOrWhiteSpace(token.Value))
                throw new ArgumentException("The value of the `token` cannot be null or empty.", "token");
            if (token.Value.Length > NativeMethods.CREDENTIAL_PASSWORD_MAXLEN)
                throw new ArgumentOutOfRangeException("token", String.Format("The value of the `token` cannot be longer than {0} characters.", NativeMethods.CREDENTIAL_PASSWORD_MAXLEN));
        }

        /// <summary>
        /// Explicity casts a personal access token token into a set of credentials
        /// </summary>
        /// <param name="token"></param>
        /// <exception cref="InvalidCastException">
        /// <paramref name="token">Throws if <see cref="Token.Type"/> is not <see cref="TokenType.Personal"/>.</paramref>
        /// </exception>
        public static explicit operator Credential(Token token)
        {
            if (token.Type != TokenType.Personal)
                throw new InvalidCastException("Cannot cast " + token + " to credentials");

            return new Credential(token.ToString(), token.Value);
        }

        /// <summary>
        /// Compares two tokens for equality.
        /// </summary>
        /// <param name="token1">Token to compare.</param>
        /// <param name="token2">Token to compare.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public static bool operator ==(Token token1, Token token2)
        {
            if (ReferenceEquals(token1, token2))
                return true;
            if (ReferenceEquals(token1, null) || ReferenceEquals(null, token2))
                return false;

            return token1.Type == token2.Type
                && String.Equals(token1.Value, token2.Value, StringComparison.Ordinal);
        }
        /// <summary>
        /// Compares two tokens for inequality.
        /// </summary>
        /// <param name="token1">Token to compare.</param>
        /// <param name="token2">Token to compare.</param>
        /// <returns>False if equal; true otherwise.</returns>
        public static bool operator !=(Token token1, Token token2)
        {
            return !(token1 == token2);
        }
    }
}
