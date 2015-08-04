using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.TeamFoundation.Authentication
{
    /// <summary>
    /// A security token, usually acquired by some authentication and identity services.
    /// </summary>
    public class Token : Secret, IEquatable<Token>
    {
        public static bool GetFriendlyNameFromType(TokenType type, out string name)
        {
            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The type parameter is invalid");

            name = null;

            System.ComponentModel.DescriptionAttribute attribute = type.GetType()
                                                                       .GetField(type.ToString())
                                                                       .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
                                                                       .SingleOrDefault() as System.ComponentModel.DescriptionAttribute;
            name = attribute == null
                ? type.ToString()
                : attribute.Description;

            return name != null;
        }

        public static bool GetTypeFromFriendlyName(string name, out TokenType type)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(name), "The name parameter is null or invalid");

            type = TokenType.Unknown;

            foreach (var value in Enum.GetValues(typeof(TokenType)))
            {
                type = (TokenType)value;

                string typename;
                if (GetFriendlyNameFromType(type, out typename))
                {
                    if (String.Equals(name, typename, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        internal Token(string value, TokenType type)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(value), "The value parameter is null or invalid");
            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The type parameter is invalid");

            this.Type = type;
            this.Value = value;
        }
        internal Token(string value, string typeName)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(value), "The value parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(typeName), "The typeName parameter is null or invalid");

            TokenType type;
            if (!GetTypeFromFriendlyName(typeName, out type))
            {
                throw new ArgumentException("Unexpected token type encountered", "typeName");
            }
        }
        internal Token(IdentityModel.Clients.ActiveDirectory.AuthenticationResult authResult, TokenType type)
        {
            Debug.Assert(authResult != null, "The authResult parameter is null");
            Debug.Assert(!String.IsNullOrWhiteSpace(authResult.AccessToken), "The authResult.AccessToken parameter is null or invalid.");
            Debug.Assert(!String.IsNullOrWhiteSpace(authResult.RefreshToken), "The authResult.RefreshToken parameter is null or invalid.");
            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The type parameter is invalid");

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

            Guid targetId = Guid.Empty;
            if (Guid.TryParse(authResult.TenantId, out targetId))
            {
                this.TenantId = targetId;
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
        /// The guid form Identity of the target
        /// </summary>
        public Guid TenantId { get; internal set; }

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
            string value;
            if (GetFriendlyNameFromType(Type, out value))
                return value;
            else
                return base.ToString();
        }

        internal static unsafe bool Deserialize(byte[] bytes, TokenType type, out Token token)
        {
            Debug.Assert(bytes != null, "The bytes parameter is null");
            Debug.Assert(bytes.Length > 0, "The bytes parameter is too short");
            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The type parameter is invlaid");

            token = null;

            try
            {
                int preamble = sizeof(TokenType) + sizeof(Guid);

                if (bytes.Length > preamble)
                {
                    TokenType readType;
                    Guid tenantId;

                    fixed (byte* p = bytes)
                    {
                        readType = *(TokenType*)p;
                        byte* g = p + sizeof(TokenType);
                        tenantId = *(Guid*)g;
                    }

                    if (readType == type)
                    {
                        string value = Encoding.UTF8.GetString(bytes, preamble, bytes.Length - preamble);

                        if (!String.IsNullOrWhiteSpace(value))
                        {
                            token = new Token(value, type);
                            token.TenantId = tenantId;
                        }
                    }
                }

                // if value hasn't been set yet, fall back to old format decode
                if (token == null)
                {
                    string value = Encoding.UTF8.GetString(bytes);

                    if (!String.IsNullOrWhiteSpace(value))
                    {
                        token = new Token(value, type);
                    }
                }
            }
            catch
            {
                Trace.WriteLine("   token deserialization error");
            }

            return token != null;
        }

        internal static unsafe bool Serialize(Token token, out byte[] bytes)
        {
            Debug.Assert(token != null, "The token parameter is null");
            Debug.Assert(!String.IsNullOrWhiteSpace(token.Value), "The token.Value is invalid");

            bytes = null;

            try
            {
                byte[] utf8bytes = Encoding.UTF8.GetBytes(token.Value);
                bytes = new byte[utf8bytes.Length + sizeof(TokenType) + sizeof(Guid)];
                byte[] guid = new byte[sizeof(Guid)];

                fixed (byte* p = bytes)
                {
                    *((TokenType*)p) = token.Type;
                    byte* g = p + sizeof(TokenType);
                    *(Guid*)g = token.TenantId;
                }

                Array.Copy(utf8bytes, 0, bytes, sizeof(TokenType) + sizeof(Guid), utf8bytes.Length);
            }
            catch
            {
                Trace.WriteLine("   token serialization error");
            }

            return bytes != null;
        }

        internal static void Validate(Token token)
        {
            if (token == null)
                throw new ArgumentNullException("token", "The `token` parameter is null or invalid.");
            if (String.IsNullOrWhiteSpace(token.Value))
                throw new ArgumentException("The value of the `token` cannot be null or empty.", "token");
            if (token.Value.Length > NativeMethods.Credential.PasswordMaxLength)
                throw new ArgumentOutOfRangeException("token", String.Format("The value of the `token` cannot be longer than {0} characters.", NativeMethods.Credential.PasswordMaxLength));
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
