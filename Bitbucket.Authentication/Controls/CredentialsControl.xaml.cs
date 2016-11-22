using Core.Authentication.Helpers;
using Core.Controls;
using System;

namespace Bitbucket.Authentication.Controls
{
    /// <summary>
    /// Interaction logic for CredentialsControl.xaml
    /// </summary>
    public partial class CredentialsControl : DialogUserControl
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
