using System;
using System.Runtime.InteropServices;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal static class NativeMethods
    {
        internal const int CREDENTIAL_ERROR_NOT_FOUND = 1168;
        internal const int CREDENTIAL_USERNAME_MAXLEN = 513;

        [Flags]
        internal enum CRED_FLAGS : uint
        {
            /// <summary>
            /// Bit set if the credential does not persist the CredentialBlob and the credential has not been written during this logon session. 
            /// This bit is ignored on input and is set automatically when queried.
            /// </summary>
            /// <remarks>
            /// If Type is CRED_TYPE_DOMAIN_CERTIFICATE, the CredentialBlob is not persisted across logon sessions because the PIN of a certificate is very sensitive information. 
            /// Indeed, when the credential is written to credential manager, the PIN is passed to the CSP associated with the certificate. 
            /// The CSP will enforce a PIN retention policy appropriate to the certificate.
            /// 
            /// If Type is CRED_TYPE_DOMAIN_PASSWORD or CRED_TYPE_DOMAIN_CERTIFICATE, an authentication package always fails an authentication attempt when using credentials marked as CRED_FLAGS_PROMPT_NOW. 
            /// The application (typically through the key ring UI) prompts the user for the password. The application saves the credential and retries the authentication. 
            /// Because the credential has been recently written, the authentication package now gets a credential that is not marked as CRED_FLAGS_PROMPT_NOW.
            /// </remarks>
            PROMPT_NOW = 0x02,
            /// <summary>
            /// Bit is set if this credential has a TargetName member set to the same value as the UserName member. 
            /// Such a credential is one designed to store the CredentialBlob for a specific user.
            /// </summary>
            /// <remarks>
            /// This bit can only be specified if Type is CRED_TYPE_DOMAIN_PASSWORD or CRED_TYPE_DOMAIN_CERTIFICATE.
            /// </remarks>
            /// <seealso cref="https://msdn.microsoft.com/en-us/library/windows/desktop/aa374801(v=vs.85).aspx"/>
            USERNAME_TARGET = 0x04,
        }

        internal enum CRED_PERSIST : uint
        {
            /// <summary>
            /// The credential persists for the life of the logon session. 
            /// It will not be visible to other logon sessions of this same user. 
            /// It will not exist after this user logs off and back on.
            /// </summary>
            SESSION = 0x01,
            /// <summary>
            /// The credential persists for all subsequent logon sessions on this same computer. 
            /// It is visible to other logon sessions of this same user on this same computer and not visible to logon sessions for this user on other computers.
            /// </summary>
            /// <remarks>
            /// Windows Vista Home Basic, Windows Vista Home Premium, Windows Vista Starter, and Windows XP Home Edition:  This value is not supported.
            /// </remarks>
            LOCAL_MACHINE = 0x02,
            /// <summary>
            /// The credential persists for all subsequent logon sessions on this same computer. 
            /// It is visible to other logon sessions of this same user on this same computer and to logon sessions for this user on other computers.
            /// </summary>
            /// <remarks>
            /// Windows Vista Home Basic, Windows Vista Home Premium, Windows Vista Starter, and Windows XP Home Edition:  This value is not supported.
            /// </remarks>
            ENTERPRISE = 0x03
        }

        internal enum CRED_TYPE : uint
        {
            /// <summary>
            /// The credential is a generic credential. The credential will not be used by any particular authentication package. 
            /// The credential will be stored securely but has no other significant characteristics.
            /// </summary>
            GENERIC = 0x01,
            /// <summary>
            /// The credential is a password credential and is specific to Microsoft's authentication packages. 
            /// The NTLM, Kerberos, and Negotiate authentication packages will automatically use this credential when connecting to the named target.
            /// </summary>
            DOMAIN_PASSWORD = 0x02,
            /// <summary>
            /// The credential is a certificate credential and is specific to Microsoft's authentication packages. 
            /// The Kerberos, Negotiate, and Schannel authentication packages automatically use this credential when connecting to the named target.
            /// </summary>
            DOMAIN_CERTIFICATE = 0x03,
            /// <summary>
            /// The credential is a password credential and is specific to authentication packages from Microsoft. 
            /// The Passport authentication package will automatically use this credential when connecting to the named target.
            /// </summary>
            [Obsolete("This value is no longer supported", true)]
            DOMAIN_VISIBLE_PASSWORD = 0x04,
            /// <summary>
            /// The credential is a certificate credential that is a generic authentication package.
            /// </summary>
            CRED_TYPE_GENERIC_CERTIFICATE = 0x05,
            /// <summary>
            /// The credential is supported by extended Negotiate packages.
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:  This value is not supported.
            /// </remarks>
            CRED_TYPE_DOMAIN_EXTENDED = 0x06,
            /// <summary>
            /// The maximum number of supported credential types.
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:  This value is not supported.
            /// </remarks>
            CRED_TYPE_MAXIMUM = 0x07,
            /// <summary>
            /// The extended maximum number of supported credential types that now allow new applications to run on older operating systems.
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:  This value is not supported.
            /// </remarks>
            CRED_TYPE_MAXIMUM_EX = CRED_TYPE_MAXIMUM + 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CREDENTIAL
        {
            /// <summary>
            /// A bit member that identifies characteristics of the credential. 
            /// Undefined bits should be initialized as zero and not otherwise altered to permit future enhancement.
            /// </summary>
            public CRED_FLAGS Flags;
            /// <summary>
            /// The type of the credential. This member cannot be changed after the credential is created. 
            /// </summary>
            public CRED_TYPE Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetName;
            /// <summary>
            /// A string comment from the user that describes this credential. 
            /// This member cannot be longer than CRED_MAX_STRING_LENGTH (256) characters.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
            /// <summary>
            /// The time, in Coordinated Universal Time (Greenwich Mean Time), of the last modification of the credential. 
            /// For write operations, the value of this member is ignored.
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            /// <summary>
            /// The size, in bytes, of the CredentialBlob member. 
            /// This member cannot be larger than CRED_MAX_CREDENTIAL_BLOB_SIZE (512) bytes.
            /// </summary>
            public uint CredentialBlobSize;
            /// <summary>
            /// Secret data for the credential. The CredentialBlob member can be both read and written.
            /// </summary>
            /// <remarks>
            /// If the Type member is CRED_TYPE_DOMAIN_PASSWORD, this member contains the plaintext Unicode password for UserName.
            /// The CredentialBlob and CredentialBlobSize members do not include a trailing zero character.
            /// Also, for CRED_TYPE_DOMAIN_PASSWORD, this member can only be read by the authentication packages.
            /// 
            /// If the Type member is CRED_TYPE_DOMAIN_CERTIFICATE, this member contains the clear test Unicode PIN for UserName.
            /// The CredentialBlob and CredentialBlobSize members do not include a trailing zero character. 
            /// Also, this member can only be read by the authentication packages.
            /// 
            /// If the Type member is CRED_TYPE_GENERIC, this member is defined by the application.
            /// 
            /// Credentials are expected to be portable. Applications should ensure that the data in CredentialBlob is portable.
            /// </remarks>
            public IntPtr CredentialBlob;
            /// <summary>
            /// Defines the persistence of this credential. This member can be read and written.
            /// </summary>
            public CRED_PERSIST Persist;
            /// <summary>
            /// The number of application-defined attributes to be associated with the credential. 
            /// This member can be read and written. Its value cannot be greater than CRED_MAX_ATTRIBUTES (64).
            /// </summary>
            public uint AttributeCount;
            /// <summary>
            /// Application-defined attributes that are associated with the credential. This member can be read and written.
            /// </summary>
            public IntPtr Attributes;
            /// <summary>
            /// Alias for the TargetName member. This member can be read and written. It cannot be longer than CRED_MAX_STRING_LENGTH (256) characters.
            /// If the credential Type is CRED_TYPE_GENERIC, this member can be non-NULL, but the credential manager ignores the member.
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetAlias;
            /// <summary>
            /// The user name of the account used to connect to TargetName.
            /// </summary>
            /// <remarks>
            /// If the credential Type is CRED_TYPE_DOMAIN_PASSWORD, this member can be either a DomainName\UserName or a UPN.
            /// If the credential Type is CRED_TYPE_DOMAIN_CERTIFICATE, this member must be a marshaled certificate reference created by calling CredMarshalCredential with a CertCredential.
            /// If the credential Type is CRED_TYPE_GENERIC, this member can be non-NULL, but the credential manager ignores the member.
            /// This member cannot be longer than CRED_MAX_USERNAME_LENGTH(513) characters.
            /// </remarks>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UserName;
        }

        /// <summary>
        /// The CredWrite function creates a new credential or modifies an existing credential in the user's credential set. 
        /// The new credential is associated with the logon session of the current token. 
        /// The token must not have the user's security identifier (SID) disabled.
        /// </summary>
        /// <param name="credential">A pointer to the CREDENTIAL structure to be written.</param>
        /// <param name="flags">
        /// Flags that control the function's operation.
        /// Must be set to 0.</param>
        /// <returns>
        /// The function returns TRUE on success and FALSE on failure. 
        /// The GetLastError function can be called to get a more specific status code.
        /// </returns>
        [DllImport("Advapi32.dll", SetLastError = true, EntryPoint = "CredWriteW", CharSet = CharSet.Unicode)]
        internal static extern bool CredWrite(ref CREDENTIAL credential, UInt32 flags);
        /// <summary>
        /// The CredRead function reads a credential from the user's credential set. 
        /// The credential set used is the one associated with the logon session of the current token. 
        /// The token must not have the user's SID disabled.
        /// </summary>
        /// <param name="targetName">Pointer to a null-terminated string that contains the name of the credential to read.</param>
        /// <param name="type">Type of the credential to read. Type must be one of the CRED_TYPE_* defined types.</param>
        /// <param name="flags">Currently reserved and must be zero.</param>
        /// <param name="credential">
        /// Pointer to a single allocated block buffer to return the credential. 
        /// Any pointers contained within the buffer are pointers to locations within this single allocated block. 
        /// The single returned buffer must be freed by calling CredFree.
        /// </param>
        /// <returns>
        /// The function returns TRUE on success and FALSE on failure. 
        /// The GetLastError function can be called to get a more specific status code.
        /// </returns>
        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredRead(string targetName, CRED_TYPE type, uint flags, out IntPtr credential);
        /// <summary>
        /// The CredDelete function deletes a credential from the user's credential set. 
        /// The credential set used is the one associated with the logon session of the current token. 
        /// The token must not have the user's SID disabled.
        /// </summary>
        /// <param name="targetName">Pointer to a null-terminated string that contains the name of the credential to delete.</param>
        /// <param name="type">
        /// Type of the credential to delete. Must be one of the CRED_TYPE_* defined types. For a list of the defined types, see the Type member of the CREDENTIAL structure.
        /// If the value of this parameter is CRED_TYPE_DOMAIN_EXTENDED, this function can delete a credential that specifies a user name when there are multiple credentials for the same target.The value of the TargetName parameter must specify the user name as Target|UserName.
        /// </param>
        /// <param name="flags">Reserved and must be zero.</param>
        /// <returns>
        /// The function returns TRUE on success and FALSE on failure. 
        /// The GetLastError function can be called to get a more specific status code.
        /// </returns>
        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool CredDelete(string targetName, CRED_TYPE type, uint flags);
        /// <summary>
        /// The CredFree function frees a buffer returned by any of the credentials management functions.
        /// </summary>
        /// <param name="buffer"Pointer to the buffer to be freed.></param>
        [DllImport("advapi32.dll", EntryPoint = "CredFree")]
        internal static extern void CredFree(IntPtr buffer);
    }
}
