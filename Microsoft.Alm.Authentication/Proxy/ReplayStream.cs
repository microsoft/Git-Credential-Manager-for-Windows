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
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Alm.Authentication.Test
{
    internal class ReplayStream : Stream
    {
        public ReplayStream(FileAccess access, IEnumerable<string> data)
        {
            _access = access;
            _buffers = new ConcurrentQueue<byte[]>();

            foreach (var item in data)
            {
                var buffer = Convert.FromBase64String(item);
                _buffers.Enqueue(buffer);
            }
        }

        private readonly FileAccess _access;
        private readonly ConcurrentQueue<byte[]> _buffers;

        public override bool CanRead
        {
            get { return (_access & FileAccess.Read) != 0; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return (_access & FileAccess.Write) != 0; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_buffers.TryDequeue(out byte[] data))
            {
                if (data is null)
                    throw new ReplayDataException("Failed read operation, no mocked data available.");

                if (count < data.Length)
                    throw new ReplayDataException($"Failed read operation, expected at least {count} bytes, received {data.Length} bytes.");

                Buffer.BlockCopy(data, 0, buffer, offset, data.Length);

                return data.Length;
            }

            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_buffers.TryDequeue(out byte[] data))
                throw new ReplayDataException("Failed write operation, no more writes were expected.");

            if (data is null)
                throw new ReplayDataException("Failed write operation, data received is null.");

            if (count != data.Length)
                throw new ReplayDataException($"Failed write operation, expected {count} bytes, received {data.Length}.");
        }
    }
}
