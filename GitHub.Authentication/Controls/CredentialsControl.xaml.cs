using System;
using GitHub.Extensions;

namespace GitHub.UI
{
    /// <summary>
    /// Interaction logic for CredentialsControl.xaml
    /// </summary>
    public partial class CredentialsControl: DialogUserControl
    {
        public CredentialsControl()
        {
            InitializeComponent();
        }

        protected override void SetFocus()
        {
            loginTextBox.TryFocus().Wait(TimeSpan.FromSeconds(1));
        }
    }
}
