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

using System.ComponentModel;
using System.Windows;
using GitHub.Shared.ViewModels;

namespace GitHub.Shared.Controls
{
    public abstract class AuthenticationDialogWindow : Window
    {
        protected AuthenticationDialogWindow()
        {
            DataContextChanged += (s, e) =>
            {
                var oldViewModel = e.OldValue as ViewModel;
                if (oldViewModel != null)
                {
                    oldViewModel.PropertyChanged -= HandleDialogResult;
                }
                DataContext = e.NewValue;
                if (DataContext != null)
                {
                    ((ViewModel)DataContext).PropertyChanged += HandleDialogResult;
                }
            };
        }

        private void HandleDialogResult(object sender, PropertyChangedEventArgs e)
        {
            var viewModel = sender as DialogViewModel;
            if (viewModel == null) return;
            if (e.PropertyName == nameof(DialogViewModel.Result))
            {
                if (viewModel.Result != AuthenticationDialogResult.None)
                {
                    Close();
                }
            }
        }
    }
}
