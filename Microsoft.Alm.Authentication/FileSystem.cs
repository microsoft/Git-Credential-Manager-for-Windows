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
        void CreateDirectory(string path);

        bool DirectoryExists(string path);

        IEnumerable<string> EnumerateFileSystemEntries(string path, string pattern, SearchOption options);

        IEnumerable<string> EnumerateFileSystemEntries(string path);

        void FileCopy(string sourcePath, string destinationPath, bool overwrite);

        void FileCopy(string sourcePath, string destinationPath);

        void FileDelete(string path);

        bool FileExists(string path);

        FileStream FileOpen(string path, FileMode mode, FileAccess access, FileShare share);

        byte[] FileReadAllBytes(string path);

        void FileWriteAllBytes(string path, byte[] data);

        string[] GetDriveRoots();

        string GetFileName(string path);

        string GetFullPath(string path);

        string GetParent(string path);
    }

    internal class FileSystem : BaseType, IFileSystem
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
