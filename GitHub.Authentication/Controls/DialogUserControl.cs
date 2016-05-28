using System.Windows.Controls;

namespace GitHub.UI
{
    public abstract class DialogUserControl : UserControl
    {
        public DialogUserControl()
        {
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    SetFocus();
                }
            };
        }

        protected abstract void SetFocus();
    }
}
