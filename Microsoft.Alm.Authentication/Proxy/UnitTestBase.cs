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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using static System.StringComparer;
using static System.FormattableString;

namespace Microsoft.Alm.Authentication.Test
{
    public enum UnitTestMode
    {
        Capture,
        NoProxy,
        Replay,
    }

    public abstract class UnitTestBase : Base, IDisposable
    {
        /// <summary>
        /// The name of the environment variable used to override the path used to place test results.
        /// </summary>
        public const string ResultPathEnvironmentVariableName = "Gcm_TestPath";

        public const string FauxDataGlobalGitConfig = FauxDataHomePath + @"\.gitconfig";
        public const string FauxDataHomePath = @"C:\Users\Tester";
        public const string FauxDataPortableGitConfig = FauxDataProgramData + @"\Git\config";
        public const string FauxDataProgramData = @"C:\ProgramData";
        public const string FauxDataProgramFiles = @"C:\Program Files";
        public const string FauxDataResultPath = FauxDataSolutionPath + @"\Result";
        public const string FauxDataSolutionPath = @"C:\Src\MS.ALM.GCM";
        public const string FauxDataSystemGitConfig = FauxDataProgramFiles + @"\Git\mingw64\etc\gitconfig";
        public const string FauxDataXdgGitConfig = FauxDataXdgPath + @"\Git\config";
        public const string FauxDataXdgPath = @"C:\Xdg";
        public const string SolutionFileName = "GitCredentialManager.sln";
        public const string TestBaseDirectoryName = "Test";
        public const string TestDataDirectoryName = "Data";
        public const string TestDataFileExtension = ".json";
        public const string TestResultDirectoryName = "Results";

        /// <summary>
        /// The mode (no-proxy, capture, or replay) that the tests are being executed in.
        /// </summary>
        public static readonly UnitTestMode ProjectTestMode;

        protected UnitTestBase(IUnitTestTrace output, string projectDirectory, [CallerFilePath] string filePath = "")
            : base(RuntimeContext.Create())
        {
            if (output is null)
                throw new ArgumentNullException(nameof(output));

            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = Invariant($"{nameof(UnitTestBase)}.cs");
            }

            TestMode = ProjectTestMode;

            _filePath = filePath;
            _iteration = -1;
            _projectDirectory = projectDirectory ?? Directory.GetParent(filePath).FullName;
            _tempDirectory = Path.GetTempPath();
            _testInstanceName = Guid.NewGuid().ToString("N");

            while (_projectDirectory != null
                && !Directory.EnumerateFiles(_projectDirectory)
                             .Any(x => OrdinalIgnoreCase.Equals(Path.GetExtension(x), ".csproj")))
            {
                _projectDirectory = Path.GetDirectoryName(_projectDirectory);
            }

            _solutionDirectory = FindSolutionDirectory(_projectDirectory);

            Context.Trace = new UnitTestTrace(Context.Trace, output);

            _output = output;

            _output.WriteLine($"Starting {GetType().FullName}.");
        }

        protected UnitTestBase(IUnitTestTrace output, [CallerFilePath] string filePath = "")
            : this(output, null, filePath)
        { }

        static UnitTestBase()
        {
            // Enable TLS1.2 for all tests.
            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol
                                                            | System.Net.SecurityProtocolType.Tls12;

            ProjectTestMode = UnitTestMode.Replay;
        }

        private readonly string _filePath;
        private bool _initialized;
        private int _iteration;
        private readonly IUnitTestTrace _output;
        private readonly string _projectDirectory;
        private IProxy _proxy;
        private readonly string _solutionDirectory;
        private readonly object _syncpoint = new object();
        private string _tempDirectory;
        private string _testDataFile;
        private string _testDataPath;
        private readonly string _testInstanceName;
        private string _testName;
        private UnitTestMode _testMode;
        private string _testResultsPath;

        protected bool AllowDirectoryCreation
        {
            get { return TestMode != UnitTestMode.Replay; }
        }

        protected bool IsParameterized
        {
            get
            {
                lock (_syncpoint)
                {
                    if (!_initialized)
                        throw new InvalidOperationException($"`{nameof(InitializeTest)}` must be called before `{nameof(IsParameterized)}' can be accessed.");

                    return _iteration > 0; ;
                }
            }
        }

        protected string ProjectDirectory
        {
            get { return _projectDirectory; }
        }

        protected IProxy Proxy
        {
            get
            {
                lock (_syncpoint)
                {
                    if (!_initialized)
                        throw new InvalidOperationException($"`{nameof(InitializeTest)}` must be called before the `{nameof(Proxy)}` property can be accessed.");

                    return _proxy;
                }
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(Proxy));

                lock (_syncpoint)
                {
                    _proxy = value;
                }
            }
        }

        protected string SolutionDirectory
        {
            get
            {
                return TestMode == UnitTestMode.Replay
                    ? FauxDataSolutionPath
                    : _solutionDirectory;
            }
        }

        protected string TempDirectory
        {
            get { lock (_syncpoint) return _tempDirectory; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(TempDirectory));

                lock (_syncpoint)
                {
                    _tempDirectory = value;

                    Trace.WriteLine($"{nameof(TempDirectory)} = '{_tempDirectory}'.");
                }
            }
        }

        /// <summary>
        /// Gets the persistent file location of where captured data gets written to or read from.
        /// </summary>
        protected string TestDataFile
        {
            get
            {
                lock (_syncpoint)
                {
                    if (!_initialized)
                        throw new InvalidOperationException($"`{nameof(InitializeTest)}` must be called before `{nameof(TestDataFile)}` property can be accessed.");

                    return _testDataFile;
                }
            }
        }

        /// <summary>
        /// Gets the persistent directory location of where captured data gets written to or read from.
        /// </summary>
        protected string TestDataPath
        {
            get
            {
                lock (_syncpoint)
                {
                    if (!_initialized)
                        throw new InvalidOperationException($"`{nameof(InitializeTest)}` must be called before `{nameof(TestDataPath)}` property can be accessed.");

                    return _testDataPath;
                }
            }
        }

        protected string TestName
        {
            get
            {
                lock (_syncpoint)
                {
                    if (!_initialized)
                        throw new InvalidOperationException($"`{nameof(InitializeTest)}` must be called before `{nameof(TestName)}' can be accessed.");

                    return _testName;
                }
            }
        }

        protected UnitTestMode TestMode
        {
            get { lock (_syncpoint) return _testMode; }
            set { lock (_syncpoint) _testMode = value; }
        }

        protected string TestResultsPath
        {
            get
            {
                lock (_syncpoint)
                {
                    if (!_initialized)
                        throw new InvalidOperationException($"`{nameof(InitializeTest)}` must be called before `{nameof(TestResultsPath)}' can be accessed.");

                    return _testResultsPath;
                }
            }
        }

        public void Dispose()
        {
            lock (_syncpoint)
            {
                Context.Trace.Flush();

                // If capturing mock data, serialize the test's meta data
                if (_initialized)
                {
                    if (TestMode == UnitTestMode.Capture)
                    {
                        var resultDirectory = Path.GetDirectoryName(_testDataFile);

                        if (!Directory.Exists(resultDirectory))
                        {
                            Directory.CreateDirectory(resultDirectory);
                        }

                        Trace.WriteLine($"writing data \"{_testDataFile}\".");

                        using (var writableStream = File.Open(_testDataFile, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            Proxy.WriteTestData(writableStream);
                        }
                    }

                    if (AllowDirectoryCreation)
                    {
                        if (Directory.Exists(TestResultsPath))
                        {
                            for (int i = 1; i <= 5; i += 1)
                            {
                                try
                                {
                                    Directory.Delete(TestResultsPath, true);
                                    break;
                                }
                                catch
                                {
                                    System.Threading.Thread.Sleep(100 * i);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void InitializeTest(int iteration = -1, [CallerMemberName] string testName = "")
        {
            if (string.IsNullOrWhiteSpace(testName))
                throw new ArgumentNullException(nameof(testName));

            lock (_syncpoint)
            {
                if (_initialized)
                    throw new InvalidOperationException("Test already initialized.");

                _iteration = iteration;

                InitializeTestPaths(testName);

                var projectDirectory = Path.Combine(_solutionDirectory, _projectDirectory);
                var options = new ProxyOptions(Translate(TestMode), _solutionDirectory, projectDirectory)
                {
                    FauxPrefixPath = FauxDataSolutionPath,
                    FauxResultPath = FauxDataResultPath,
                    FauxHomePath = FauxDataHomePath,
                };

                if (_proxy is null)
                {
                    _proxy = Test.Proxy.Create(Context, options);
                }

                switch (TestMode)
                {
                    case UnitTestMode.Capture:
                    {
                        _proxy.Data.ResultPath = Path.Combine(projectDirectory, TestResultDirectoryName);
                        _proxy.Data.DisplayName = _testName;
                    }
                    break;

                    case UnitTestMode.Replay:
                    {
                        using (var readableStream = File.Open(_testDataFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            _proxy.ReadTestData(readableStream);
                        }

                        _proxy.Data.ResultPath = FauxDataResultPath;
                    }
                    break;

                    case UnitTestMode.NoProxy:
                        break;

                    default:
                        throw new InvalidOperationException($"`{TestMode}` is an undefined value for `{typeof(UnitTestMode).FullName}`.");
                }

                _initialized = true;
            }
        }

        protected static ProxyMode Translate(UnitTestMode mode)
        {
            switch (mode)
            {
                case UnitTestMode.Capture:
                    return ProxyMode.DataCapture;

                case UnitTestMode.NoProxy:
                    return ProxyMode.DataPassthrough;

                case UnitTestMode.Replay:
                    return ProxyMode.DataReplay;
            }

            throw new InvalidOperationException($"'{mode}' is an undefined value of `{nameof(UnitTestMode)}`");
        }

        protected static UnitTestMode Translate(ProxyMode mode)
        {
            switch (mode)
            {
                case ProxyMode.DataCapture:
                    return UnitTestMode.Capture;

                case ProxyMode.DataReplay:
                    return UnitTestMode.Replay;

                case ProxyMode.DataPassthrough:
                    return UnitTestMode.NoProxy;
            }

            throw new InvalidOperationException($"'{mode}' is an undefined value of` {nameof(ProxyMode)}`");
        }

        private static string FindSolutionDirectory(string startingDirectory)
        {
            var directoryInfo = new DirectoryInfo(startingDirectory);

            while (directoryInfo.Parent != null)
            {
                if (directoryInfo.EnumerateFiles().Any(f => f.Name.Equals(SolutionFileName, StringComparison.OrdinalIgnoreCase)))
                    return directoryInfo.FullName;

                directoryInfo = directoryInfo.Parent;
            }

            return null;
        }

        private void InitializeTestPaths(string testName)
        {
            if (testName is null)
                throw new ArgumentNullException(nameof(testName));

            _testName = Invariant($"{GetType().Name}_{testName}");

            Trace.WriteLine($"{nameof(TestName)} = '{_testName}'.");

            string directory = null;
            string envpath = Environment.GetEnvironmentVariable(ResultPathEnvironmentVariableName);

            if (!string.IsNullOrWhiteSpace(envpath))
            {
                try
                {
                    // Expand any environment variables in the path.
                    directory = Environment.ExpandEnvironmentVariables(envpath);
                }
                catch
                { /* squelch */ }
            }

            if (directory is null)
            {
                directory = Path.Combine(_solutionDirectory, _projectDirectory);
            }

            directory = Path.GetFullPath(directory);

            if (AllowDirectoryCreation && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string testDataPath = Path.Combine(directory, TestDataDirectoryName);
            testDataPath = Path.GetFullPath(testDataPath);

            if (AllowDirectoryCreation && !Directory.Exists(testDataPath))
            {
                Directory.CreateDirectory(testDataPath);
            }

            _testDataPath = testDataPath;

            Trace.WriteLine($"{nameof(TestDataPath)} = '{_testDataPath}'.");

            string dataFilePath = _testName.Replace("::", "-");

            dataFilePath = Path.Combine(_testDataPath, dataFilePath);

            if (_iteration >= 0)
            {
                dataFilePath = Invariant($"{dataFilePath}-{_iteration:00}");
            }

            _testDataFile = Path.ChangeExtension(dataFilePath, TestDataFileExtension);

            Trace.WriteLine($"{nameof(TestDataFile)} = \"{_testDataFile}\".");

            if (TestMode == UnitTestMode.Replay)
            {
                _testResultsPath = Path.Combine(FauxDataResultPath, testName);
            }
            else
            {
                string testResultsPath = Path.Combine(directory, TestResultDirectoryName, testName, _testInstanceName);
                testResultsPath = Path.GetFullPath(testResultsPath);

                if (AllowDirectoryCreation && !Directory.Exists(testResultsPath))
                {
                    Directory.CreateDirectory(testResultsPath);
                }

                _testResultsPath = testResultsPath;
            }

            Trace.WriteLine($"{nameof(TestResultsPath)} = '{_testResultsPath}'.");
        }
    }
}
