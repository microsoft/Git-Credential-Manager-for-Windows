using System;
using System.Windows.Controls;

namespace GitHub.UI
{
    public partial class TwoFactorControl : UserControl
    {
        public TwoFactorControl()
        {
            InitializeComponent();

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    SetFocus();
                }
            };
        }

        void SetFocus()
        {
            authenticationCode.TryFocus().Wait(TimeSpan.FromSeconds(1));
        }
    }
}
