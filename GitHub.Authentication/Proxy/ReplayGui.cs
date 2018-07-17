using System;
using System.Collections.Generic;
using GitHub.Authentication.ViewModels;
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

using GitHub.Shared.Controls;
using GitHub.Shared.ViewModels;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Authentication.Test;
using static System.StringComparer;

namespace GitHub.Authentication.Test
{
    public class ReplayGui : IGui, IReplayService<CapturedGuiData>
    {
        internal ReplayGui(RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _captured = new Queue<CapturedGuiOperation>();
            _context = context;
            _replayed = new Stack<CapturedGuiOperation>();
            _syncpoint = new object();
        }

        private readonly Queue<CapturedGuiOperation> _captured;
        private readonly RuntimeContext _context;
        private readonly Stack<CapturedGuiOperation> _replayed;
        private readonly object _syncpoint;

        public string ServiceName
            => nameof(Gui);

        public Type ServiceType
            => typeof(IGui);

        public bool ShowViewModel(DialogViewModel viewModel, Func<AuthenticationDialogWindow> windowCreator)
        {
            if (!TryGetNext(out CapturedGuiOperation operation))
                throw new ReplayNotFoundException($"Failed to find next `{nameof(CapturedGuiOperation)}`.");
            if (!Ordinal.Equals(viewModel?.GetType().FullName, operation.DialogType))
                throw new ReplayInputTypeException($"Expected `{viewModel?.GetType().FullName}` vs. Actual `{operation.DialogType}`.");

            _context.Trace.WriteLine($"replay {nameof(ShowViewModel)}.");

            viewModel.IsValid = operation.Output.IsValid;
            viewModel.Result = (AuthenticationDialogResult)operation.Output.Result;

            switch (viewModel)
            {
                case CredentialsViewModel cvm:
                {
                    cvm.Login = operation.Output.Login;
                    cvm.Password = operation.Output.Password;
                }
                break;

                case TwoFactorViewModel tfvm:
                {
                    tfvm.AuthenticationCode = operation.Output.AuthenticationCode;
                }
                break;
            }

            return operation.Output.Success;
        }

        internal void SetReplayData(CapturedGuiData replayData)
        {
            lock (_syncpoint)
            {
                _captured.Clear();

                if (replayData.Operations != null)
                {
                    foreach (var operation in replayData.Operations)
                    {
                        _captured.Enqueue(operation);
                    }
                }

                _captured.TrimExcess();
            }
        }

        private bool TryGetNext(out CapturedGuiOperation operation)
        {
            lock (_syncpoint)
            {
                if (_captured.Count > 0)
                {
                    operation = _captured.Dequeue();
                    return true;
                }
            }

            operation = default(CapturedGuiOperation);
            return false;
        }

        void IReplayService<CapturedGuiData>.SetReplayData(CapturedGuiData replayData)
            => SetReplayData(replayData);

        void IReplayService.SetReplayData(object replayData)
        {
            if (!(replayData is CapturedGuiData guiData)
                && !CapturedGuiData.TryDeserialize(replayData, out guiData))
            {
                var inner = new System.IO.InvalidDataException($"Failed to deserialize data into `{nameof(CapturedGuiData)}`.");
                throw new ArgumentException(inner.Message, nameof(replayData), inner);
            }

            SetReplayData(guiData);
        }
    }
}
