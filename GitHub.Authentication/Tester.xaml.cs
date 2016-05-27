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
            new CredentialsWindow().ShowDialog();
        }

        private void ShowAuthenticationCode(object sender, RoutedEventArgs e)
        {
            new TwoFactorWindow().ShowDialog();
        }
    }
}
