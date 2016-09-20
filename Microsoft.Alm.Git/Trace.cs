using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Alm.Git
{
    internal interface ITrace
    {
        void AddListener(TextWriter listener);
        void Flush();
        void WriteLine(string message, string filePath, int lineNumber, string memberName);
    }

    public sealed class Trace : ITrace
    {
        public const string EnvironmentVariableKey = "GCM_TRACE";

        private Trace()
        {
            _writers = new List<TextWriter>();

            try
            {
                string traceValue = Environment.GetEnvironmentVariable(EnvironmentVariableKey);
                int val = 0;

                // if the value is true or a number greater than zero, then trace to standard error
                if (StringComparer.OrdinalIgnoreCase.Equals(traceValue, "true")
                    || (Int32.TryParse(traceValue, out val) && val > 0))
                {
                    _writers.Add(Console.Error);
                }
                // if the value is a rooted path, then trace to that file and not to the console
                else if (Path.IsPathRooted(traceValue))
                {
                    // open or create the log file
                    var stream = File.Open(traceValue, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

                    // seek to the end, no inserting at the front
                    stream.Seek(0, SeekOrigin.End);

                    // create the writer and add it to the list
                    var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true);
                    _writers.Add(writer);
                }
            }
            catch { }
        }

        ~Trace()
        {
            lock (_syncpoint)
            {
                foreach (var writer in _writers)
                {
                    try
                    {
                        writer?.Dispose();
                    }
                    catch { }
                }
            }
        }

        internal static ITrace Instance
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_instance == null)
                    {
                        _instance = new Trace();
                    }
                    return _instance;
                }
            }
            set { _instance = value; }
        }
        private static ITrace _instance;

        private static readonly object _syncpoint = new object();
        private readonly List<TextWriter> _writers;

        public static void AddListener(TextWriter listener)
            => Instance.AddListener(listener);

        public static void Flush()
            => Instance.Flush();

        public static void Write(string message,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
            => Instance.InternalWrite(message, filePath, lineNumber, memberName);

        public static void WriteLine(string message,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
            => Instance.WriteLine(message, filePath, lineNumber, memberName);

        private static string FormatText(string message, string filePath, int lineNumber, string memberName)
        {
            string source = Path.GetFileName(filePath);

            // the source:line column is 23 characters wide, we need to live within that limit
            string lnNumStr = Convert.ToString(lineNumber);
            int maxWidth = 23 - lnNumStr.Length - 1;

            if (source.Length > maxWidth)
            {
                source = source.Substring(0, maxWidth);
            }

            source = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", source, lineNumber);

            string details = String.Format(System.Globalization.CultureInfo.InvariantCulture, "[{0}] {1}", memberName, message);

            string text = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:hh:mm:ss.ffffff} {1,-23} {2}", DateTime.Now, source, details);

            return text;
        }

        void ITrace.AddListener(TextWriter listener)
        {
            lock (_syncpoint)
            {
                // try not to add the same listener more than once
                if (_writers.Contains(listener))
                    return;

                _writers.Add(listener);
            }
        }

        void ITrace.Flush()
        {
            lock (_syncpoint)
            {
                foreach (var writer in _writers)
                {
                    writer?.Flush();
                }
            }
        }

        void ITrace.WriteLine(string message, string filePath, int lineNumber, string memberName)
        {
            lock (_syncpoint)
            {
                if (_writers.Count == 0)
                    return;

                string text = FormatText(message, filePath, lineNumber, memberName);

                foreach (var writer in _writers)
                {
                    writer?.Write(text);
                    writer?.Write('\n');
                    writer?.Flush();
                }
            }
        }
    }
}
