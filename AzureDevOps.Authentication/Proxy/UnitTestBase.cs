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

using System.Runtime.CompilerServices;
using Microsoft.Alm.Authentication.Test;

namespace AzureDevOps.Authentication.Test
{
    public class UnitTestBase : Microsoft.Alm.Authentication.Test.UnitTestBase
    {
        protected UnitTestBase(IUnitTestTrace output, string projectDirectory, [CallerFilePath] string filePath = "")
            : base(output, projectDirectory, filePath)
        {
            if (GetService<IAdal>() is null)
            {
                SetService(new Adal(Context));
            }
        }

        protected UnitTestBase(IUnitTestTrace output, [CallerFilePath] string filePath = "")
            : this(output, null, filePath)
        { }

        protected override void InitializeTest(int iteration = -1, [CallerMemberName] string testName = "")
        {
            switch (TestMode)
            {
                case UnitTestMode.Capture:
                {
                    var serviceAdal = GetService<IAdal>();
                    var captureAdal = new CaptureAdal(Context, serviceAdal);

                    SetService(captureAdal);
                }
                break;

                case UnitTestMode.NoProxy: break;

                case UnitTestMode.Replay:
                {
                    var replayAdal = new ReplayAdal(Context);

                    SetService(replayAdal);
                }
                break;
            }

            base.InitializeTest(iteration, testName);
        }
    }
}
