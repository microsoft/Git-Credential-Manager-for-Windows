using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal static class NativeMethods
    {
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

        internal enum CREDUI_ERROR : int
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
        [DllImport("credui.dll", CharSet = CharSet.Unicode, EntryPoint = "CredUIPromptForWindowsCredentials")]
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
        internal static extern bool CredPackAuthenticationBuffer(uint dwFlags, string pszUserName, string pszPassword, IntPtr pPackedCredentials, ref uint pcbPackedCredentials);
    }
}
