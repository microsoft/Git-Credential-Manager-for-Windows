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
using GitHub.Authentication.ViewModels;
using GitHub.Shared.Controls;
using GitHub.Shared.ViewModels;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Authentication.Test;

namespace GitHub.Authentication.Test
{
    public class CaptureGui : IGui, ICaptureService<CapturedGuiData>
    {
        internal const string FauxPassword = "is!realPassword?";
        internal const string FauxUsername = "tester";

        internal CaptureGui(RuntimeContext context, IGui gui)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));
            if (gui is null)
                throw new ArgumentNullException(nameof(gui));

            _captured = new Queue<CapturedGuiOperation>();
            _context = context;
            _gui = gui;
            _syncpoint = new object();
        }

        private readonly Queue<CapturedGuiOperation> _captured;
        private readonly RuntimeContext _context;
        private readonly IGui _gui;
        private readonly object _syncpoint;

        public string ServiceName
            => nameof(Gui);

        public Type ServiceType
            => typeof(IGui);

        public bool ShowViewModel(DialogViewModel viewModel, Func<AuthenticationDialogWindow> windowCreator)
        {
            _context.Trace.WriteLine($"capture {nameof(ShowViewModel)}.");

            var success = _gui.ShowViewModel(viewModel, windowCreator);

            Capture(success, viewModel);

            return success;
        }

        internal bool GetCapturedData(ICapturedDataFilter filter, out CapturedGuiData capturedData)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            lock (_syncpoint)
            {
                capturedData = new CapturedGuiData
                {
                    Operations = new List<CapturedGuiOperation>(_captured.Count),
                };

                foreach (var item in _captured)
                {
                    var operation = new CapturedGuiOperation
                    {
                        Output = new CapturedGuiOutput
                        {
                            AuthenticationCode = item.Output.AuthenticationCode,
                            IsValid = item.Output.IsValid,
                            Login = item.Output.Login != null
                                ? FauxUsername
                                : null,
                            Password = item.Output.Password != null
                                ? FauxPassword
                                : null,
                            Result = item.Output.Result,
                            Success = item.Output.Success,
                        },
                        DialogType = item.DialogType,
                    };

                    capturedData.Operations.Add(operation);
                }
            }

            return true;
        }

        private void Capture(bool success, DialogViewModel viewModel)
        {
            var capture = default(CapturedGuiOperation);

            switch (viewModel)
            {
                case CredentialsViewModel cvm:
                {
                    capture = new CapturedGuiOperation
                    {
                        Output = new CapturedGuiOutput
                        {
                            Login = cvm.Login,
                            IsValid = viewModel.IsValid,
                            Password = cvm.Password,
                            Result = (int)viewModel.Result,
                            Success = success,
                        },
                        DialogType = cvm.GetType().FullName,
                    };
                }
                break;

                case TwoFactorViewModel tfvm:
                {
                    capture = new CapturedGuiOperation
                    {
                        Output = new CapturedGuiOutput
                        {
                            AuthenticationCode = tfvm.AuthenticationCode,
                            IsValid = viewModel.IsValid,
                            Result = (int)viewModel.Result,
                            Success = success,
                        },
                        DialogType = tfvm.GetType().FullName,
                    };
                }
                break;

                default:
                    throw new ReplayDataException($"Unknown type `{viewModel.GetType().FullName}`");
            }

            lock (_syncpoint)
            {
                _captured.Enqueue(capture);
            }
        }

        bool ICaptureService<CapturedGuiData>.GetCapturedData(ICapturedDataFilter filter, out CapturedGuiData capturedData)
            => GetCapturedData(filter, out capturedData);

        bool ICaptureService.GetCapturedData(ICapturedDataFilter filter, out object capturedData)
        {
            if (GetCapturedData(filter, out CapturedGuiData guiData))
            {
                capturedData = guiData;
                return true;
            }

            capturedData = null;
            return false;
        }
    }
}
