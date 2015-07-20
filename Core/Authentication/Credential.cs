using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// Credentials for user authentication.
    /// </summary>
    public sealed class Credential : Secret, IEquatable<Credential>
    {
        public static readonly Credential Empty = new Credential(String.Empty, String.Empty);

        /// <summary>
        /// Creates a credential object with a username and password pair.
        /// </summary>
        /// <param name="username">The username value of the <see cref="Credential"/>.</param>
        /// <param name="password">The password value of the <see cref="Credential"/>.</param>
        public Credential(string username, string password)
        {
            this.Username = username ?? String.Empty;
            this.Password = password ?? String.Empty;
        }
        /// <summary>
        /// Creates a credential object with only a username.
        /// </summary>
        /// <param name="username">The username value of the <see cref="Credential"/>.</param>
        public Credential(string username)
            : this(username, String.Empty)
        { }

        /// <summary>
        /// Secret related to the username.
        /// </summary>
        public readonly string Password;
        /// <summary>
        /// Unique identitfier of the user.
        /// </summary>
        public readonly string Username;

        /// <summary>
        /// Compares an object to this <see cref="Credential"/> for equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True is equal; false otherwise.</returns>
        public override bool Equals(Object obj)
        {
            return this == obj as Credential;
        }
        /// <summary>
        /// Compares a <see cref="Credential"/> to this Credetials for equality.
        /// </summary>
        /// <param name="other">Credential to compared.</param>
        /// <returns>True if equal; false if otherwise.</returns>
        public bool Equals(Credential other)
        {
            return this == other;
        }
        /// <summary>
        /// Gets a hash code based on the contents of the <see cref="Credential"/>.
        /// </summary>
        /// <returns>32-bit hash code.</returns>
        public override Int32 GetHashCode()
        {
            unchecked
            {
                return Username.GetHashCode() + 7 * Password.GetHashCode();
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static void Validate(Credential credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException("credentials", "The Credentials object cannot be null");
            if (credentials.Password.Length > NativeMethods.CREDENTIAL_PASSWORD_MAXLEN)
                throw new ArgumentOutOfRangeException("credentials", string.Format("The Password field of the Credentials object cannot be longer than {0} characters", NativeMethods.CREDENTIAL_USERNAME_MAXLEN));
            if (credentials.Username.Length > NativeMethods.CREDENTIAL_USERNAME_MAXLEN)
                throw new ArgumentOutOfRangeException("credentials", string.Format("The Username field of the Credentials object cannot be longer than {0} characters", NativeMethods.CREDENTIAL_USERNAME_MAXLEN));
        }

        /// <summary>
        /// Compares two credentials for equality.
        /// </summary>
        /// <param name="credential1">Credential to compare.</param>
        /// <param name="credential2">Credential to compare.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public static bool operator ==(Credential credential1, Credential credential2)
        {
            if (ReferenceEquals(credential1, credential2))
                return true;
            if (ReferenceEquals(credential1, null) || ReferenceEquals(null, credential2))
                return false;

            return String.Equals(credential1.Username, credential2.Username, StringComparison.Ordinal)
                && String.Equals(credential1.Password, credential2.Password, StringComparison.Ordinal);
        }
        /// <summary>
        /// Compares two credentials for inequality.
        /// </summary>
        /// <param name="credential1">Credential to compare.</param>
        /// <param name="credential2">Credential to compare.</param>
        /// <returns>False if equal; true otherwise.</returns>
        public static bool operator !=(Credential credential1, Credential credential2)
        {
            return !(credential1 == credential2);
        }
    }
}
