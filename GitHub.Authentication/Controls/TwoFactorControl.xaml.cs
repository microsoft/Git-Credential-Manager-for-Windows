using System;
using GitHub.Authentication.Controls;

namespace GitHub.UI
{
    public partial class TwoFactorControl : DialogUserControl
    {
        public TwoFactorControl()
        {
            InitializeComponent();
        }

        protected override void SetFocus()
        {
            authenticationCode.TryFocus().Wait(TimeSpan.FromSeconds(1));
        }
    }
}
