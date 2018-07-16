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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Alm.Win32;
using Microsoft.Win32;
using static System.StringComparer;
using static System.Diagnostics.Debug;

namespace Microsoft.Alm.Authentication
{
    public interface IStorage : IRuntimeService
    {
        /// <summary>
        /// Creates all directories and subdirectories in the specified path.
        /// </summary>
        /// <param name="path">The directory path to create.</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Determines whether the given path refers to an existing directory on disk.
        /// <para/>
        /// Returns `<see langword="true"/>` if path refers to an existing directory; otherwise, `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">The path to test.</param>
        bool DirectoryExists(string path);

        /// <summary>
        ///  Returns an enumerable collection of file names and directory names that match a search pattern in a specified path, and optionally searches subdirectories.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="pattern">
        /// The search string to match against the names of directories in path
        /// </param>
        /// <param name="options">
        /// Include only the current directory or should include all subdirectories.
        /// <para/>
        /// The default value is `<see cref="SearchOption.TopDirectoryOnly"/>`.
        /// </param>
        IEnumerable<string> EnumerateFileSystemEntries(string path, string pattern, SearchOption options);

        /// <summary>
        /// Returns an enumerable collection of file-system entries in a specified path.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        IEnumerable<string> EnumerateFileSystemEntries(string path);

        /// <summary>
        /// Returns an enumerable collection of decrypted secrets, from the operating system's secure store, filtered by `<paramref name="prefix"/>`.
        /// </summary>
        /// <param name="prefix">
        /// Value that any secure store entry's key must start with.
        /// <para/>
        /// Value can be `<see langword="null"/>`.
        /// </param>
        IEnumerable<SecureData> EnumerateSecureData(string prefix);

        /// <summary>
        /// Copies an existing file to a new file.
        /// <para/>
        /// Overwriting a file of the same name is not allowed.
        /// </summary>
        /// <param name="sourcePath">The file to copy.</param>
        /// <param name="targetPath">
        /// The name of the destination file.
        /// <para/>
        /// This cannot be a directory or an existing file.
        /// </param>
        void FileCopy(string sourcePath, string targetPath, bool overwrite);

        /// <summary>
        /// Copies an existing file to a new file.
        /// </summary>
        /// <param name="sourcePath">The file to copy.</param>
        /// <param name="targetPath">
        /// The name of the destination file.
        /// <para/>
        /// This cannot be a directory.
        /// </param>
        /// <param name="overwrite">
        /// `<see langword="true"/>` if the destination file can be overwritten; otherwise, `<see langword="false"/>`.
        /// </param>
        void FileCopy(string sourcePath, string targetPath);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">
        /// The name of the file to be deleted.
        /// <para/>
        /// Wildcard characters are not supported.
        /// </param>
        void FileDelete(string path);

        /// <summary>
        /// Determines whether the specified file exists.
        /// <para/>
        /// `<see langword="true"/>` if the caller has the required permissions and path contains the name of an existing file; otherwise, `<see langword="false"/>`.
        /// <para/>
        /// Returns `<see langword="false"/>` if `<paramref name="path"/>` is `<see langword="null"/>`, an invalid path, or a zero-length `<see langword="string"/>`.
        /// <para/>
        /// Returns `<see langword="false"/>` if the caller does not have sufficient permissions to read the specified file, no exception is thrown.
        /// <para/>
        /// </summary>
        /// <param name="path">The file to check.</param>
        bool FileExists(string path);

        /// <summary>
        /// Returns a `<see cref="Stream"/>` specified by `<paramref name="path"/>`, having the specified mode with read, write, or read/write access and the specified sharing option.
        /// </summary>
        /// <param name="path">The file to open.</param>
        /// <param name="mode">
        /// A `<see cref="FileMode"/>` value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.
        /// </param>
        /// <param name="access">
        /// A `<see cref="FileAccess"/>` value that specifies the operations that can be performed on the file.
        /// </param>
        /// <param name="share">
        /// A `<see cref="FileShare value"/>` specifying the type of access other threads have to the file.
        /// </param>
        Stream FileOpen(string path, FileMode mode, FileAccess access, FileShare share);

        /// <summary>
        /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
        /// <para/>
        /// Returns a byte array containing the contents of the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        byte[] FileReadAllBytes(string path);

        /// <summary>
        /// Opens a file, reads all lines of the file with the specified encoding, and then closes the file.
        /// <para/>
        /// Returns all of text contents of the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <param name="encoding">The encoding applied to the contents of the file.</param>
        string FileReadAllText(string path, System.Text.Encoding encoding);

        /// <summary>
        /// Opens a file, reads all lines of the file with UTF-8 encoding, and then closes the file.
        /// <para/>
        /// Returns all of text contents of the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        string FileReadAllText(string path);

        /// <summary>
        /// Creates a new file, writes the specified byte array to the file, and then closes the file.
        /// <para/>
        /// If `<paramref name="path"/>` already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="bytes">The bytes to write to the file.</param>
        void FileWriteAllBytes(string path, byte[] data);

        /// <summary>
        /// Creates a new file, writes the specified string to the file using the specified encoding, and then closes the file.
        /// <para/>
        /// If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file.</param>
        /// <param name="encoding">The encoding to apply to the string.</param>
        void FileWriteAllText(string path, string contents, System.Text.Encoding encoding);

        /// <summary>
        /// Creates a new file, writes the specified string to the file using UTF-8 encoding, and then closes the file.
        /// <para/>
        /// If the target file already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="contents">The string to write to the file.</param>
        void FileWriteAllText(string path, string contents);

        /// <summary>
        /// Returns an array of paths to all known drive roots.
        /// </summary
        string[] GetDriveRoots();

        /// <summary>
        /// Returns the file name and extension of the specified path string.
        /// <para/>
        /// If the last character f path is a directory or volume separator character, this method returns `<see cref="string.Empty"/>`.
        /// <para/>
        /// If `<paramref name="path"/>` is `<see langword="null"/>`, this method returns `<see langword="null"/>`.
        /// </summary>
        /// <param name="path">
        /// The path string from which to obtain the file name and extension.
        /// </param>
        string GetFileName(string path);

        /// <summary>
        /// Returns the absolute path for the specified path string.
        /// </summary>
        /// <param name="path">
        /// The file or directory for which to obtain absolute path information.
        /// </param>
        string GetFullPath(string path);

        /// <summary>
        /// Returns the directory information for the specified path string.
        /// <para/>
        /// Returns `<see cref="string.Empty"/>` if `<paramref name="path"/>` does not contain directory information.
        /// </summary>
        /// <param name="path">The path of a file or directory.</param>
        string GetParent(string path);

        /// <summary>
        /// Opens `<paramref name="registryHive"/>` on the local machine with `<paramref name="registryView"/>`, and retrieves the value associated with `<paramref name="registryPath"/>` and `<paramref name="keyName"/>` provided.
        /// <para/>
        /// Returns the requested value as a `<see cref="string"/>` if possible; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="registryHive">The HKEY to open.</param>
        /// <param name="registryView">The registry view to use.</param>
        /// <param name="registryPath">The name or path of the subkey to open as read-only.</param>
        /// <param name="keyName">
        /// The name of the value to retrieve.
        /// <para/>
        /// This string is not case-sensitive.
        /// </param>
        string RegistryReadString(RegistryHive registryHive, RegistryView registryView, string registryPath, string keyName);

        /// <summary>
        /// Opens `<paramref name="registryHive"/>` on the local machine with the default view, and retrieves the value associated with `<paramref name="registryPath"/>` and `<paramref name="keyName"/>` provided.
        /// <para/>
        /// Returns the requested value as a `<see cref="string"/>` if possible; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="registryHive">The HKEY to open.</param>
        /// <param name="registryPath">The name or path of the subkey to open as read-only.</param>
        /// <param name="keyName">
        /// The name of the value to retrieve.
        /// <para/>
        /// This string is not case-sensitive.
        /// </param>
        string RegistryReadString(RegistryHive registryHive, string registryPath, string keyName);

        /// <summary>
        /// Opens Current User registry on the local machine with the default view, and retrieves the value associated with `<paramref name="registryPath"/>` and `<paramref name="keyName"/>` provided.
        /// <para/>
        /// Returns the requested value as a `<see cref="string"/>` if possible; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="registryPath">The name or path of the subkey to open as read-only.</param>
        /// <param name="keyName">
        /// The name of the value to retrieve.
        /// <para/>
        /// This string is not case-sensitive.
        /// </param>
        string RegistryReadString(string registryPath, string keyName);

        /// <summary>
        /// Deletes any, and all, entries from the operating system's secure storage with keys starting with `<paramref name="prefix"/>`.
        /// <para/>
        /// Returns the count of entries deleted.
        /// </summary>
        /// <param name="prefix">
        /// Value that any secure store entry's key must start with.
        /// <para/>
        /// Value can be `<see langword="null"/>`.
        /// </param>
        int TryPurgeSecureData(string prefix);

        /// <summary>
        /// Reads data from the operating system's secure storage.
        /// <para/>
        /// Data written to the store is uniquely identified by the associated `<paramref name="key"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="key">The key used to uniquely identify the data in the secure store.</param>
        /// <param name="name">
        /// The 'name' associated with the key, often a username.
        /// <para/>
        /// This information might not be encrypted by the operating system.
        /// </param>
        /// <param name="data">
        /// The encrypted data, to be decrypted, to be read from the operating systems secure storage.
        /// <para/>
        /// This information is encrypted, and will be decrypted, by the operating system.
        /// </param>
        bool TryReadSecureData(string key, out string name, out byte[] data);

        /// <summary>
        /// Writes data to the operating system's secure storage.
        /// <para/>
        /// Data written to the store is uniquely identified by the associated `<paramref name="key"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="key">The key used to uniquely identify the data in the secure store.</param>
        /// <param name="name">
        /// The 'name' associated with the key, often a username.
        /// <para/>
        /// This information might not be encrypted by the operating system.
        /// </param>
        /// <param name="data">
        /// The data, to be encrypted, to be written to the operating systems secure storage.
        /// <para/>
        /// This information will be encrypted by the operating system.
        /// </param>
        bool TryWriteSecureData(string key, string name, byte[] data);
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct SecureData : IEquatable<SecureData>
    {
        internal static readonly SecureDataComparer Comparer = SecureDataComparer.Instance;

        internal SecureData(string key, string name, byte[] data)
        {
            _data = data;
            _key = key;
            _name = name;
        }

        private byte[] _data;
        private string _key;
        private string _name;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public byte[] Data
        {
            get { return _data; }
        }

        public string Key
        {
            get { return _key; }
        }

        public string Name
        {
            get { return _name; }
        }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(SecureData)}: \"{_key}\" => \"{_name}\" : [{_data.Length}]"; }
        }

        public bool Equals(SecureData other)
            => Comparer.Equals(this, other);

        public override bool Equals(object obj)
        {
            return (obj is SecureData a && Equals(a))
                || base.Equals(obj);
        }

        public override int GetHashCode()
            => Comparer.GetHashCode(this);

        public static bool operator ==(SecureData lhs, SecureData rhs)
            => Comparer.Equals(lhs, rhs);

        public static bool operator !=(SecureData lhs, SecureData rhs)
            => !Comparer.Equals(lhs, rhs);
    }

    internal class SecureDataComparer : IEqualityComparer<SecureData>
    {
        public static readonly SecureDataComparer Instance = new SecureDataComparer();

        private SecureDataComparer()
        { }

        public bool Equals(SecureData lhs, SecureData rhs)
        {
            if (lhs.Data?.Length != rhs.Data?.Length
                || !Ordinal.Equals(lhs.Key, rhs.Key)
                || !Ordinal.Equals(lhs.Name, rhs.Name))
                return false;

            if (lhs.Data is null && rhs.Data is null)
                return true;

            if (lhs.Data is null || rhs.Data is null)
                return false;

            for (int i = 0; i < lhs.Data.Length; i += 1)
            {
                if (lhs.Data[i] != rhs.Data[i])
                    return false;
            }

            return true;
        }

        public int GetHashCode(SecureData obj)
        {
            int keyHash = obj.Key?.GetHashCode() ?? 0;
            int nameHash = obj.Name?.GetHashCode() ?? 0;

            unchecked
            {
                return (keyHash & (int)0xFFFF0000) | (nameHash & 0x0000FFFF);
            }
        }
    }

    internal class Storage : Base, IStorage
    {
        public Storage(RuntimeContext context)
            : base(context)
        { }

        public Type ServiceType
            => typeof(IStorage);

        public void CreateDirectory(string path)
            => Directory.CreateDirectory(path);

        public bool DirectoryExists(string path)
            => Directory.Exists(path);

        public IEnumerable<string> EnumerateFileSystemEntries(string path, string pattern, SearchOption options)
            => Directory.EnumerateFileSystemEntries(path, pattern, options);

        public IEnumerable<string> EnumerateFileSystemEntries(string path)
            => EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);

        public IEnumerable<SecureData> EnumerateSecureData(string prefix)
        {
            string filter = prefix ?? string.Empty + "*";

            if (NativeMethods.CredEnumerate(filter, 0, out int count, out IntPtr credentialArrayPtr))
            {
                Trace.WriteLine($"{count} credentials enumerated from secret store.");

                try
                {
                    for (int i = 0; i < count; i += 1)
                    {
                        int offset = i * Marshal.SizeOf(typeof(IntPtr));
                        IntPtr credentialPtr = Marshal.ReadIntPtr(credentialArrayPtr, offset);

                        if (credentialPtr != IntPtr.Zero)
                        {
                            NativeMethods.Credential credStruct = Marshal.PtrToStructure<NativeMethods.Credential>(credentialPtr);
                            int passwordLength = credStruct.CredentialBlobSize;

                            byte[] data = new byte[credStruct.CredentialBlobSize];
                            Marshal.Copy(credStruct.CredentialBlob, data, 0, credStruct.CredentialBlobSize);

                            string name = credStruct.UserName ?? string.Empty;
                            string key = credStruct.TargetName;

                            yield return new SecureData(key, name, data);
                        }
                    }
                }
                finally
                {
                    NativeMethods.CredFree(credentialArrayPtr);
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                if (error != NativeMethods.Win32Error.FileNotFound
                    && error != NativeMethods.Win32Error.NotFound)
                {
                    Fail($"Failed with error code 0x{error.ToString("X8")}.");
                }
            }

            yield break;
        }

        public void FileCopy(string sourcePath, string destinationPath)
            => FileCopy(sourcePath, destinationPath, false);

        public void FileCopy(string sourcePath, string destinationPath, bool overwrite)
            => File.Copy(sourcePath, destinationPath, overwrite);

        public void FileDelete(string path)
            => File.Delete(path);

        public bool FileExists(string path)
            => File.Exists(path);

        public Stream FileOpen(string path, FileMode mode, FileAccess access, FileShare share)
            => File.Open(path, mode, access, share);

        public byte[] FileReadAllBytes(string path)
            => File.ReadAllBytes(path);

        public string FileReadAllText(string path, System.Text.Encoding encoding)
            => File.ReadAllText(path, encoding);

        public string FileReadAllText(string path)
            => FileReadAllText(path, System.Text.Encoding.UTF8);

        public void FileWriteAllBytes(string path, byte[] bytes)
            => File.WriteAllBytes(path, bytes);

        public void FileWriteAllText(string path, string contents, System.Text.Encoding encoding)
            => File.WriteAllText(path, contents, encoding);

        public void FileWriteAllText(string path, string contents)
            => FileWriteAllText(path, contents, System.Text.Encoding.UTF8);

        public string[] GetDriveRoots()
        {
            var drives = DriveInfo.GetDrives();
            var paths = new string[drives.Length];

            for (int i = 0; i < drives.Length; i += 1)
            {
                paths[i] = drives[i].RootDirectory.FullName;
            }

            return paths;
        }

        public string GetFileName(string path)
            => Path.GetFileName(path);

        public string GetFullPath(string path)
            => Path.GetFullPath(path);

        public string GetParent(string path)
            => Path.GetDirectoryName(path);

        public string RegistryReadString(RegistryHive registryHive, RegistryView registryView, string registryPath, string keyName)
        {
            if (registryPath is null)
                throw new ArgumentNullException(nameof(registryPath));
            if (keyName is null)
                throw new ArgumentNullException(nameof(keyName));

            using (var baseKey = RegistryKey.OpenBaseKey(registryHive, registryView))
            using (var dataKey = baseKey?.OpenSubKey(registryPath))
            {
                return dataKey?.GetValue(keyName, null) as string;
            }
        }

        public string RegistryReadString(RegistryHive registryHive, string registryPath, string keyName)
            => RegistryReadString(registryHive, RegistryView.Default, registryPath, keyName);

        public string RegistryReadString(string registryPath, string keyName)
            => RegistryReadString(RegistryHive.CurrentUser, RegistryView.Default, registryPath, keyName);

        public int TryPurgeSecureData(string prefix)
        {
            if (prefix is null)
                throw new ArgumentNullException(nameof(prefix));

            string filter = prefix ?? string.Empty + "*";
            int purgeCount = 0;

            if (NativeMethods.CredEnumerate(filter, 0, out int count, out IntPtr credentialArrayPtr))
            {
                try
                {
                    for (int i = 0; i < count; i += 1)
                    {
                        int offset = i * Marshal.SizeOf(typeof(IntPtr));
                        IntPtr credentialPtr = Marshal.ReadIntPtr(credentialArrayPtr, offset);

                        if (credentialPtr != IntPtr.Zero)
                        {
                            NativeMethods.Credential credential = Marshal.PtrToStructure<NativeMethods.Credential>(credentialPtr);

                            if (NativeMethods.CredDelete(credential.TargetName, credential.Type, 0))
                            {
                                purgeCount += 1;
                            }
                            else
                            {
                                int error = Marshal.GetLastWin32Error();
                                if (error != NativeMethods.Win32Error.FileNotFound)
                                {
                                    Fail("Failed with error code " + error.ToString("X"));
                                }
                            }
                        }
                    }
                }
                finally
                {
                    NativeMethods.CredFree(credentialArrayPtr);
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                if (error != NativeMethods.Win32Error.FileNotFound
                    && error != NativeMethods.Win32Error.NotFound)
                {
                    Fail("Failed with error code " + error.ToString("X"));
                }
            }

            return purgeCount;
        }

        public bool TryReadSecureData(string key, out string name, out byte[] data)
        {
            const string NoSuchSessionMessage = "The logon session does not exist or there is no credential set associated with this logon session. Network logon sessions do not have an associated credential set.";

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            var credPtr = IntPtr.Zero;

            if (NativeMethods.CredRead(key, NativeMethods.CredentialType.Generic, 0, out credPtr))
            {
                try
                {
                    var credStruct = (NativeMethods.Credential)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.Credential));
                    if (credStruct.CredentialBlob != null && credStruct.CredentialBlobSize > 0)
                    {
                        int size = credStruct.CredentialBlobSize;
                        data = new byte[size];
                        Marshal.Copy(credStruct.CredentialBlob, data, 0, size);
                        name = credStruct.UserName;

                        return true;
                    }
                }
                finally
                {
                    if (credPtr != IntPtr.Zero)
                    {
                        NativeMethods.CredFree(credPtr);
                    }
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                var errorCode = (ErrorCode)error;

                if (errorCode == ErrorCode.NoSuchLogonSession)
                    throw new InvalidOperationException(NoSuchSessionMessage);
            }

            data = null;
            name = null;

            return false;
        }

        public bool TryWriteSecureData(string key, string name, byte[] data)
        {
            const string BadUsernameMessage = "The UserName member of the passed in Credential structure is not valid.";
            const string NoSuchSessionMessage = "The logon session does not exist or there is no credential set associated with this logon session. Network logon sessions do not have an associated credential set.";

            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var credential = new NativeMethods.Credential
            {
                Type = NativeMethods.CredentialType.Generic,
                TargetName = key,
                CredentialBlob = Marshal.AllocCoTaskMem(data.Length),
                CredentialBlobSize = data.Length,
                Persist = NativeMethods.CredentialPersist.LocalMachine,
                AttributeCount = 0,
                UserName = name,
            };

            try
            {
                Marshal.Copy(data, 0, credential.CredentialBlob, data.Length);

                if (NativeMethods.CredWrite(ref credential, 0))
                    return true;

                int error = Marshal.GetLastWin32Error();
                var errorCode = (ErrorCode)error;

                switch (errorCode)
                {
                    case ErrorCode.NoSuchLogonSession:
                    throw new InvalidOperationException(NoSuchSessionMessage);

                    case ErrorCode.BadUserName:
                    throw new ArgumentException(BadUsernameMessage, nameof(name));
                }
            }
            finally
            {
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(credential.CredentialBlob);
                }
            }

            return false;
        }
    }
}
