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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    internal class CaptureStorage : ICaptureService<CapturedStorageData>, IStorage
    {
        internal static IReadOnlyList<Regex> AllowedCapturedPathValues
            = new List<Regex>
            {
                new Regex(@"^C:[\\/]Windows[\\/]?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex(@"^C:[\\/]Program Files[\\/]Git[\\/]?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex(@"^C:[\\/]Program Files[\\/]Git LFS[\\/]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex(@"^C:[\\/]Program Files \(x86\)[\\/]Git[\\/]?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex(@"^C:[\\/]Program Files \(x86\)[\\/]Git LFS[\\/]?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex(@"^C:[\\/]ProgramData[\\/]?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex(@"^C:[\\/]Users[\\/]Tester[\\/]?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex(@"^C:[\\/]Src[\\/]MS\.ALM\.GCM[\\/]?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase),
                new Regex(@"^reg:", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
            };
        internal static IReadOnlyList<(Regex Regex, string Replacement)> DataFilters
            = new List<(Regex, string)>
            {
                (new Regex(@"[/\\]$", RegexOptions.Compiled | RegexOptions.CultureInvariant), string.Empty),
                (new Regex(@"[/\\]+", RegexOptions.Compiled | RegexOptions.CultureInvariant), "\\"),
            };

        internal CaptureStorage(RuntimeContext context, Func<string, string> normalizePath)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (normalizePath is null)
                throw new ArgumentNullException(nameof(normalizePath));

            _captured = new Dictionary<string, Queue<CapturedStorageQuery>>(Ordinal);
            _context = context;
            _storage = context.Storage;
            _normalizePath = normalizePath;
            _queryOridinal = 0;
        }

        // Operations > Events > Queries
        private readonly Dictionary<string, Queue<CapturedStorageQuery>> _captured;
        private readonly RuntimeContext _context;
        private readonly Func<string, string> _normalizePath;
        private int _queryOridinal;
        private readonly IStorage _storage;

        public string ServiceName
            => "Storage";

        public Type ServiceType
            => typeof(IStorage);

        public void CreateDirectory(string path)
        {
            _storage.CreateDirectory(path);
            var input = _normalizePath(path);

            Capture(nameof(CreateDirectory), null, input);
        }

        public bool DirectoryExists(string path)
        {
            var output = _storage.DirectoryExists(path);
            var input = _normalizePath(path);

            Capture(nameof(DirectoryExists), output, input);

            return output;
        }

        public IEnumerable<string> EnumerateFileSystemEntries(string path, string pattern, SearchOption options)
        {
            var result = _storage.EnumerateFileSystemEntries(path, pattern, options);

            var input = new CapturedStorageQuery.EnumerateInput
            {
                Path = _normalizePath(path),
                Pattern = pattern,
                Options = (int)options,
            };
            var output = new List<string>(result);

            Capture(nameof(EnumerateFileSystemEntries), output, input);

            return output;
        }

        public IEnumerable<string> EnumerateFileSystemEntries(string path)
            => EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);

        public IEnumerable<SecureData> EnumerateSecureData(string prefix)
        {
            var result = _storage.EnumerateSecureData(prefix);
            var data = new List<SecureData>(result);
            var output = new List<CapturedStorageQuery.SecureDataOutput>(data.Count);
            var input = prefix;

            foreach (var item in data)
            {
                var capture = new CapturedStorageQuery.SecureDataOutput
                {
                    Data = item.Data,
                    Key = item.Key,
                    Name = item.Name,
                    Result = true,
                };

                output.Add(capture);
            }

            Capture(nameof(EnumerateSecureData), output, input);

            return data;
        }

        public void FileCopy(string sourcePath, string targetPath, bool overwrite)
        {
            _storage.FileCopy(sourcePath, targetPath, overwrite);

            var input = new CapturedStorageQuery.CopyInput
            {
                Overwrite = overwrite,
                SourcePath = _normalizePath(sourcePath),
                TargetPath = _normalizePath(targetPath),
            };

            Capture(nameof(DirectoryExists), null, input);
        }

        public void FileCopy(string sourcePath, string destinationPath)
            => FileCopy(sourcePath, destinationPath, false);

        public void FileDelete(string path)
        {
            _storage.FileDelete(path);

            var input = _normalizePath(path);

            Capture(nameof(FileDelete), null, input);
        }

        public bool FileExists(string path)
        {
            var output = _storage.FileExists(path);
            var input = _normalizePath(path);

            Capture(nameof(FileExists), output, input);

            return output;
        }

        public Stream FileOpen(string path, FileMode mode, FileAccess access, FileShare share)
        {
            var stream = _storage.FileOpen(path, mode, access, share);
            var output = new CaptureStream(access, stream);
            var input = new CapturedStorageQuery.OpenInput
            {
                Path = _normalizePath(path),
                Mode = (int)mode,
                Access = (int)access,
                Share = (int)share,
            };

            Capture(nameof(FileOpen), output, input);

            return output;
        }

        public byte[] FileReadAllBytes(string path)
        {
            var output = _storage.FileReadAllBytes(path);
            var input = _normalizePath(path);

            Capture(nameof(FileReadAllBytes), output, input);

            return output;
        }

        public string FileReadAllText(string path, Encoding encoding)
        {
            var output = _storage.FileReadAllText(path);
            var input = _normalizePath(path);

            Capture(nameof(FileReadAllText), output, input);

            return output;
        }

        public string FileReadAllText(string path)
            => FileReadAllText(path, Encoding.UTF8);

        public void FileWriteAllBytes(string path, byte[] data)
        {
            _storage.FileWriteAllBytes(path, data);

            var input = new CapturedStorageQuery.WriteAllBytesInput
            {
                Data = data,
                Path = _normalizePath(path),
            };

            Capture(nameof(FileWriteAllBytes), null, input);
        }

        public void FileWriteAllText(string path, string contents, Encoding encoding)
        {
            _storage.FileWriteAllText(path, contents, encoding);

            var input = new CapturedStorageQuery.WriteAllTextInput
            {
                Contents = contents,
                Path = _normalizePath(path),
            };

            Capture(nameof(FileWriteAllText), null, input);
        }

        public void FileWriteAllText(string path, string contents)
            => FileWriteAllText(path, contents, Encoding.UTF8);

        public string[] GetDriveRoots()
        {
            var output = _storage.GetDriveRoots();

            Capture(nameof(GetDriveRoots), output, null);

            return output;
        }

        public string GetFileName(string path)
        {
            var output = _storage.GetFileName(path);
            var input = _normalizePath(path);

            Capture(nameof(GetFileName), output, input);

            return output;
        }

        public string GetFullPath(string path)
        {
            var output = Path.GetFullPath(path);
            var input = _normalizePath(path);

            Capture(nameof(GetFullPath), output, input);

            return output;
        }

        public string GetParent(string path)
        {
            var output = _storage.GetParent(path);
            var input = _normalizePath(path);

            Capture(nameof(GetParent), output, input);

            return output;
        }

        public string RegistryReadString(RegistryHive registryHive, RegistryView registryView, string registryPath, string keyName)
        {
            var output = _storage.RegistryReadString(registryHive, registryView, registryPath, keyName);
            var input = new CapturedStorageQuery.RegistryReadInput
            {
                Hive = (int)registryHive,
                Name = keyName,
                Path = registryPath,
                View = (int)registryView,
            };

            Capture(nameof(RegistryReadString), output, input);

            return output;
        }

        public string RegistryReadString(RegistryHive registryHive, string registryPath, string keyName)
            => RegistryReadString(registryHive, RegistryView.Default, registryPath, keyName);

        public string RegistryReadString(string registryPath, string keyName)
            => RegistryReadString(RegistryHive.CurrentUser, RegistryView.Default, registryPath, keyName);

        public int TryPurgeSecureData(string prefix)
        {
            var output = _storage.TryPurgeSecureData(prefix);
            var input = prefix;

            Capture(nameof(TryPurgeSecureData), output, input);

            return output;
        }

        public bool TryReadSecureData(string key, out string name, out byte[] data)
        {
            var result = _storage.TryReadSecureData(key, out name, out data);
            var output = new CapturedStorageQuery.SecureDataOutput
            {
                Data = data,
                Name = name,
                Result = result,
            };
            var input = key;

            Capture(nameof(TryReadSecureData), output, input);

            return result;
        }

        public bool TryWriteSecureData(string key, string name, byte[] data)
        {
            var output = _storage.TryWriteSecureData(key, name, data);
            var input = new CapturedStorageQuery.SecureDataInput
            {
                Data = data,
                Key = key,
                Name = name,
            };

            Capture(nameof(TryWriteSecureData), output, input);

            return output;
        }

        internal bool GetCapturedData(ICapturedDataFilter filter, out CapturedStorageData capturedData)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            filter = new CapturedDataFilter(filter);

            foreach(var item in DataFilters)
            {
                filter.AddFilter(item.Regex, item.Replacement);
            }

            var paths = new Dictionary<string, Dictionary<string, List<CapturedStorageQuery>>>(OrdinalIgnoreCase);

            foreach (var capture in _captured)
            {
                var capturedMethod = capture.Key;
                var capturedQueries = capture.Value;

                foreach (var capturedQuery in capturedQueries)
                {
                    string path;
                    CapturedStorageQuery query = capturedQuery;

                    switch (capturedMethod)
                    {
                        case nameof(CreateDirectory):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (capturedQuery.Output != null)
                                throw new InvalidDataException();

                            input = filter.ApplyFilter(input);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = null,
                            };
                            path = input;
                        }
                        break;

                        case nameof(DirectoryExists):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is bool output))
                                throw new InvalidDataException();

                            input = filter.ApplyFilter(input);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input;
                        }
                        break;

                        case nameof(EnumerateFileSystemEntries):
                        {
                            if (!(capturedQuery.Input is CapturedStorageQuery.EnumerateInput input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is List<string> list))
                                throw new InvalidDataException();

                            input = new CapturedStorageQuery.EnumerateInput
                            {
                                Path = filter.ApplyFilter(input.Path),
                                Options = input.Options,
                                Pattern = input.Pattern,
                            };

                            var output = new List<string>(list.Count);
                            foreach (var entry in list)
                            {
                                var normalized = filter.ApplyFilter(entry);

                                bool isAllowed = false;

                                foreach (var allow in AllowedCapturedPathValues)
                                {
                                    if (allow.IsMatch(normalized))
                                    {
                                        isAllowed = true;
                                        break;
                                    }
                                }

                                if (!isAllowed)
                                    continue;

                                output.Add(normalized);
                            }

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input.Path;
                        }
                        break;

                        case nameof(EnumerateSecureData):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is List<SecureData> data))
                                throw new InvalidDataException();

                            var output = new List<CapturedStorageQuery.SecureDataOutput>(data.Count);

                            foreach (var datum in data)
                            {
                                output.Add(new CapturedStorageQuery.SecureDataOutput
                                {
                                    Data = datum.Data,
                                    Key = datum.Key,
                                    Name = datum.Name,
                                });
                            }

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = string.Empty;
                        }
                        break;

                        case nameof(FileCopy):
                        {
                            if (!(capturedQuery.Input is CapturedStorageQuery.CopyInput input))
                                throw new InvalidDataException();
                            if (capturedQuery.Output is null)
                                throw new InvalidDataException();

                            input = new CapturedStorageQuery.CopyInput
                            {
                                Overwrite = input.Overwrite,
                                SourcePath = filter.ApplyFilter(input.SourcePath),
                                TargetPath = filter.ApplyFilter(input.TargetPath),
                            };

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = null,
                            };
                            path = input.SourcePath;
                        }
                        break;

                        case nameof(FileDelete):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (capturedQuery.Output is null)
                                throw new InvalidDataException();

                            input = filter.ApplyFilter(input);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = null,
                            };
                            path = input;
                        }
                        break;

                        case nameof(FileExists):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is bool output))
                                throw new InvalidDataException();

                            input = filter.ApplyFilter(input);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input;
                        }
                        break;

                        case nameof(FileOpen):
                        {
                            if (!(capturedQuery.Input is CapturedStorageQuery.OpenInput input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is CaptureStream stream))
                                throw new InvalidDataException();

                            input.Path = filter.ApplyFilter(input.Path);

                            stream.GetCapturedData(filter, out var outputData);

                            var output = new CapturedStorageQuery.OpenOutput
                            {
                                Access = (int)stream.Access,
                                Data = outputData,
                            };

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input.Path;
                        }
                        break;

                        case nameof(FileReadAllBytes):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is byte[] output))
                                throw new InvalidDataException();

                            input = filter.ApplyFilter(input);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input;
                        }
                        break;

                        case nameof(FileWriteAllBytes):
                        {
                            if (!(capturedQuery.Input is CapturedStorageQuery.WriteAllBytesInput input))
                                throw new InvalidDataException();
                            if (capturedQuery.Output != null)
                                throw new InvalidDataException();

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = null,
                            };
                            path = input.Path;
                        }
                        break;

                        case nameof(GetDriveRoots):
                        {
                            if (capturedQuery.Input != null)
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is string output))
                                throw new InvalidDataException();

                            query = new CapturedStorageQuery
                            {
                                Input = null,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = string.Empty;
                        }
                        break;

                        case nameof(GetFileName):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is string output))
                                throw new InvalidDataException();

                            input = filter.ApplyFilter(input);
                            output = filter.ApplyFilter(output);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input;
                        }
                        break;

                        case nameof(GetFullPath):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is string output))
                                throw new InvalidDataException();

                            input = filter.ApplyFilter(input);
                            output = filter.ApplyFilter(output);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input;
                        }
                        break;

                        case nameof(GetParent):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();

                            var output = capturedQuery.Output as string;

                            input = filter.ApplyFilter(input);
                            output = filter.ApplyFilter(output);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input;
                        }
                        break;

                        case nameof(RegistryReadString):
                        {
                            if (!(capturedQuery.Input is CapturedStorageQuery.RegistryReadInput input))
                                throw new InvalidDataException();

                            string output = capturedQuery.Output as string;

                            output = filter.ApplyFilter(output);

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = input.ToString();
                        }
                        break;

                        case nameof(TryPurgeSecureData):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (capturedQuery.Output != null)
                                throw new InvalidDataException();

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = null,
                            };
                            path = string.Empty;
                        }
                        break;

                        case nameof(TryReadSecureData):
                        {
                            if (!(capturedQuery.Input is string input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is CapturedStorageQuery.SecureDataOutput output))
                                throw new InvalidDataException();

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = string.Empty;
                        }
                        break;

                        case nameof(TryWriteSecureData):
                        {
                            if (!(capturedQuery.Input is CapturedStorageQuery.SecureDataInput input))
                                throw new InvalidDataException();
                            if (!(capturedQuery.Output is bool output))
                                throw new InvalidDataException();

                            query = new CapturedStorageQuery
                            {
                                Input = input,
                                Oridinal = capturedQuery.Oridinal,
                                Output = output,
                            };
                            path = string.Empty;
                        }
                        break;

                        default:
                        throw new InvalidOperationException($"Unexpected method name: `{capturedMethod}`.");
                    }

                    path = filter.ApplyFilter(path);

                    if (path.Length > 0)
                    {
                        bool isAllowed = false;

                        foreach (var allowed in AllowedCapturedPathValues)
                        {
                            if (allowed.IsMatch(path))
                            {
                                isAllowed = true;
                                break;
                            }
                        }

                        if (!isAllowed)
                            continue;
                    }

                    if (!paths.TryGetValue(path, out var methodQueue))
                    {
                        methodQueue = new Dictionary<string, List<CapturedStorageQuery>>(Ordinal);

                        paths.Add(path, methodQueue);
                    }

                    if (!methodQueue.TryGetValue(capturedMethod, out var queries))
                    {
                        queries = new List<CapturedStorageQuery>();

                        methodQueue.Add(capturedMethod, queries);
                    }

                    queries.Add(query);
                }
            }

            var operations = new List<CapturedStorageOperation>(paths.Count);

            foreach (var item in paths)
            {
                var path = item.Key;
                var methodData = item.Value;

                var operation = new CapturedStorageOperation
                {
                    Path = path,
                    Methods = new List<CapturedStorageMethod>(methodData.Count),
                };

                foreach (var queries in methodData)
                {
                    var methodName = queries.Key;
                    var queryData = queries.Value;

                    var method = new CapturedStorageMethod
                    {
                        MethodName = methodName,
                        Queries = queryData,
                    };

                    operation.Methods.Add(method);
                }

                operations.Add(operation);
            }

            capturedData = new CapturedStorageData
            {
                Operations = operations,
            };
            return true;
        }

        private void Capture(string method, object output, object input = null)
        {
            lock (_captured)
            {
                if (!_captured.TryGetValue(method, out var queue))
                {
                    queue = new Queue<CapturedStorageQuery>();
                    _captured.Add(method, queue);
                }

                var query = new CapturedStorageQuery
                {
                    Input = input,
                    Oridinal = Interlocked.Increment(ref _queryOridinal),
                    Output = output
                };

                queue.Enqueue(query);
            }
        }

        string IProxyService.ServiceName
            => ServiceName;

        bool ICaptureService<CapturedStorageData>.GetCapturedData(ICapturedDataFilter filter, out CapturedStorageData capturedData)
            => GetCapturedData(filter, out capturedData);

        bool ICaptureService.GetCapturedData(ICapturedDataFilter filter, out object capturedData)
        {
            if (GetCapturedData(filter, out CapturedStorageData capturedStorageData))
            {
                capturedData = capturedStorageData;
                return true;
            }

            capturedData = null;
            return false;
        }
    }
}
