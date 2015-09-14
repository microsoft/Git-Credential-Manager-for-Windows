using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Alm.Authentication
{
    internal static class NativeMethods
    {
        private const string Advapi32 = "advapi32.dll";
        private const string Kernel32 = "kernel32.dll";

        #region advapi32.dll
        /// <summary>
        /// <para>The CredWrite function creates a new credential or modifies an existing 
        /// credential in the user's credential set. </para>
        /// <para>The new credential is associated with the logon session of the current token. </para>
        /// <para>The token must not have the user's security identifier (SID) disabled.</para>
        /// </summary>
        /// <param name="credential">A pointer to the `<see cref="Credential)"/>` structure to be 
        /// written.</param>
        /// <param name="flags">Flags that control the function's operation. Must be set to 0.</param>
        /// <returns>True if success; false otherwise.</returns>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredWriteW", SetLastError = true)]
        internal static extern bool CredWrite(ref Credential credential, UInt32 flags);

        /// <summary>
        /// <para>Reads a credential from the user's credential set. </para>
        /// <para>The credential set used is the one associated with the logon session of the 
        /// current token. </para>
        /// <para>The token must not have the user's SID disabled.</para>
        /// </summary>
        /// <param name="targetName">Pointer to a null-terminated string that contains the name of 
        /// the credential to read.</param>
        /// <param name="type">Type of the credential to read. Type must be one of the `<see cref="CredentialType"/>` 
        /// defined types.</param>
        /// <param name="flags">Currently reserved and must be zero.</param>
        /// <param name="credential">
        /// <para>Pointer to a single allocated block buffer to return the credential. </para>
        /// <para>Any pointers contained within the buffer are pointers to locations within this 
        /// single allocated block. </para>
        /// <para>The single returned buffer must be freed by calling `<see cref="CredFree(IntPtr)"/>`.</para>
        /// </param>
        /// <returns>True if success; false otherwise.</returns>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredReadW", SetLastError = true)]
        internal static extern bool CredRead(string targetName, CredentialType type, uint flags, out IntPtr credential);

        /// <summary>
        /// <para>The CredDelete function deletes a credential from the user's credential set.</para> 
        /// <para>The credential set used is the one associated with the logon session of the 
        /// current token. </para>
        /// <para>The token must not have the user's SID disabled.</para>
        /// </summary>
        /// <param name="targetName">Pointer to a null-terminated string that contains the name of 
        /// the credential to delete.</param>
        /// <param name="type">
        /// <para>Type of the credential to delete. Must be one of the `<see cref="CredentialType"/>` 
        /// defined types. For a list of the defined types, see the Type member of the `<see cref="Credential"/>` 
        /// structure.</para>
        /// <para>If the value of this parameter is `<see cref="CredentialType.DomainExtended"/>`, 
        /// this function can delete a credential that specifies a user name when there are 
        /// multiple credentials for the same target, and the value of the `<see cref="Credential.TargetName"/>` 
        /// parameter must specify the user name as Target|UserName.</para>
        /// </param>
        /// <param name="flags">Reserved and must be zero.</param>
        /// <returns>True if success; false otherwise.</returns>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredDeleteW", SetLastError = true)]
        internal static extern bool CredDelete(string targetName, CredentialType type, uint flags);

        /// <summary>
        /// The CredFree function frees a buffer returned by any of the credentials management 
        /// functions.
        /// </summary>
        /// <param name="buffer"Pointer to the buffer to be freed.></param>
        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "CredFree", SetLastError = true)]
        internal static extern void CredFree(IntPtr credential);

        [Flags]
        internal enum CredentialFlags : uint
        {
            /// <summary>
            /// <para>Bit set if the `<see cref="Credential"/>` does not persist the `<see cref="Credential.CredentialBlob"/>` 
            /// and the credential has not been written during this logon session. This bit is 
            /// ignored on input and is set automatically when queried.</para>
            /// <para>If `<see cref="Credential.Type"/>` is <see cref="CredentialType.DomainCertificate"/>, 
            /// the `<see cref="Credential.CredentialBlob"/>` is not persisted across logon 
            /// sessions because the PIN of a certificate is very sensitive information.</para>
            /// <para>Indeed, when the credential is written to credential manager, the PIN is 
            /// passed to the CSP associated with the certificate. The CSP will enforce a PIN 
            /// retention policy appropriate to the certificate.</para>
            /// <para>If Type is `<see cref="CredentialType.DomainPassword"/>` or 
            /// `<see cref="CredentialType.DomainCertificate"/>`, an authentication package always 
            /// fails an authentication attempt when using credentials marked as `<see cref="CredentialFlags.PromptNow"/>`.</para>
            /// <para>The application (typically through the key ring UI) prompts the user for the 
            /// password. The application saves the credential and retries the authentication. </para>
            /// <para>Because the credential has been recently written, the authentication package 
            /// now gets a credential that is not marked as `<see cref="CredentialFlags.PromptNow"/>`.</para>
            /// </summary>
            PromptNow = 0x02,
            /// <summary>
            /// <para>Bit is set if this `<see cref="Credential"/>` has a `<see cref="Credential.TargetName"/>` 
            /// member set to the same value as the `<see cref="Credential.UserName"/>` member.</para>
            /// <para>Such a credential is one designed to store the `<see cref="Credential.CredentialBlob"/>` 
            /// for a specific user.</para>
            /// <para>This bit can only be specified if `<see cref="Credential.Type"/>` is 
            /// `<see cref="CredentialType.DomainPassword"/>` or <see cref="CredentialType.DomainCertificate"/>.</para>
            /// </summary>
            UsernameTarget = 0x04,
        }

        internal enum CredentialPersist : uint
        {
            /// <summary>
            /// <para>The `<see cref="Credential"/>` persists for the life of the logon session.</para>
            /// <para>It will not be visible to other logon sessions of this same user.</para>
            /// <para>It will not exist after this user logs off and back on.</para>
            /// </summary>
            Session = 0x01,
            /// <summary>
            /// <para>The `<see cref="Credential"/>` persists for all subsequent logon sessions on 
            /// this same computer.</para> 
            /// <para>It is visible to other logon sessions of this same user on this same computer 
            /// and not visible to logon sessions for this user on other computers.</para>
            /// </summary>
            /// <remarks>
            /// Windows Vista Home Basic, Windows Vista Home Premium, Windows Vista Starter, and 
            /// Windows XP Home Edition:  This value is not supported.
            /// </remarks>
            LocalMachine = 0x02,
            /// <summary>
            /// <para>The `<see cref="Credential"/>` persists for all subsequent logon sessions on 
            /// this same computer. </para>
            /// <para>It is visible to other logon sessions of this same user on this same computer 
            /// and to logon sessions for this user on other computers.</para>
            /// </summary>
            /// <remarks>
            /// Windows Vista Home Basic, Windows Vista Home Premium, Windows Vista Starter, and 
            /// Windows XP Home Edition:  This value is not supported.
            /// </remarks>
            Enterprise = 0x03
        }

        internal enum CredentialType : uint
        {
            /// <summary>
            /// <para>The `<see cref="Credential"/>` is a generic credential. The credential will 
            /// not be used by any particular authentication package.</para>
            /// <para>The credential will be stored securely but has no other significant 
            /// characteristics.<para>
            /// </summary>
            Generic = 0x01,
            /// <summary>
            /// <para>The `<see cref="Credential"/>` is a password credential and is specific to 
            /// Microsoft's authentication packages. </para>
            /// <para>The NTLM, Kerberos, and Negotiate authentication packages will automatically 
            /// use this credential when connecting to the named target.</para>
            /// </summary>
            DomainPassword = 0x02,
            /// <summary>
            /// <para>The `<see cref="Credential"/>` is a certificate credential and is specific to 
            /// Microsoft's authentication packages. </para>
            /// <para>The Kerberos, Negotiate, and Schannel authentication packages automatically 
            /// use this credential when connecting to the named target.</para>
            /// </summary>
            DomainCertificate = 0x03,
            /// <summary>
            /// <para>The `<see cref="Credential"/>` is a password credential and is specific to 
            /// authentication packages from Microsoft. </para>
            /// <para>The Passport authentication package will automatically use this credential 
            /// when connecting to the named target.</para>
            /// </summary>
            [Obsolete("This value is no longer supported", true)]
            DomainVisiblePassword = 0x04,
            /// <summary>
            /// <para>The `<see cref="Credential"/>` is a certificate credential that is a generic 
            /// authentication package.</para>
            /// </summary>
            GenericCertificate = 0x05,
            /// <summary>
            /// <para>The `<see cref="Credential"/>` is supported by extended Negotiate packages.</para>
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:  This 
            /// value is not supported.
            /// </remarks>
            DomainExtended = 0x06,
            /// <summary>
            /// <para>The maximum number of supported credential types.<para>
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:  This 
            /// value is not supported.
            /// </remarks>
            Maximum = 0x07,
            /// <summary>
            /// <para>The extended maximum number of supported credential types that now allow new 
            /// applications to run on older operating systems.</para>
            /// </summary>
            /// <remarks>
            /// Windows Server 2008, Windows Vista, Windows Server 2003, and Windows XP:  This 
            /// value is not supported.
            /// </remarks>
            MaximumEx = Maximum + 1000
        }

        [SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct Credential
        {
            internal const int AttributeMaxLengh = 63;
            internal const int PasswordMaxLength = 2047;
            internal const int StringMaxLength = 255;
            internal const int UsernameMaxLength = 511;

            /// <summary>
            /// <para>A bit member that identifies characteristics of the credential. </para>
            /// <para>Undefined bits should be initialized as zero and not otherwise altered to 
            /// permit future enhancement.</para>
            /// </summary>
            public CredentialFlags Flags;
            /// <summary>
            /// <para>The type of the credential. This member cannot be changed after the credential 
            /// is created. </para>
            /// </summary>
            public CredentialType Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetName;
            /// <summary>
            /// <para>A string comment from the user that describes this credential. </para>
            /// <para>This member cannot be longer than `<see cref="StringMaxLength"/>` characters.</para>
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
            /// <summary>
            /// <para>The time, in Coordinated Universal Time (Greenwich Mean Time), of the last 
            /// modification of the credential. </para>
            /// <para>For write operations, the value of this member is ignored.</para>
            /// </summary>
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            /// <summary>
            /// <para>The size, in bytes, of the `<see cref="CredentialBlob"/>` member. </para>
            /// <para>This member cannot be larger than `<see cref="PasswordMaxLength"/>` bytes.</para>
            /// </summary>
            public uint CredentialBlobSize;
            /// <summary>
            /// <para>Secret data for the credential. The CredentialBlob member can be both read 
            /// and written.</para>
            /// <para>If the `<see cref="Type"/>` member is `<see cref="CredentialType.DomainPassword"/>`, 
            /// this member contains the plaintext Unicode password for `<see cref="UserName"/>`.</para>
            /// <para>The `<see cref="CredentialBlob"/>` and `<see cref="CredentialBlobSize"/>` 
            /// members do not include a trailing zero character.</para>
            /// <para>Also, for `<see cref="CredentialType.DomainPassword"/>`, this member can only 
            /// be read by the authentication packages.</para>
            /// <para>If the Type member is `<see cref="CredentialType.DomainCertificate"/>`, this 
            /// member contains the clear test Unicode PIN for `<see cref="UserName"/>`.</para>
            /// <para>The `<see cref="CredentialBlob"/>` and `<see cref="CredentialBlobSize"/>` 
            /// members do not include a trailing zero character. </para>
            /// <para>Also, this member can only be read by the authentication packages.</para>
            /// <para>If the `<see cref="Type"/>` member is `<see cref="CredentialType.Generic"/>`, 
            /// this member is defined by the application.</para>
            /// <para>Credentials are expected to be portable. Applications should ensure that the 
            /// data in `<see cref="CredentialBlob"/>` is portable.</para>
            /// </summary>
            public IntPtr CredentialBlob;
            /// <summary>
            /// <para>Defines the persistence of this credential. This member can be read and 
            /// written.</para>
            /// </summary>
            public CredentialPersist Persist;
            /// <summary>
            /// <para>The number of application-defined attributes to be associated with the 
            /// credential. </para>
            /// <para>This member can be read and written. Its value cannot be greater than `<see cref="AttributeMaxLengh"/>`.</para>
            /// </summary>
            public uint AttributeCount;
            /// <summary>
            /// <para>Application-defined attributes that are associated with the credential. This 
            /// member can be read and written.</para>
            /// </summary>
            public IntPtr Attributes;
            /// <summary>
            /// <para>Alias for the TargetName member. This member can be read and written. It 
            /// cannot be longer than `<see cref="StringMaxLength"/>` characters.</para>
            /// <para>If the credential Type is `<see cref="CredentialType.Generic"/>`, this member 
            /// can be non-NULL, but the credential manager ignores the member.</para>
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetAlias;
            /// <summary>
            /// <para>The user name of the account used to connect to TargetName.</para>
            /// <para>If the credential Type is `<see cref="CredentialType.DomainPassword"/>`, this 
            /// member can be either a DomainName\UserName or a UPN.</para>
            /// <para>If the credential Type is `<see cref="CredentialType.DomainCertificate"/>`, 
            /// this member must be a marshaled certificate reference created by calling 
            /// `CredMarshalCredential` with a `CertCredential`.</para>
            /// <para>If the credential Type is `<see cref="CredentialType.Generic"/>`, this member 
            /// can be non-NULL, but the credential manager ignores the member.</para>
            /// <para>This member cannot be longer than CRED_MAX_USERNAME_LENGTH(513) characters.</para>
            /// </summary>
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UserName;
        }
        #endregion
        #region kernel32.dll
        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        /// <returns>True is successful; otherwise false.</returns>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "CloseHandle", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        /// <returns>True is successful; otherwise false.</returns>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "CloseHandle", SetLastError = true)]
        public static extern bool CloseHandle(SafeHandle handle);

        /// <summary>
        /// Creates or opens a file or I/O device. The most commonly used I/O devices are as 
        /// follows: file, file stream, directory, physical disk, volume, console buffer, tape 
        /// drive, communications resource, mailslot, and pipe. The function returns a handle that 
        /// can be used to access the file or device for various types of I/O depending on the file 
        /// or device and the flags and attributes specified.
        /// </summary>
        /// <param name="fileName">
        /// <para>The name of the file or device to be created or opened. You may use either 
        /// forward slashes (/) or backslashes (\) in this name.</para>
        /// <para>In the ANSI version of this function, the name is limited to MAX_PATH characters. 
        /// To extend this limit to 32,767 wide characters, call the Unicode version of the 
        /// function and prepend "\\?\" to the path.</para>
        /// </param>
        /// <param name="desiredAccess">
        /// <para>The requested access to the file or device, which can be summarized as read, 
        /// write, both or neither zero).</para>
        /// <para>f this parameter is zero, the application can query certain metadata such as file, 
        /// directory, or device attributes without accessing that file or device, even if 
        /// <see cref="FileAccess.GenericRead"/> access would have been denied.</para>
        /// <para>You cannot request an access mode that conflicts with the sharing mode that is 
        /// specified by the <paramref name="sharedMode"/> parameter in an open request that 
        /// already has an open handle.</para>
        /// </param>
        /// <param name="shareMode">
        /// <para>The requested sharing mode of the file or device, which can be read, write, both, 
        /// delete, all of these, or none (refer to the following table). Access requests to 
        /// attributes or extended attributes are not affected by this flag.</para>
        /// <para>If this parameter is zero and CreateFile succeeds, the file or device cannot be 
        /// shared and cannot be opened again until the handle to the file or device is closed.</para>
        /// <para>You cannot request a sharing mode that conflicts with the access mode that is 
        /// specified in an existing request that has an open handle. CreateFile would fail and 
        /// the <see cref="Marshal.GetLastWin32Error"/> function would return 
        /// <see cref="Win32Error.SharingViloation"/>.</para>
        /// <para>To enable a process to share a file or device while another process has the file 
        /// or device open, use a compatible combination of one or more of the following values.</para>
        /// </param>
        /// <param name="securityAttributes">This parameter should be <see cref="IntPtr.Zero"/>.</param>
        /// <param name="creationDisposition">
        /// <para>An action to take on a file or device that exists or does not exist.</para>
        /// <para>For devices other than files, this parameter is usually set to <see cref="FileCreationDisposition.OpenExisting"/>.</para>
        /// </param>
        /// <param name="flagsAndAttributes">
        /// <para>The file or device attributes and flags, <see cref="FileAttributes.Normal"/> 
        /// being the most common default value for files.</para>
        /// <para>This parameter can include any combination of <see cref="FileAttributes"/>. All 
        /// other file attributes override <see cref="FileAttributes.Normal"/>.</para>
        /// </param>
        /// <param name="templateFile">This parameter should be <see cref="IntPtr.Zero"/>.</param>
        /// <returns>A handle to the file created.</returns>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "CreateFileW", SetLastError = true)]
        public static extern SafeFileHandle CreateFile(string fileName, FileAccess desiredAccess, FileShare shareMode, IntPtr securityAttributes, FileCreationDisposition creationDisposition, FileAttributes flagsAndAttributes, IntPtr templateFile);

        /// <summary>
        /// Retrieves the current input mode of a console's input buffer or the current output mode 
        /// of a console screen buffer.
        /// </summary>
        /// <param name="consoleHandle">
        /// A handle to the console input buffer or the console screen buffer. The handle must have 
        /// the <see cref="FileAccess.GenericRead"/> access right.
        /// </param>
        /// <param name="consoleMode">
        /// <para>A pointer to a variable that receives the current mode of the specified buffer.</para>
        /// <para>If the <paramref name="consoleHandle"/> parameter is an input handle, the mode 
        /// can be one or more of the following values. When a console is created, all input modes 
        /// except <see cref="ConsoleMode.WindowInput"/> are enabled by default.</para>
        /// <para>If the <paramref name="consoleHandle"/> parameter is a screen buffer handle, the 
        /// mode can be one or more of the following values. When a screen buffer is created, both 
        /// output modes are enabled by default.</para>
        /// </param>
        /// <returns>True if success; otherwise false.</returns>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "GetConsoleMode", SetLastError = true)]
        public static extern bool GetConsoleMode(SafeFileHandle consoleHandle, out ConsoleMode consoleMode);

        /// <summary>
        /// Reads character input from the console input buffer and removes it from the buffer.
        /// </summary>
        /// <param name="consoleInputHandle">
        /// A handle to the console input buffer. The handle must have the <see cref="FileAccess.GenericRead"/> access right.
        /// </param>
        /// <param name="buffer">
        /// <para>A pointer to a buffer that receives the data read from the console input buffer.</para>
        /// <para>The storage for this buffer is allocated from a shared heap for the process that is 64 KB in size. The maximum size of the buffer will depend on heap usage.</para>
        /// </param>
        /// <param name="numberOfCharsToRead">
        /// The number of characters to be read. The size of the buffer pointed to by the <paramref name="buffer"/> parameter should be at least <paramref name="NumberofCharsToRead"/> * sizeof(<see cref="char"/>) bytes.
        /// </param>
        /// <param name="numberOfCharsRead">
        /// A pointer to a variable that receives the number of characters actually read.
        /// </param>
        /// <param name="reserved">Reserved; must be <see cref="IntPtr.Zero"/>.</param>
        /// <returns>True if success; otherwise false.</returns>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "ReadConsoleW", SetLastError = true)]
        public static extern bool ReadConsole(SafeFileHandle consoleInputHandle, [Out]StringBuilder buffer, uint numberOfCharsToRead, out uint numberOfCharsRead, IntPtr reserved);

        /// <summary>
        /// Sets the input mode of a console's input buffer or the output mode of a console screen 
        /// buffer.
        /// </summary>
        /// <param name="consoleHandle">
        /// A handle to the console input buffer or a console screen buffer. The handle must have 
        /// the <see cref="FileAccess.GenericRead"/> access right. 
        /// </param>
        /// <param name="consoleMode">
        /// <para>The input or output mode to be set. If the <paramref name="consoleHandle"/> 
        /// parameter is an input handle, the mode can be one or more of the following values. When 
        /// a console is created, all input modes except <see cref="ConsoleMode.WindowInput"/> are 
        /// enabled by default.</para>
        /// <para>If the <paramref name="consoleHandle"/> parameter is a screen buffer handle, the 
        /// mode can be one or more of the following values. When a screen buffer is created, both 
        /// output modes are enabled by default.</para>
        /// </param>
        /// <returns>True if success; otherwise false.</returns>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "SetConsoleMode", SetLastError = true)]
        public static extern bool SetConsoleMode(SafeFileHandle consoleHandle, ConsoleMode consoleMode);

        /// <summary>
        /// Writes a character string to a console screen buffer beginning at the current cursor 
        /// location.
        /// </summary>
        /// <param name="consoleOutputHandle">A handle to the console screen buffer. The handle 
        /// must have the <see cref="FileAccess.GenericWrite"/> access right.</param>
        /// <param name="buffer">
        /// <para>A pointer to a buffer that contains characters to be written to the console 
        /// screen buffer.</para>
        /// <para>The storage for this buffer is allocated from a shared heap for the process that 
        /// is 64 KB in size. The maximum size of the buffer will depend on heap usage.</para>
        /// </param>
        /// <param name="numberOfCharsToWrite">
        /// The number of characters to be written. If the total size of the specified number of 
        /// characters exceeds the available heap, the function fails with <see cref="Win32Error.NotEnoughMemory"/>.
        /// </param>
        /// <param name="numberOfCharsWritten">
        /// A pointer to a variable that receives the number of characters actually written.
        /// </param>
        /// <param name="reserved">Reserved; must be <see cref="IntPtr.Zero"/>.</param>
        /// <returns>True if success; otherwise false.</returns>
        [DllImport(Kernel32, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, EntryPoint = "WriteConsoleW", SetLastError = true)]
        public static extern bool WriteConsole(SafeHandle consoleOutputHandle, [In]StringBuilder buffer, uint numberOfCharsToWrite, out uint numberOfCharsWritten, IntPtr reserved);

        [Flags]
        public enum ConsoleMode : uint
        {
            /// <summary>
            /// CTRL+C is processed by the system and is not placed in the input buffer. If the 
            /// input buffer is being read by <see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/>, 
            /// other control keys are processed by the system and are not returned in the 
            /// ReadConsole buffer. If the <see cref="LineInput"/> mode is also enabled, backspace, 
            /// carriage return, and line feed characters are handled by the system.
            /// </summary>
            ProcessedInput = 0x0001,
            /// <summary>
            /// The <see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/> 
            /// function returns only when a carriage return character is read. If this mode is 
            /// disabled, the functions return when one or more characters are available.
            /// </summary>
            LineInput = 0x0002,
            /// <summary>
            /// Characters read by the <see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/> 
            /// function are written to the active screen buffer as they are read. This mode can be 
            /// used only if the <see cref="LineInput"/> mode is also enabled.
            /// </summary>
            EchoInput = 0x0004,
            /// <summary>
            /// User interactions that change the size of the console screen buffer are reported in 
            /// the console's input buffer. Information about these events can be read from the 
            /// input buffer by applications using the ReadConsoleInput function, but not by those 
            /// using <see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/>.
            /// </summary>
            WindowInput = 0x0008,
            /// <summary>
            /// If the mouse pointer is within the borders of the console window and the window has 
            /// the keyboard focus, mouse events generated by mouse movement and button presses are 
            /// placed in the input buffer. These events are discarded by <see cref="ReadConsole(SafeFileHandle, StringBuilder, uint, out uint, IntPtr)"/>, 
            /// even when this mode is enabled.
            /// </summary>
            MouseInput = 0x0010,
            /// <summary>
            /// When enabled, text entered in a console window will be inserted at the current 
            /// cursor location and all text following that location will not be overwritten. When 
            /// disabled, all following text will be overwritten.
            /// </summary>
            InsertMode = 0x0020,
            /// <summary>
            /// This flag enables the user to use the mouse to select and edit text.
            /// </summary>
            QuickEdit = 0x0040,

            /// <summary>
            /// Characters written by the <see cref="WriteConsole(SafeHandle, StringBuilder, uint, out uint, IntPtr)"/> 
            /// function or echoed by the ReadFile or ReadConsole function are parsed for ASCII 
            /// control sequences, and the correct action is performed. Backspace, tab, bell, 
            /// carriage return, and line feed characters are processed.
            /// </summary>
            ProcessedOuput = 0x0001,
            /// <summary>
            /// When writing with <see cref="WriteConsole(SafeHandle, StringBuilder, uint, out uint, IntPtr)"/> 
            /// or echoing with ReadFile or ReadConsole, the cursor moves to the beginning of the 
            /// next row when it reaches the end of the current row. This causes the rows displayed 
            /// in the console window to scroll up automatically when the cursor advances beyond 
            /// the last row in the window. It also causes the contents of the console screen 
            /// buffer to scroll up (discarding the top row of the console screen buffer) when the 
            /// cursor advances beyond the last row in the console screen buffer. If this mode is 
            /// disabled, the last character in the row is overwritten with any subsequent 
            /// characters.
            /// </summary>
            WrapAtEolOutput = 0x0002,
        }

        [Flags]
        public enum FileAccess : uint
        {
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            GenericExecute = 0x20000000,
            GenericAll = 0x10000000,
        }

        [Flags]
        public enum FileAttributes : uint
        {
            /// <summary>
            /// The file is read only. Applications can read the file, but cannot write to or 
            /// delete it.
            /// </summary>
            Readonly = 0x00000001,
            /// <summary>
            /// The file is hidden. Do not include it in an ordinary directory listing.
            /// </summary>
            Hidden = 0x00000002,
            /// <summary>
            /// The file is part of or used exclusively by an operating system.
            /// </summary>
            System = 0x00000004,
            Directory = 0x00000010,
            /// <summary>
            /// The file should be archived. Applications use this attribute to mark files for 
            /// backup or removal.
            /// </summary>
            Archive = 0x00000020,
            Device = 0x00000040,
            /// <summary>
            /// The file does not have other attributes set. This attribute is valid only if used 
            /// alone.
            /// </summary>
            Normal = 0x00000080,
            /// <summary>
            /// The file is being used for temporary storage.
            /// </summary>
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            /// <summary>
            /// The data of a file is not immediately available. This attribute indicates that file 
            /// data is physically moved to offline storage. This attribute is used by Remote 
            /// Storage, the hierarchical storage management software. Applications should not 
            /// arbitrarily change this attribute.
            /// </summary>
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            /// <summary>
            /// <para>The file or directory is encrypted. For a file, this means that all data in 
            /// the file is encrypted. For a directory, this means that encryption is the default 
            /// for newly created files and subdirectories.</para>
            /// <para>This flag has no effect if <see cref="Archive"/> is also specified.</para>
            /// <para>This flag is not supported on Home, Home Premium, Starter, or ARM editions of 
            /// Windows.</para>
            /// </summary>
            Encrypted = 0x00004000,
            FirstPipeInstance = 0x00080000,
            /// <summary>
            /// The file data is requested, but it should continue to be located in remote storage. 
            /// It should not be transported back to local storage. This flag is for use by remote 
            /// storage systems.
            /// </summary>
            OpenNoRecall = 0x00100000,
            /// <summary>
            /// <para>Normal reparse point processing will not occur; <see cref="CreateFile(string, FileAccess, FileShare, IntPtr, FileCreationDisposition, FileAttributes, IntPtr)"/> 
            /// will attempt to open the reparse point. When a file is opened, a file handle is 
            /// returned, whether or not the filter that controls the reparse point is operational.</para>
            /// <para>This flag cannot be used with the <see cref="FileCreationDisposition.CreateAlways"/> 
            /// flag.</para>
            /// <para>If the file is not a reparse point, then this flag is ignored.</para>
            /// </summary>
            OpenReparsePoint = 0x00200000,
            /// <summary>
            /// The file or device is being opened with session awareness. If this flag is not 
            /// specified, then per-session devices (such as a redirected USB device) cannot be 
            /// opened by processes running in session 0. This flag has no effect for callers not 
            /// in session 0. This flag is supported only on server editions of Windows.
            /// </summary>
            SessionAware = 0x00800000,
            /// <summary>
            /// Access will occur according to POSIX rules. This includes allowing multiple files 
            /// with names, differing only in case, for file systems that support that naming. Use 
            /// care when using this option, because files created with this flag may not be 
            /// accessible by applications that are written for MS-DOS or 16-bit Windows.
            /// </summary>
            PosixSemantics = 0x01000000,
            /// <summary>
            /// <para>The file is being opened or created for a backup or restore operation. The 
            /// system ensures that the calling process overrides file security checks when the 
            /// process has SE_BACKUP_NAME and SE_RESTORE_NAME privileges.</para>
            /// <para>You must set this flag to obtain a handle to a directory. A directory handle 
            /// can be passed to some functions instead of a file handle.</para>
            /// </summary>
            BackupSemantics = 0x02000000,
            /// <summary>
            /// <para>The file is to be deleted immediately after all of its handles are closed, 
            /// which includes the specified handle and any other open or duplicated handles.</para>
            /// <para>If there are existing open handles to a file, the call fails unless they were 
            /// all opened with the <see cref="FileShare.Delete"/> share mode.</para>
            /// <para>Subsequent open requests for the file fail, unless the 
            /// <see cref="FileShare.Delete"/> share mode is specified.</para>
            /// </summary>
            DeleteOnClose = 0x04000000,
            /// <summary>
            /// <para>Access is intended to be sequential from beginning to end. The system can use 
            /// this as a hint to optimize file caching.</para>
            /// <para>This flag should not be used if read-behind (that is, reverse scans) will be 
            /// used.</para>
            /// <para>This flag has no effect if the file system does not support cached I/O and 
            /// <see cref="NoBuffering"/>.</para>
            /// </summary>
            SequentialScan = 0x08000000,
            /// <summary>
            /// <para>Access is intended to be random. The system can use this as a hint to 
            /// optimize file caching.</para>
            /// <para>This flag has no effect if the file system does not support cached I/O and 
            /// <see cref="NoBuffering"/>.</para>
            /// </summary>
            RandomAccess = 0x10000000,
            /// <summary>
            /// <para>The file or device is being opened with no system caching for data reads and 
            /// writes. This flag does not affect hard disk caching or memory mapped files.</para>
            /// <para>There are strict requirements for successfully working with files opened with 
            /// <see cref="CreateFile(string, FileAccess, FileShare, IntPtr, FileCreationDisposition, FileAttributes, IntPtr)"/> 
            /// using the <see cref="NoBuffering"/> flag.</para>
            /// </summary>
            NoBuffering = 0x20000000,
            /// <summary>
            /// <para>The file or device is being opened or created for asynchronous I/O.</para>
            /// <para>When subsequent I/O operations are completed on this handle, the event 
            /// specified in the OVERLAPPED structure will be set to the signaled state.</para>
            /// <para>If this flag is specified, the file can be used for simultaneous read and 
            /// write operations.</para>
            /// <para>If this flag is not specified, then I/O operations are serialized, even if 
            /// the calls to the read and write functions specify an OVERLAPPED structure.</para>
            /// </summary>
            Overlapped = 0x40000000,
            /// <summary>
            /// Write operations will not go through any intermediate cache, they will go directly 
            /// to disk.
            /// </summary>
            WriteThrough = 0x80000000,
        }

        public enum FileCreationDisposition : uint
        {
            /// <summary>
            /// <para>Creates a new file, only if it does not already exist.</para>
            /// <para>If the specified file exists, the function fails and the last-error code is 
            /// set to <see cref="Win32Error.FileExists"/>.</para>
            /// <para>If the specified file does not exist and is a valid path to a writable 
            /// location, a new file is created.</para>
            /// </summary>
            New = 1,
            /// <summary>
            /// <para>Creates a new file, always.</para>
            /// <para>If the specified file exists and is writable, the function overwrites the 
            /// file, the function succeeds, and last-error code is set to <see cref="Win32Error.AlreadExists"/>.</para>
            /// <para>If the specified file does not exist and is a valid path, a new file is 
            /// created, the function succeeds, and the last-error code is set to zero.</para>
            /// </summary>
            CreateAlways = 2,
            /// <summary>
            /// <para>Opens a file, always.</para>
            /// <para>If the specified file exists, the function succeeds and the last-error code 
            /// is set to <see cref="Win32Error.AlreadExists"/>.</para>
            /// <para>If the specified file does not exist and is a valid path to a writable 
            /// location, the function creates a file and the last-error code is set to zero.</para>
            /// </summary>
            OpenExisting = 3,
            /// <summary>
            /// <para>Opens a file or device, only if it exists.</para> 
            /// <para>If the specified file or device does not exist, the function fails and the 
            /// last-error code is set to <see cref="Win32Error.FileNotFound"/>.</para> 
            /// </summary>
            OpenAlways = 4,
            /// <summary>
            /// <para>Opens a file and truncates it so that its size is zero bytes, only if it 
            /// exists.</para> 
            /// <para>If the specified file does not exist, the function fails and the last-error 
            /// code is set to <see cref="Win32Error.FileNotFound"/>.</para>
            /// <para>The calling process must open the file with <see cref="FileAccess.GenericWrite"/>.</para>
            /// </summary>
            TruncateExisting = 5
        }

        [Flags]
        public enum FileShare : uint
        {
            /// <summary>
            /// Prevents other processes from opening a file or device if they request delete, read, 
            /// or write access.
            /// </summary>
            None = 0x00000000,
            /// <summary>
            /// <para>Enables subsequent open operations on an object to request read access.</para> 
            /// <para>Otherwise, other processes cannot open the object if they request read access.</para>
            /// <para>If this flag is not specified, but the object has been opened for read access, 
            /// the function fails.</para>
            /// </summary>
            Read = 0x00000001,
            /// <summary>
            /// <para>Enables subsequent open operations on an object to request write access.</para> 
            /// <para>Otherwise, other processes cannot open the object if they request write 
            /// access.</para>  
            /// <para>If this flag is not specified, but the object has been opened for write 
            /// access, the function fails.</para> 
            /// </summary>
            Write = 0x00000002,
            /// <summary>
            /// <para>Enables subsequent open operations on an object to request delete access.</para> 
            /// <para>Otherwise, other processes cannot open the object if they request delete 
            /// access.</para> 
            /// <para>If this flag is not specified, but the object has been opened for delete 
            /// access, the function fails.</para> 
            /// </summary>
            Delete = 0x00000004
        }
        #endregion

        /// <summary>
        /// The System Error Codes are very broad. Each one can occur in one of many hundreds of 
        /// locations in the system. Consequently the descriptions of these codes cannot be very 
        /// specific. Use of these codes requires some amount of investigation and analysis. You 
        /// need to note both the programmatic and the run-time context in which these errors occur. 
        /// Because these codes are defined in WinError.h for anyone to use, sometimes the codes 
        /// are returned by non-system software. Sometimes the code is returned by a function deep 
        /// in the stack and far removed from your code that is handling the error.
        /// </summary>
        internal static class Win32Error
        {
            /// <summary>
            /// The system cannot find the file specified.
            /// </summary>
            public const int FileNotFound = 2;
            /// <summary>
            /// The handle is invalid.
            /// </summary>
            public const int InvalidHandle = 6;
            /// <summary>
            /// Not enough storage is available to process this command.
            /// </summary>
            public const int NotEnoughMemory = 8;
            /// <summary>
            /// The process cannot access the file because it is being used by another process.
            /// </summary>
            public const int SharingViloation = 32;
            /// <summary>
            /// The file exists.
            /// </summary>
            public const int FileExists = 80;
            /// <summary>
            /// Cannot create a file when that file already exists.
            /// </summary>
            public const int AlreadExists = 183;
            /// <summary>
            /// Element not found.
            /// </summary>
            public const int NotFound = 1168;
            /// <summary>
            /// A specified logon session does not exist. It may already have been terminated.
            /// </summary>
            public const int NoSuchLogonSession = 1312;
        }
    }
}

