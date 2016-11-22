using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Core.Controls
{
    public abstract class DialogUserControl : UserControl
    {
        public DialogUserControl()
        {
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
                        new Action(delegate ()
                        {
                            SetFocus();
                        }));
                }
            };
        }

        protected abstract void SetFocus();
    }
}
