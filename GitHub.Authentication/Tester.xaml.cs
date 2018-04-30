using Microsoft.Alm.Authentication;
using System;
using System.Windows;

namespace GitHub.Authentication
{
    /// <summary>
    /// Interaction logic for Tester.xaml
    /// </summary>
    public partial class Tester : Window
    {
        public Tester()
        {
            InitializeComponent();
        }

        private void ShowCredentials(object sender, RoutedEventArgs e)
        {
            new CredentialsWindow(RuntimeContext.Default, IntPtr.Zero).ShowDialog();
        }

        private void ShowAuthenticationCode(object sender, RoutedEventArgs e)
        {
            new TwoFactorWindow(RuntimeContext.Default, IntPtr.Zero).ShowDialog();
        }
    }
}
