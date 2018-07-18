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
using System.Threading;

namespace Microsoft.Alm.Authentication.Test
{
    internal class CaptureStream : Stream
    {
        public CaptureStream(FileAccess access, Stream baseStream)
        {
            if (baseStream is null)
                throw new ArgumentNullException(nameof(baseStream));

            _access = access;
            _base = baseStream;
            _buffers = new ConcurrentQueue<byte[ ]>();
        }

        private readonly FileAccess _access;
        private Stream _base;
        private readonly ConcurrentQueue<byte[]> _buffers;

        public FileAccess Access
        {
            get { return _access; }
        }

        public override bool CanRead
        {
            get { return _base.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _base.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _base.CanWrite; }
        }

        public override long Length
        {
            get { return _base.Length; }
        }

        public override long Position
        {
            get { return _base.Position; }
            set { throw new NotSupportedException(); }
        }

        public override void Close()
        {
            base.Close();

            _base?.Close();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        protected override void Dispose(bool disposing)
        {
            Flush();

            if (disposing)
            {
                Stream stream;

                if ((stream = Interlocked.Exchange(ref _base, null)) != null)
                {
                    stream.Close();
                    try
                    {
                        stream.Dispose();
                    }
                    catch
                    { /* squelch */ }
                }
            }

            base.Dispose(disposing);
        }

        public override int Read(byte[ ] buffer, int offset, int count)
        {
            int read = _base.Read(buffer, offset, count);

            var bytes = new byte[read];

            Buffer.BlockCopy(buffer, offset, bytes, 0, read);

            _buffers.Enqueue(bytes);

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _base.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _base.SetLength(value);
        }

        public override void Write(byte[ ] buffer, int offset, int count)
        {
            _base.Write(buffer, offset, count);

            var bytes = new byte[count];

            Buffer.BlockCopy(buffer, offset, bytes, 0, count);

            _buffers.Enqueue(bytes);
        }

        public override void Flush()
        {
            try
            {
                _base?.Flush();
            }
            catch { /* squelch */ }
        }

        internal void GetCapturedData(ICapturedDataFilter filter, out List<string> capturedData)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            var buffers = new List<string>(_buffers.Count);

            foreach (var buffer in _buffers)
            {
                var encodedBuffer = Convert.ToBase64String(buffer);

                buffers.Add(encodedBuffer);
            }

            capturedData = buffers;
        }
    }
}
