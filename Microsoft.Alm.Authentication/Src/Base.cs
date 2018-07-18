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

namespace Microsoft.Alm.Authentication
{
    public abstract class Base
    {
        protected Base(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _context = context;
        }

        private readonly RuntimeContext _context;

        protected RuntimeContext Context
            => _context;

        protected INetwork Network
            => _context.Network;

        protected ISettings Settings
            => _context.Settings;

        protected IStorage Storage
            => _context.Storage;

        protected Git.ITrace Trace
            => _context.Trace;

        protected Git.IWhere Where
            => _context.Where;

        protected IEnumerable<IRuntimeService> EnumerateServices()
            => _context.EnumerateServices();

        protected T GetService<T>() where T : class, IRuntimeService
            => _context.GetService<T>();

        protected void SetService<T>(T service) where T : class, IRuntimeService
            => _context.SetService(service);
    }
}
