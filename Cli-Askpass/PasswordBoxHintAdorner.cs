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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Microsoft.Alm.Gui
{
    [System.Runtime.InteropServices.ComVisible(false)]
    public class PasswordBoxHintAdorner : Adorner
    {
        public PasswordBoxHintAdorner(UIElement adornedElement, string hintText, Style hintStyle, VisibilityDelegate visibilityCallback)
          : base(adornedElement)
        {
            _visibilityCallback = visibilityCallback;

            _label = new Label()
            {
                Content = hintText,
                Style = hintStyle,
            };

            IsHitTestVisible = true;
            Visibility = Visibility.Visible;

            if (adornedElement != null)
            {
                adornedElement.GotFocus += Invalidate;
                adornedElement.LostFocus += Invalidate;
                adornedElement.IsVisibleChanged += Invalidate;
            }

            _visualCollection = new VisualCollection(this);
            _contentPresenter = new ContentPresenter();
            _visualCollection.Add(_contentPresenter);
            _contentPresenter.Content = _label;

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            adornerLayer?.Add(this);

            IsHitTestVisible = false;
        }

        public object Content
        {
            get { lock (_syncpoint) return _contentPresenter.Content; }
            set { lock (_syncpoint) _contentPresenter.Content = value; }
        }

        protected override int VisualChildrenCount
        {
            get { lock (_syncpoint) return _visualCollection.Count; }
        }

        private readonly ContentPresenter _contentPresenter;
        private readonly Label _label;
        private readonly object _syncpoint = new object();
        private readonly VisibilityDelegate _visibilityCallback;
        private readonly VisualCollection _visualCollection;

        protected override Size ArrangeOverride(Size finalSize)
        {
            lock (_syncpoint)
            {
                _contentPresenter.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));

                return _contentPresenter.RenderSize;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            lock (_syncpoint)
            {
                return _visualCollection[index];
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            lock (_syncpoint)
            {
                _contentPresenter.Measure(constraint);
                return _contentPresenter.DesiredSize;
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
        }

        private void Invalidate()
        {
            if (_visibilityCallback != null)
            {
                Visibility = _visibilityCallback.Invoke();
            }

            InvalidateVisual();
        }

        private void Invalidate(object sender, EventArgs e)
            => Invalidate();

        private void Invalidate(object sender, DependencyPropertyChangedEventArgs e)
            => Invalidate();

        public delegate Visibility VisibilityDelegate();
    }
}
