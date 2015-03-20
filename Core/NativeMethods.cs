using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal static class NativeMethods
    {
        #region advapi.dll
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
        #endregion
        #region credui.dll
        internal enum CREDUIWIN : uint
        {
            /// <summary>
            /// The caller is requesting that the credential provider return the user name and password in plain text.
            /// This value cannot be combined with SECURE_PROMPT.
            /// </summary>
            GENERIC = 0x1,
            /// <summary>
            /// The Save check box is displayed in the dialog box.
            /// </summary>
            CHECKBOX = 0x2,
            /// <summary>
            /// Only credential providers that support the authentication package specified by the authPackage parameter should be enumerated.
            /// This value cannot be combined with CREDUIWIN_IN_CRED_ONLY.
            /// </summary>
            AUTHPACKAGE_ONLY = 0x10,
            /// <summary>
            /// Only the credentials specified by the InAuthBuffer parameter for the authentication package specified by the authPackage parameter should be enumerated.
            /// If this flag is set, and the InAuthBuffer parameter is NULL, the function fails.
            /// This value cannot be combined with CREDUIWIN_AUTHPACKAGE_ONLY.
            /// </summary>
            IN_CRED_ONLY = 0x20,
            /// <summary>
            /// Credential providers should enumerate only administrators. This value is intended for User Account Control (UAC) purposes only. We recommend that external callers not set this flag.
            /// </summary>
            ENUMERATE_ADMINS = 0x100,
            /// <summary>
            /// Only the incoming credentials for the authentication package specified by the authPackage parameter should be enumerated.
            /// </summary>
            ENUMERATE_CURRENT_USER = 0x200,
            /// <summary>
            /// The credential dialog box should be displayed on the secure desktop. This value cannot be combined with CREDUIWIN_GENERIC.
            /// Windows Vista: This value is not supported until Windows Vista with SP1.
            /// </summary>
            SECURE_PROMPT = 0x1000,
            /// <summary>
            /// The credential provider should align the credential BLOB pointed to by the refOutAuthBuffer parameter to a 32-bit boundary, even if the provider is running on a 64-bit system.
            /// </summary>
            PACK_32_WOW = 0x10000000,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        internal enum CREDUI_ERROR : uint
        {
            NO_ERROR = 0,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_NOT_FOUND = 1168,
            ERROR_INVALID_ACCOUNT_NAME = 1315,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INVALID_FLAGS = 1004
        }

        internal enum CRED_PACK : uint
        {
            GENERIC_CREDENTIALS = 4
        }

        /// <summary>
        /// The CredUIPromptForWindowsCredentials function creates and displays a configurable dialog box that allows users to supply credential information by using any credential provider installed on the local computer.
        /// </summary>
        /// <param name="uiInfo">
        /// A pointer to a CREDUI_INFO structure that contains information for customizing the appearance of the dialog box that this function displays.
        /// If the hwndParent member of the CREDUI_INFO structure is not NULL, this function displays a modal dialog box centered on the parent window.
        /// If the hwndParent member of the CREDUI_INFO structure is NULL, the function displays a dialog box centered on the screen.
        /// This function ignores the hbmBanner member of the CREDUI_INFO structure.
        /// </param>
        /// <param name="dwAuthError">A Windows error code, defined in Winerror.h, that is displayed in the dialog box. If credentials previously collected were not valid, the caller uses this parameter to pass the error message from the API that collected the credentials (for example, Winlogon) to this function. The corresponding error message is formatted and displayed in the dialog box. Set the value of this parameter to zero to display no error message.</param>
        /// <param name="pulAuthPackage">
        /// On input, the value of this parameter is used to specify the authentication package for which the credentials in the pvInAuthBuffer buffer are serialized. If the value of pvInAuthBuffer is NULL and the CREDUIWIN_AUTHPACKAGE_ONLY flag is set in the dwFlags parameter, only credential providers capable of serializing credentials for the specified authentication package are to be enumerated.
        /// To get the appropriate value to use for this parameter on input, call the LsaLookupAuthenticationPackage function and use the value of the AuthenticationPackage parameter of that function.
        /// On output, this parameter specifies the authentication package for which the credentials in the ppvOutAuthBuffer buffer are serialized.
        /// </param>
        /// <param name="pvInAuthBuffer">A pointer to a credential BLOB that is used to populate the credential fields in the dialog box. Set the value of this parameter to NULL to leave the credential fields empty.</param>
        /// <param name="ulInAuthBufferSize">The size, in bytes, of the pvInAuthBuffer buffer.</param>
        /// <param name="ppvOutAuthBuffer">
        /// The address of a pointer that, on output, specifies the credential BLOB. For Kerberos, NTLM, or Negotiate credentials, call the CredUnPackAuthenticationBuffer function to convert this BLOB to string representations of the credentials.
        /// When you have finished using the credential BLOB, clear it from memory by calling the SecureZeroMemory function, and free it by calling the CoTaskMemFree function.
        /// </param>
        /// <param name="pulOutAuthBufferSize">The size, in bytes, of the ppvOutAuthBuffer buffer.</param>
        /// <param name="pfSave">
        /// A pointer to a Boolean value that, on input, specifies whether the Save check box is selected in the dialog box that this function displays. On output, the value of this parameter specifies whether the Save check box was selected when the user clicks the Submit button in the dialog box. Set this parameter to NULL to ignore the Save check box.
        /// This parameter is ignored if the CREDUIWIN_CHECKBOX flag is not set in the dwFlags parameter.
        /// </param>
        /// <param name="dwFlags">A value that specifies behavior for this function. </param>
        /// <returns>If the function succeeds, the function returns ERROR_SUCCESS. If the function is canceled by the user, it returns ERROR_CANCELLED. Any other return value indicates that the function failed to load.</returns>
        [DllImport("credui.dll", CharSet = CharSet.Unicode, EntryPoint = "CredUIPromptForWindowsCredentials", SetLastError = true)]
        internal static extern CREDUI_ERROR CredUIPromptForWindowsCredentials(ref CREDUI_INFO uiInfo, uint dwAuthError, ref uint pulAuthPackage, IntPtr pvInAuthBuffer, uint ulInAuthBufferSize, out IntPtr ppvOutAuthBuffer, out uint pulOutAuthBufferSize, ref bool pfSave, CREDUIWIN dwFlags);
        /// <summary>
        /// The CredUnPackAuthenticationBuffer function converts an authentication buffer returned by a call to the CredUIPromptForWindowsCredentials function into a string user name and password.
        /// </summary>
        /// <param name="dwFlags">
        /// Setting the value of this parameter to CRED_PACK_PROTECTED_CREDENTIALS specifies that the function attempts to decrypt the credentials in the authentication buffer. If the credential cannot be decrypted, the function returns FALSE, and a call to the GetLastError function will return the value ERROR_NOT_CAPABLE.
        /// How the decryption is done depends on the format of the authentication buffer.
        /// If the authentication buffer is a SEC_WINNT_AUTH_IDENTITY_EX2 structure, the function can decrypt the buffer if it is encrypted by using SspiEncryptAuthIdentityEx with the SEC_WINNT_AUTH_IDENTITY_ENCRYPT_SAME_LOGON option.
        /// If the authentication buffer is one of the marshaled KERB_* _LOGON structures, the function decrypts the password before returning it in the pszPassword buffer.
        /// </param>
        /// <param name="pAuthBuffer">
        /// A pointer to the authentication buffer to be converted.
        /// This buffer is typically the output of the CredUIPromptForWindowsCredentials or CredPackAuthenticationBuffer function. This must be one of the following types:
        /// A SEC_WINNT_AUTH_IDENTITY_EX2 structure for identity credentials. The function does not accept other SEC_WINNT_AUTH_IDENTITY structures.
        /// A KERB_INTERACTIVE_LOGON or KERB_INTERACTIVE_UNLOCK_LOGON structure for password credentials.
        /// A KERB_CERTIFICATE_LOGON or KERB_CERTIFICATE_UNLOCK_LOGON structure for smart card certificate credentials.
        /// GENERIC_CRED for generic credentials.
        /// </param>
        /// <param name="cbAuthBuffer">The size, in bytes, of the pAuthBuffer buffer.</param>
        /// <param name="pszUserName">A pointer to a null-terminated string that receives the user name.</param>
        /// <param name="pcchMaxUserName">A pointer to a DWORD value that specifies the size, in characters, of the pszUserName buffer. On output, if the buffer is not of sufficient size, specifies the required size, in characters, of the pszUserName buffer. The size includes terminating null character.</param>
        /// <param name="pszDomainName">A pointer to a null-terminated string that receives the name of the user's domain.</param>
        /// <param name="pcchMaxDomainame">A pointer to a DWORD value that specifies the size, in characters, of the pszDomainName buffer. On output, if the buffer is not of sufficient size, specifies the required size, in characters, of the pszDomainName buffer. The size includes the terminating null character. The required size can be zero if there is no domain name.</param>
        /// <param name="pszPassword">A pointer to a null-terminated string that receives the password.</param>
        /// <param name="pcchMaxPassword">A pointer to a DWORD value that specifies the size, in characters, of the pszPassword buffer. On output, if the buffer is not of sufficient size, specifies the required size, in characters, of the pszPassword buffer. The size includes the terminating null character.</param>
        /// <returns>TRUE if the function succeeds; otherwise, FALSE.</returns>
        [DllImport("credui.dll", CharSet = CharSet.Auto, EntryPoint = "CredUnPackAuthenticationBuffer", SetLastError = true)]
        internal static extern bool CredUnPackAuthenticationBuffer(uint dwFlags, IntPtr pAuthBuffer, uint cbAuthBuffer, StringBuilder pszUserName, ref uint pcchMaxUserName, StringBuilder pszDomainName, ref uint pcchMaxDomainame, StringBuilder pszPassword, ref uint pcchMaxPassword);
        /// <summary>
        /// The CredPackAuthenticationBuffer function converts a string user name and password into an authentication buffer.
        /// Beginning with Windows 8 and Windows Server 2012, the CredPackAuthenticationBuffer function converts an identity credential into an authentication buffer, which is a SEC_WINNT_AUTH_IDENTITY_EX2 structure.This buffer can be passed to LsaLogonUser, AcquireCredentialsHandle, or other identity provider interfaces.
        /// </summary>
        /// <param name="dwFlags">Specifies how the credential should be packed.</param>
        /// <param name="pszUserName">A pointer to a null-terminated string that specifies the user name to be converted.</param>
        /// <param name="pszPassword">A pointer to a null-terminated string that specifies the password to be converted.</param>
        /// <param name="pPackedCredentials">A pointer to an array of bytes that, on output, receives the packed authentication buffer. This parameter can be NULL to receive the required buffer size in the pcbPackedCredentials parameter.</param>
        /// <param name="pcbPackedCredentials">A pointer to a DWORD value that specifies the size, in bytes, of the pPackedCredentials buffer. On output, if the buffer is not of sufficient size, specifies the required size, in bytes, of the pPackedCredentials buffer.</param>
        /// <returns>TRUE if the function succeeds; otherwise, FALSE.</returns>
        [DllImport("credui.dll", CharSet = CharSet.Unicode, EntryPoint = "CredPackAuthenticationBuffer", SetLastError = true)]
        internal static extern bool CredPackAuthenticationBuffer(CRED_PACK dwFlags, string pszUserName, string pszPassword, IntPtr pPackedCredentials, ref uint pcbPackedCredentials);
        #endregion
    }
}

