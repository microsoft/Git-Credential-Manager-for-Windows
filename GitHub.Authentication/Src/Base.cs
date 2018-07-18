/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) GitHub Corporation
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

using Microsoft.Alm.Authentication;

namespace GitHub.Authentication
{
    public abstract class Base : Microsoft.Alm.Authentication.Base
    {
        protected Base(RuntimeContext context)
            : base(context)
        {
            // If the Gui service is already registered, then we do not need to allocate
            // and add a new one.
            var gui = GetService<IGui>();

            if (gui is null)
            {
                // Since there's no pre-existing Gui service registered with the current
                // context, we'll need to allocate and add one to it.
                gui = new Gui(Context);

                SetService(gui);
            }
        }

        protected IGui Gui
            => GetService<IGui>();
    }
}
