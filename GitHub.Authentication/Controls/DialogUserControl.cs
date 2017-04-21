using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GitHub.UI
{
    public abstract class DialogUserControl: UserControl
    {
        protected DialogUserControl()
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
