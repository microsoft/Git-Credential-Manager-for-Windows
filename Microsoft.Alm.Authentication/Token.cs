/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// A security token, usually acquired by some authentication and identity services.
    /// </summary>
    public class Token : Secret, IEquatable<Token>
    {
        public static readonly StringComparer TokenComparer = StringComparer.Ordinal;

        public Token(string value, TokenType type)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), $"The `{nameof(type)}` parameter is invalid");

            _type = type;
            _value = value;
        }

        public Token(string value, string typeName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentNullException(nameof(typeName));

            TokenType type;
            if (!GetTypeFromFriendlyName(typeName, out type))
                throw new ArgumentException("Unknown type name.", nameof(typeName));
        }

        public Token(string value, Guid tenantId, TokenType type)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (type != TokenType.AzureAccess && type != TokenType.AzureFederated && type != TokenType.Personal && type != TokenType.Test)
                throw new ArgumentOutOfRangeException(nameof(type));

            _targetIdentity = tenantId;
            _type = type;
            _value = value;
        }

        private readonly TokenType _type;
        private readonly string _value;
        private Guid _targetIdentity;

        /// <summary>
        /// The type of the security token.
        /// </summary>
        public TokenType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// The raw contents of the token.
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <summary>
        /// The `<see cref="Guid"/>` form Identity of the target
        /// </summary>
        public Guid TargetIdentity
        {
            get { return _targetIdentity; }
            set { _targetIdentity = value; }
        }

        /// <summary>
        /// Compares an object to this <see cref="Token"/> for equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True is equal; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Token);
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
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            type = TokenType.Unknown;

            foreach (var value in Enum.GetValues(typeof(TokenType)))
            {
                type = (TokenType)value;

                if (GetFriendlyNameFromType(type, out string typename))
                {
                    if (string.Equals(name, typename, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a hash code based on the contents of the token.
        /// </summary>
        /// <returns>32-bit hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)_type) * Value.GetHashCode();
            }
        }

        /// <summary>
        /// Converts the token to a human friendly string.
        /// <para/>
        /// Returns a human readable name of the token.
        /// </summary>
        public override string ToString()
        {
            if (GetFriendlyNameFromType(_type, out string value))
                return value;
            else
                return base.ToString();
        }

        public static void Validate(Token token)
        {
            if (token is null)
                throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrWhiteSpace(token._value))
                throw new ArgumentException("Value property returned null or empty.", nameof(token));
            if (token._value.Length > NativeMethods.Credential.PasswordMaxLength)
                throw new ArgumentOutOfRangeException(nameof(token));
        }

        internal static unsafe bool Deserialize(
            RuntimeContext context,
            byte[] bytes,
            TokenType type,
            out Token token)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0)
                throw new ArgumentException("Zero length byte array.", nameof(bytes));

            Debug.Assert(Enum.IsDefined(typeof(TokenType), type), "The type parameter is invalid");

            token = null;

            try
            {
                int preamble = sizeof(TokenType) + sizeof(Guid);

                if (bytes.Length > preamble)
                {
                    TokenType readType;
                    Guid targetIdentity;

                    fixed (byte* p = bytes)
                    {
                        readType = *(TokenType*)p;
                        byte* g = p + sizeof(TokenType);
                        targetIdentity = *(Guid*)g;
                    }

                    if (readType == type)
                    {
                        string value = Encoding.UTF8.GetString(bytes, preamble, bytes.Length - preamble);

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            token = new Token(value, type)
                            {
                                _targetIdentity = targetIdentity
                            };
                        }
                    }
                }

                // If value hasn't been set yet, fall back to old format decode.
                if (token is null)
                {
                    string value = Encoding.UTF8.GetString(bytes);

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        token = new Token(value, type);
                    }
                }
            }
            catch
            {
                context.Trace.WriteLine("! token deserialization error.");
            }

            return token != null;
        }

        internal static unsafe bool Serialize(
            RuntimeContext context,
            Token token,
            out byte[] bytes)
        {
            if (token is null)
                throw new ArgumentNullException(nameof(token));
            if (string.IsNullOrWhiteSpace(token._value))
                throw new ArgumentException("Value property returned null or empty.", nameof(token));

            bytes = null;

            try
            {
                byte[] utf8bytes = Encoding.UTF8.GetBytes(token._value);
                bytes = new byte[utf8bytes.Length + sizeof(TokenType) + sizeof(Guid)];

                fixed (byte* p = bytes)
                {
                    *((TokenType*)p) = token._type;
                    byte* g = p + sizeof(TokenType);
                    *(Guid*)g = token._targetIdentity;
                }

                Array.Copy(utf8bytes, 0, bytes, sizeof(TokenType) + sizeof(Guid), utf8bytes.Length);
            }
            catch
            {
                context.Trace.WriteLine("! token serialization error.");
            }

            return bytes != null;
        }

        /// <summary>
        /// Explicitly casts a personal access token into a set of credentials
        /// </summary>
        /// <param name="token">The token to be cast as a `<see cref="Credential"/>`.</param>
        /// <exception cref="InvalidCastException">
        /// <paramref name="token">Throws if `<see cref="Token.Type"/>` is not `<see cref="TokenType.Personal"/>`.</paramref>
        /// </exception>
        public static explicit operator Credential(Token token)
        {
            if (token is null)
                return null;

            if (token.Type != TokenType.Personal)
                throw new InvalidCastException($"`{nameof(Token)}` -> `{nameof(Credential)}`");

            return new Credential(token.ToString(), token._value);
        }

        /// <summary>
        /// Compares two tokens for equality.
        /// </summary>
        /// <param name="left">Token to compare.</param>
        /// <param name="right">Token to compare.</param>
        /// <returns><see langword="true"/> if equal; otherwise <see langword="false"/>.</returns>
        public static bool operator ==(Token left, Token right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null || right is null)
                return false;

            return left.Type == right.Type
                && TokenComparer.Equals(left._value, right._value);
        }

        /// <summary>
        /// Compares two tokens for inequality.
        /// </summary>
        /// <param name="left">Token to compare.</param>
        /// <param name="right">Token to compare.</param>
        /// <returns><see langword="false"/> if equal; otherwise <see langword="true"/>.</returns>
        public static bool operator !=(Token left, Token right)
        {
            return !(left == right);
        }
    }
}
