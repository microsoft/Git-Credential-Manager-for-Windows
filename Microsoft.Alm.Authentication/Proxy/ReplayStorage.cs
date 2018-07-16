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
using Microsoft.Win32;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    public class ReplayStorage : IStorage, IReplayService<CapturedStorageData>
    {
        internal ReplayStorage(RuntimeContext context, Func<string, string> normalizePath)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (normalizePath is null)
                throw new ArgumentNullException(nameof(normalizePath));

            _captures = new Dictionary<string, Dictionary<string, Queue<CapturedStorageQuery>>>(OrdinalIgnoreCase);
            _context = context;
            _normalizePath = normalizePath;
            _replayed = new Dictionary<string, Dictionary<string, List<CapturedStorageQuery>>>(OrdinalIgnoreCase);
            _syncpoint = new object();
        }

        private readonly Dictionary<string, Dictionary<string, Queue<CapturedStorageQuery>>> _captures;
        private readonly RuntimeContext _context;
        private readonly Dictionary<string, Dictionary<string, List<CapturedStorageQuery>>> _replayed;
        private readonly Func<string, string> _normalizePath;
        private readonly object _syncpoint;

        public string ServiceName
            => "Storage";

        public Type ServiceType
            => typeof(IStorage);

        public void CreateDirectory(string path)
        {
            if (!TryReadNext(nameof(CreateDirectory), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
        }

        public bool DirectoryExists(string path)
        {
            if (!TryReadNext(nameof(DirectoryExists), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is bool output))
                throw new ReplayOutputTypeException(typeof(string), query.Output?.GetType());

            return output;
        }

        public IEnumerable<string> EnumerateFileSystemEntries(string path, string pattern, SearchOption options)
        {
            if (!TryReadNext(nameof(EnumerateFileSystemEntries), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is CapturedStorageQuery.EnumerateInput input))
                throw new ReplayInputTypeException(typeof(CapturedStorageQuery.EnumerateInput), query.Input?.GetType());
            if (!(query.Output is IEnumerable<string> output))
                throw new ReplayOutputTypeException(typeof(string[ ]), query.Output?.GetType());

            // Validate inputs are identical
            if (input.Options != (int)options)
                throw new ReplayInputException($"Unexpected `{nameof(options)}`: expected \"{input.Options}\" vs actual \"{(int)options}\".");
            if (!Ordinal.Equals(input.Pattern, pattern))
                throw new ReplayInputException($"Unexpected `{nameof(pattern)}`: expected \"{input.Pattern}\" vs actual \"{pattern}\".");

            return output;
        }

        public IEnumerable<string> EnumerateFileSystemEntries(string path)
            => EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);

        public IEnumerable<SecureData> EnumerateSecureData(string prefix)
        {
            string path = string.Empty;

            if (!TryReadNext(nameof(EnumerateSecureData), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException("Unable to dequeue next operation", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is CapturedStorageQuery.SecureDataOutput[ ] output))
                throw new ReplayOutputTypeException(typeof(CapturedStorageQuery.SecureDataOutput[ ]), query.Output?.GetType());

            // Validate inputs are identical
            if (!OrdinalIgnoreCase.Equals(input, prefix))
                throw new ReplayInputException($"Unexpected `{nameof(prefix)}`: expected \"{input}\" vs actual \"{prefix}\".");

            var result = new List<SecureData>(output.Length);

            foreach (var item in output)
            {
                result.Add(new SecureData(item.Key, item.Name, item.Data));
            }

            return result;
        }

        public void FileCopy(string sourcePath, string targetPath, bool overwrite)
        {
            if (!TryReadNext(nameof(FileCopy), ref sourcePath, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{sourcePath}\"", sourcePath);
            if (!(query.Input is CapturedStorageQuery.CopyInput input))
                throw new ReplayInputTypeException(typeof(CapturedStorageQuery.WriteAllBytesInput), query.Input?.GetType());

            // Validate that inputs are identical
            if (!OrdinalIgnoreCase.Equals(sourcePath, input.SourcePath))
                throw new ReplayInputException($"Unexpected `{nameof(sourcePath)}`: expected \"{input.SourcePath}\" vs actual \"{sourcePath}\".");
            if (!OrdinalIgnoreCase.Equals(targetPath, input.TargetPath))
                throw new ReplayInputException($"Unexpected `{nameof(targetPath)}`: expected \"{input.TargetPath}\" vs actual \"{targetPath}\".");
            if (overwrite != input.Overwrite)
                throw new ReplayInputException($"Unexpected `{nameof(overwrite)}`: expected \"{input.Overwrite}\" vs actual \"{overwrite}\".");
        }

        public void FileCopy(string sourcePath, string targetPath)
            => FileCopy(sourcePath, targetPath, false);

        public void FileDelete(string path)
        {
            if (!TryReadNext(nameof(FileDelete), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException("Unable to dequeue next operation", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
        }

        public bool FileExists(string path)
        {
            if (!TryReadNext(nameof(FileExists), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is bool output))
                throw new ReplayOutputTypeException(typeof(bool), query.Output?.GetType());

            return output;
        }

        public Stream FileOpen(string path, FileMode mode, FileAccess access, FileShare share)
        {
            if (!TryReadNext(nameof(FileOpen), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is CapturedStorageQuery.OpenInput input))
                throw new ReplayInputTypeException(typeof(CapturedStorageQuery.OpenInput), query.Input?.GetType());
            if (!(query.Output is CapturedStorageQuery.OpenOutput output))
                throw new ReplayOutputTypeException(typeof(byte[ ]), query.Output?.GetType());

            // Validate inputs are as expected
            if (!OrdinalIgnoreCase.Equals(input.Path, path))
                throw new ReplayInputException($"Unexpected `{nameof(path)}` value. Expected \"{input.Path}\" vs actual \"{path}\".");
            if (input.Mode != (int)mode)
                throw new ReplayInputException($"Unexpected `{nameof(mode)}` value. Expected \"{input.Mode}\" vs actual \"{mode}\".");
            if (input.Access != (int)access)
                throw new ReplayInputException($"Unexpected `{nameof(access)}` value. Expected \"{input.Access}\" vs actual \"{access}\".");
            if (input.Share != (int)share)
                throw new ReplayInputException($"Unexpected `{nameof(share)}` value. Expected \"{input.Share}\" vs actual \"{share}\".");

            return new ReplayStream((FileAccess)output.Access, output.Data); ;
        }

        public byte[ ] FileReadAllBytes(string path)
        {
            if (!TryReadNext(nameof(FileReadAllBytes), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is byte[ ] output))
                throw new ReplayOutputTypeException(typeof(byte[ ]), query.Output?.GetType());

            return output;
        }

        public string FileReadAllText(string path, Encoding encoding)
        {
            if (!TryReadNext(nameof(FileReadAllText), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is string output))
                throw new ReplayOutputTypeException(typeof(string), query.Output?.GetType());

            return output;
        }

        public string FileReadAllText(string path)
            => FileReadAllText(path, Encoding.UTF8);

        public void FileWriteAllBytes(string path, byte[ ] data)
        {
            if (!TryReadNext(nameof(FileWriteAllBytes), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is CapturedStorageQuery.WriteAllBytesInput input))
                throw new ReplayInputTypeException(typeof(CapturedStorageQuery.WriteAllBytesInput), query.Input?.GetType());

            // Validate that inputs are identical
            if (!OrdinalIgnoreCase.Equals(path, input.Path))
                throw new ReplayInputException($"Unexpected `{nameof(path)}` value. Expected \"{input.Path}\" vs actual \"{path}\".");

            if ((data == null && input.Data != null)
                || (data != null && input.Data == null)
                || data.Length != input.Data.Length)
                throw new ReplayInputException($"Contents of `{nameof(data)}` do not match the expected value.");
        }

        public void FileWriteAllText(string path, string contents, Encoding encoding)
        {
            if (!TryReadNext(nameof(FileWriteAllBytes), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is CapturedStorageQuery.WriteAllTextInput input))
                throw new ReplayInputTypeException(typeof(CapturedStorageQuery.WriteAllBytesInput), query.Input?.GetType());

            // Validate that inputs are identical
            if (!OrdinalIgnoreCase.Equals(path, input.Path))
                throw new ReplayInputException($"Unexpected `{nameof(path)}` value. Expected \"{input.Path}\" vs actual \"{path}\".");
            if (!Ordinal.Equals(contents, input.Contents))
                throw new ReplayInputException($"Unexpected `{nameof(contents)}` value. Expected \"{input.Contents}\" vs actual \"{contents}\".");
        }

        public void FileWriteAllText(string path, string contents)
            => FileWriteAllText(path, contents, Encoding.UTF8);

        public string[ ] GetDriveRoots()
        {
            string path = string.Empty;

            if (!TryReadNext(nameof(GetDriveRoots), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException("Unable to dequeue next operation", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string[ ]), query.Input?.GetType());
            if (!(query.Output is string[ ] output))
                throw new ReplayOutputTypeException(typeof(string), query.Output?.GetType());

            return output;
        }

        public string GetFileName(string path)
        {
            if (!TryReadNext(nameof(GetFileName), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is string output))
                throw new ReplayOutputTypeException(typeof(string), query.Output?.GetType());

            return output;
        }

        public string GetFullPath(string path)
        {
            if (!TryReadNext(nameof(GetFullPath), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is string output))
                throw new ReplayOutputTypeException(typeof(string), query.Output?.GetType());

            return output;
        }

        public string GetParent(string path)
        {
            if (!TryReadNext(nameof(GetParent), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is string output))
                throw new ReplayOutputTypeException(typeof(string), query.Output?.GetType());

            return output;
        }

        public string RegistryReadString(RegistryHive registryHive, RegistryView registryView, string registryPath, string keyName)
        {
            string path = CapturedStorageQuery.RegistryReadInput.CreateStoragePath(registryHive,
                                                                                   registryView,
                                                                                   registryPath,
                                                                                   keyName);

            if (!TryReadNext(nameof(RegistryReadString), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException($"Unable to dequeue next operation for \"{path}\"", path);
            if (!(query.Input is CapturedStorageQuery.RegistryReadInput input))
                throw new ReplayInputTypeException(typeof(CapturedStorageQuery.RegistryReadInput), query.Input?.GetType());

            string output = query.Output as string;

            // Validated inputs are as expected
            if (input.Hive != (int)registryHive)
                throw new ReplayInputException($"Unexpected `{nameof(registryHive)}` value. Expected \"{(RegistryHive)input.Hive}\" vs actual \"{registryHive}\".");
            if (input.View != (int)registryView)
                throw new ReplayInputException($"Unexpected `{nameof(registryView)}` value. Expected \"{(RegistryView)input.View}\" vs actual \"{registryView}\".");
            if (!OrdinalIgnoreCase.Equals(input.Path, registryPath))
                throw new ReplayInputException($"Unexpected `{nameof(registryPath)}` value. Expected \"{input.Path}\" vs actual \"{registryPath}\".");
            if (!OrdinalIgnoreCase.Equals(input.Name, keyName))
                throw new ReplayInputException($"Unexpected `{nameof(keyName)}` value. Expected \"{input.Name}\" vs actual \"{keyName}\".");

            return output;
        }

        public string RegistryReadString(RegistryHive registryHive, string registryPath, string keyName)
            => RegistryReadString(registryHive, RegistryView.Default, registryPath, keyName);

        public string RegistryReadString(string registryPath, string keyName)
            => RegistryReadString(RegistryHive.CurrentUser, RegistryView.Default, registryPath, keyName);

        public int TryPurgeSecureData(string prefix)
        {
            string path = string.Empty;

            if (!TryReadNext(nameof(TryPurgeSecureData), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException("Unable to dequeue next operation", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is int output))
                throw new ReplayOutputTypeException(typeof(int), query.Output?.GetType());

            return output;
        }

        public bool TryReadSecureData(string key, out string name, out byte[ ] data)
        {
            string path = string.Empty;

            if (!TryReadNext(nameof(TryReadSecureData), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException("Unable to dequeue next operation", path);
            if (!(query.Input is string input))
                throw new ReplayInputTypeException(typeof(string), query.Input?.GetType());
            if (!(query.Output is CapturedStorageQuery.SecureDataOutput output))
                throw new ReplayOutputTypeException(typeof(CapturedStorageQuery.SecureDataOutput), query.Output?.GetType());

            // Validate inputs are as expected
            if (!OrdinalIgnoreCase.Equals(key, input))
                throw new ReplayInputException($"Unexpected `{nameof(key)}` value. Expected \"{input}\" vs actual \"{key}\".");

            data = output.Data;
            name = output.Name;

            return output.Result;
        }

        public bool TryWriteSecureData(string key, string name, byte[ ] data)
        {
            string path = string.Empty;

            if (!TryReadNext(nameof(TryWriteSecureData), ref path, out CapturedStorageQuery query))
                throw new ReplayNotFoundException("Unable to dequeue next operation", path);
            if (!(query.Input is CapturedStorageQuery.SecureDataInput input))
                throw new ReplayInputTypeException(typeof(CapturedStorageQuery.SecureDataInput), query.Input?.GetType());
            if (!(query.Output is bool output))
                throw new ReplayOutputTypeException(typeof(bool), query.Output?.GetType());

            // Validate inputs are as expected
            if (!OrdinalIgnoreCase.Equals(key, input.Key))
                throw new ReplayInputException($"Unexpected `{nameof(key)}` value. Expected \"{input.Key}\" vs actual \"{key}\".");
            if (!OrdinalIgnoreCase.Equals(name, input.Name))
                throw new ReplayInputException($"Unexpected `{nameof(name)}` value. Expected \"{input.Name}\" vs actual \"{name}\".");
            if ((data == null && input.Data != null)
                || (data != null && input.Data == null)
                || data.Length != input.Data.Length)
                throw new ReplayInputException($"Unexpected `{nameof(data)}` value. Expected \"{input.Data?.Length}\" vs actual \"{data?.Length}\".");

            return output;
        }

        internal void SetReplayData(CapturedStorageData data)
        {
            if (data.Operations is null)
                return;

            foreach (var operation in data.Operations)
            {
                if (operation.Path is null)
                    continue;

                if (!_captures.TryGetValue(operation.Path, out Dictionary<string, Queue<CapturedStorageQuery>> methods))
                {
                    methods = new Dictionary<string, Queue<CapturedStorageQuery>>(Ordinal);

                    _captures.Add(operation.Path, methods);
                }

                foreach (var method in operation.Methods)
                {
                    if (method.MethodName is null)
                        continue;

                    foreach (var query in method.Queries)
                    {
                        var item = query;

                        switch (method.MethodName)
                        {
                            case nameof(CreateDirectory): break;

                            case nameof(DirectoryExists):
                            {
                                if (query.Output is string value && bool.TryParse(value, out bool output))
                                {
                                    item.Output = output;
                                }
                            }
                            break;

                            case nameof(EnumerateFileSystemEntries):
                            {
                                if (query.Input is Newtonsoft.Json.Linq.JObject jobject)
                                {
                                    item.Input = jobject.ToObject<CapturedStorageQuery.EnumerateInput>();
                                }

                                if (query.Output is Newtonsoft.Json.Linq.JArray jarray)
                                {
                                    item.Output = jarray.ToObject<List<string>>();
                                }
                            }
                            break;

                            case nameof(EnumerateSecureData):
                            {
                                if (query.Output is Newtonsoft.Json.Linq.JArray jarray)
                                {
                                    item.Output = jarray.ToObject<List<CapturedStorageQuery.SecureDataOutput>>();
                                }
                            }
                            break;

                            case nameof(FileCopy):
                            {
                                if (query.Input is Newtonsoft.Json.Linq.JObject jobject)
                                {
                                    item.Input = jobject.ToObject<CapturedStorageQuery.CopyInput>();
                                }

                                if (item.Output is string value && bool.TryParse(value, out bool output))
                                {
                                    item.Output = output;
                                }
                            }
                            break;

                            case nameof(FileDelete): break;

                            case nameof(FileExists):
                            {
                                if (query.Output is string value && bool.TryParse(value, out bool output))
                                {
                                    item.Output = output;
                                }
                            }
                            break;

                            case nameof(FileOpen):
                            {
                                if (query.Input is Newtonsoft.Json.Linq.JObject jobject)
                                {
                                    item.Input = jobject.ToObject<CapturedStorageQuery.OpenInput>();
                                }

                                if (query.Output is Newtonsoft.Json.Linq.JArray jarray)
                                {
                                    ;
                                }

                                if (query.Output is Newtonsoft.Json.Linq.JObject joutput)
                                {
                                    item.Output = joutput.ToObject<CapturedStorageQuery.OpenOutput>();
                                }
                            }
                            break;

                            case nameof(FileReadAllBytes):
                            {
                                if (query.Output is string base64)
                                {
                                    item.Output = Convert.FromBase64String(base64);
                                }
                            }
                            break;

                            case nameof(FileReadAllText): break;

                            case nameof(FileWriteAllBytes):
                            {
                                if (query.Input is string base64)
                                {
                                    item.Input = Convert.FromBase64String(base64);
                                }
                            }
                            break;

                            case nameof(FileWriteAllText): break;

                            case nameof(GetDriveRoots):
                            {
                                if (query.Output is Newtonsoft.Json.Linq.JArray jarray)
                                {
                                    item.Output = jarray.ToObject<string[ ]>();
                                }
                            }
                            break;

                            case nameof(GetFileName): break;
                            case nameof(GetFullPath): break;
                            case nameof(GetParent): break;

                            case nameof(RegistryReadString):
                            {
                                if (query.Input is Newtonsoft.Json.Linq.JObject jobject)
                                {
                                    item.Input = jobject.ToObject<CapturedStorageQuery.RegistryReadInput>();
                                }
                            }
                            break;

                            case nameof(TryPurgeSecureData): break;

                            case nameof(TryReadSecureData):
                            {
                                if (query.Output is Newtonsoft.Json.Linq.JObject jobject)
                                {
                                    item.Output = jobject.ToObject<CapturedStorageQuery.SecureDataOutput>();
                                }
                            }
                            break;

                            case nameof(TryWriteSecureData):
                            {
                                if (query.Input is Newtonsoft.Json.Linq.JObject jobject)
                                {
                                    item.Input = jobject.ToObject<CapturedStorageQuery.SecureDataInput>();
                                }

                                if (item.Output is string value && bool.TryParse(value, out bool output))
                                {
                                    item.Output = output;
                                }
                            }
                            break;

                            default:
                                throw new InvalidDataException($"Unknown method `{method.MethodName}`.");
                        }

                        if (!methods.TryGetValue(method.MethodName, out Queue<CapturedStorageQuery> queries))
                        {
                            queries = new Queue<CapturedStorageQuery>();

                            methods.Add(method.MethodName, queries);
                        }

                        queries.Enqueue(item);
                    }
                }
            }
        }

        private bool TryReadNext(string method, ref string path, out CapturedStorageQuery query)
        {
            lock (_syncpoint)
            {
                if (_normalizePath != null)
                {
                    path = _normalizePath(path);
                }

                if (_captures.TryGetValue(path, out var capturedMethods))
                {
                    if (!_replayed.TryGetValue(path, out var replayedMethods))
                    {
                        replayedMethods = new Dictionary<string, List<CapturedStorageQuery>>(Ordinal);

                        _replayed.Add(path, replayedMethods);
                    }

                    if (capturedMethods.TryGetValue(method, out var capturedQueries))
                    {
                        if (!replayedMethods.TryGetValue(method, out var replayedQueries))
                        {
                            replayedQueries = new List<CapturedStorageQuery>();
                            replayedMethods.Add(method, replayedQueries);
                        }

                        if (capturedQueries.Count > 0)
                        {
                            query = capturedQueries.Dequeue();

                            replayedQueries.Add(query);

                            return true;
                        }
                    }
                }

                query = default(CapturedStorageQuery);
                return false;
            }
        }

        void IReplayService<CapturedStorageData>.SetReplayData(CapturedStorageData replayData)
            => SetReplayData(replayData);

        void IReplayService.SetReplayData(object replayData)
        {
            if (!(replayData is CapturedStorageData storageData)
                && !CapturedStorageData.TryDeserialize(replayData, out storageData))
            {
                var inner = new System.IO.InvalidDataException($"Failed to deserialize data into `{nameof(CapturedSettingsData)}`.");
                throw new ArgumentException(inner.Message, nameof(replayData), inner);
            }

            SetReplayData(storageData);
        }
    }
}
