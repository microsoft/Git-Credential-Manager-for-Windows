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

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GitHub.Shared.Helpers
{
    public static class FocusHelper
    {
        /// <summary>
        /// Attempts to move focus to an element within the provided container waiting for the
        /// element to be loaded if necessary (waits max 1 second to protect against confusing focus
        /// shifts if the element gets loaded much later).
        /// </summary>
        /// <param name="element">The element to move focus from.</param>
        /// <param name="direction">The direction to give focus.</param>
        public static Task<bool> TryMoveFocus(this FrameworkElement element, FocusNavigationDirection direction)
        {
            return TryFocusImpl(element, e => e.MoveFocus(new TraversalRequest(direction)));
        }

        /// <summary>
        /// Attempts to move focus to the element, waiting for the element to be loaded if necessary
        /// (waits max 1 second to protect against confusing focus shifts if the element gets loaded
        /// much later).
        /// </summary>
        /// <param name="element">The element to give focus to.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "There's a Take(1) in there it'll be fine")]
        public static Task<bool> TryFocus(this FrameworkElement element)
        {
            return TryFocusImpl(element, e => e.Focus());
        }

        private static async Task<bool> TryFocusImpl(FrameworkElement element, Func<FrameworkElement, bool> focusAction)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return false;
            }

            var loadedElement = await WaitForElementLoaded(element);

            if (focusAction?.Invoke(element) ?? false)
                return true;

            // TODO: MoveFocus almost always requires its descendant elements to be fully loaded, we
            // have no way of knowing if they are so we should try again before bailing out.
            return false;
        }

        private static Task<FrameworkElement> WaitForElementLoaded(FrameworkElement element)
        {
            if (element.IsLoaded) return Task.FromResult(element);
            var taskCompletionSource = new TaskCompletionSource<FrameworkElement>();
            element.Loaded += (s, e) => taskCompletionSource.SetResult(element);
            return taskCompletionSource.Task;
        }
    }
}
