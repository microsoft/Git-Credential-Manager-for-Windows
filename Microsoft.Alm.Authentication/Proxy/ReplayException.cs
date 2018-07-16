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
using System.Runtime.Serialization;
using static System.FormattableString;

namespace Microsoft.Alm.Authentication.Test
{
    [Serializable]
    public class ReplayException : Exception
    {
        public ReplayException(string message)
            : base(message)
        { }

        public ReplayException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ReplayException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class ReplayDataException : ReplayException
    {
        public ReplayDataException(string message)
            : base(message)
        { }

        public ReplayDataException(FormattableString message)
            : this(Invariant(message))
        { }

        public ReplayDataException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ReplayDataException(FormattableString message, Exception innerException)
            : this(Invariant(message), innerException)
        { }

        public ReplayDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class ReplayInputException : Exception
    {
        public ReplayInputException(string message)
            : base(message)
        { }

        public ReplayInputException(FormattableString message)
            : this(Invariant(message))
        { }

        public ReplayInputException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ReplayInputException(FormattableString message, Exception innerException)
            : this(Invariant(message), innerException)
        { }

        public ReplayInputException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class ReplayInputTypeException : ReplayTypeException
    {
        public ReplayInputTypeException(Type expected, Type actual)
            : base(expected, actual)
        { }

        public ReplayInputTypeException(string message)
            : base(message)
        { }

        public ReplayInputTypeException(Type expected, Type actual, Exception innerException)
            : base(expected, actual, innerException)
        { }

        public ReplayInputTypeException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ReplayInputTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class ReplayNotFoundException : ReplayException
    {
        public ReplayNotFoundException(string message)
            : base(message)
        { }

        public ReplayNotFoundException(FormattableString message)
            : this(Invariant(message))
        { }

        public ReplayNotFoundException(string message, string path)
            : base(message)
        {
            _path = path;
        }

        public ReplayNotFoundException(FormattableString message, string path)
            : this(Invariant(message), path)
        { }

        public ReplayNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ReplayNotFoundException(FormattableString message, Exception innerException)
            : this(Invariant(message), innerException)
        { }

        public ReplayNotFoundException(string message, string path, Exception innerException)
            : base(message, innerException)
        {
            _path = path;
        }

        public ReplayNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _path = info.GetString(nameof(Path));
        }

        private readonly string _path;

        public string Path
        {
            get { return _path; }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Path), _path);

            base.GetObjectData(info, context);
        }
    }

    [Serializable]
    public class ReplayOutputTypeException : ReplayTypeException
    {
        public ReplayOutputTypeException(Type expected, Type actual)
            : base(expected, actual)
        { }

        public ReplayOutputTypeException(string message)
            : base(message)
        { }

        public ReplayOutputTypeException(FormattableString message)
            : this(Invariant(message))
        { }

        public ReplayOutputTypeException(Type expected, Type actual, Exception innerException)
            : base(expected, actual, innerException)
        { }

        public ReplayOutputTypeException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ReplayOutputTypeException(FormattableString message, Exception innerException)
            : this(Invariant(message), innerException)
        { }

        public ReplayOutputTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    [Serializable]
    public class ReplayTypeException : Exception
    {
        public ReplayTypeException(Type expected, Type actual)
            : this(CreateMessage(expected, actual))
        { }

        public ReplayTypeException(string message)
            : base(message)
        { }

        public ReplayTypeException(Type expected, Type actual, Exception innerException)
            : this(CreateMessage(expected, actual), innerException)
        { }

        public ReplayTypeException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public ReplayTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        internal static string CreateMessage(Type expected, Type actual)
        {
            string expectedName = (expected is null)
                ? "Unknown"
                : expected.GetType().FullName;
            string actualName = (actual is null)
                ? "Unknown"
                : actual.GetType().FullName;

            return $"Expected {expectedName}, found {actualName}.";
        }
    }
}
