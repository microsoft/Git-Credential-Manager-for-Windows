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

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Alm.Authentication
{
    public interface IFileSystem
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
        /// ne of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.
        /// <para/>
        /// The default value is `<see cref="System.IO.SearchOption.TopDirectoryOnly"/>`.
        /// </param>
        IEnumerable<string> EnumerateFileSystemEntries(string path, string pattern, SearchOption options);

        /// <summary>
        /// Returns an enumerable collection of file-system entries in a specified path.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        IEnumerable<string> EnumerateFileSystemEntries(string path);

        /// <summary>
        /// Copies an existing file to a new file.
        /// <para/>
        /// Overwriting a file of the same name is not allowed.
        /// </summary>
        /// <param name="sourcePath">The file to copy.</param>
        /// <param name="destinationPath">
        /// The name of the destination file.
        /// <para/>
        /// This cannot be a directory or an existing file.
        /// </param>
        void FileCopy(string sourcePath, string destinationPath, bool overwrite);

        /// <summary>
        /// Copies an existing file to a new file.
        /// </summary>
        /// <param name="sourcePath">The file to copy.</param>
        /// <param name="destinationPath">
        /// The name of the destination file.
        /// <para/>
        /// This cannot be a directory.
        /// </param>
        /// <param name="overwrite">
        /// `<see langword="true"/>` if the destination file can be overwritten; otherwise, `<see langword="false"/>`.
        /// </param>
        void FileCopy(string sourcePath, string destinationPath);

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
        /// Returns a `<see cref="FileStream"/>` specified by `<paramref name="path"/>`, having the specified mode with read, write, or read/write access and the specified sharing option.
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
        FileStream FileOpen(string path, FileMode mode, FileAccess access, FileShare share);

        /// <summary>
        /// Opens a binary file, reads the contents of the file into a byte array, and then closes the file.
        /// <para/>
        /// Returns a byte array containing the contents of the file.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        byte[] FileReadAllBytes(string path);

        /// <summary>
        /// Creates a new file, writes the specified byte array to the file, and then closes the file.
        /// <para/>
        /// If `<paramref name="path"/>` already exists, it is overwritten.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="bytes">The bytes to write to the file.</param>
        void FileWriteAllBytes(string path, byte[] data);

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
    }

    internal class FileSystem : Base, IFileSystem
    {
        public FileSystem(RuntimeContext context)
            : base(context)
        { }

        public void CreateDirectory(string path)
            => Directory.CreateDirectory(path);

        public bool DirectoryExists(string path)
            => Directory.Exists(path);

        public IEnumerable<string> EnumerateFileSystemEntries(string path, string pattern, SearchOption options)
            => Directory.EnumerateFileSystemEntries(path, pattern, options);

        public IEnumerable<string> EnumerateFileSystemEntries(string path)
            => Directory.EnumerateFileSystemEntries(path);

        public void FileCopy(string sourcePath, string destinationPath)
            => File.Copy(sourcePath, destinationPath);

        public void FileCopy(string sourcePath, string destinationPath, bool overwrite)
            => File.Copy(sourcePath, destinationPath, overwrite);

        public void FileDelete(string path)
            => File.Delete(path);

        public bool FileExists(string path)
            => File.Exists(path);

        public FileStream FileOpen(string path, FileMode mode, FileAccess access, FileShare share)
            => File.Open(path, mode, access, share);

        public byte[] FileReadAllBytes(string path)
            => File.ReadAllBytes(path);

        public void FileWriteAllBytes(string path, byte[] bytes)
            => File.WriteAllBytes(path, bytes);

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
    }
}
