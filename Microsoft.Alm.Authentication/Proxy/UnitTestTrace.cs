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
using System.IO;
using System.Runtime.CompilerServices;
using static System.Globalization.CultureInfo;

namespace Microsoft.Alm.Authentication.Test
{
    public interface IUnitTestTrace
    {
        void WriteLine(string output);
    }

    internal class UnitTestTrace : IUnitTestTrace, Git.ITrace
    {
        public UnitTestTrace(Git.ITrace trace, IUnitTestTrace other)
        {
            if (trace is null)
                throw new ArgumentNullException(nameof(trace));
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            _other = other;
            _trace = trace;
        }

        private readonly IUnitTestTrace _other;
        private readonly Git.ITrace _trace;

        public Type ServiceType
            => typeof(Git.ITrace);

        public void WriteLine(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                return;

            var lines = output.Split('\r', '\n');

            for (int i = 0; i < lines.Length; i += 1)
            {
                var line = lines[i];

                if (string.IsNullOrEmpty(line))
                    continue;

                line = (i == 0)
                    ? string.Format(InvariantCulture, "{0:HH:mm:ss.ffffff} {1}", DateTime.Now, line)
                    : string.Format(InvariantCulture, "                {0}", line);

                _other.WriteLine(line);
            }
        }

        public void AddListener(TextWriter listener)
        {
            _trace.AddListener(listener);
        }

        public void Flush()
        {
            _trace.Flush();
        }

        public void WriteException(Exception exception, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            _trace.WriteException(exception, filePath, lineNumber, memberName);

            WriteLine(exception?.ToString());
        }

        public void WriteLine(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string memberName = "")
        {
            _trace.WriteLine(message, filePath, lineNumber, memberName);

            WriteLine(message);
        }
    }
}
