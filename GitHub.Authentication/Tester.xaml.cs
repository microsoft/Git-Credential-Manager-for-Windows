using System.Windows;
using GitHub.Authentication.ViewModels;

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
            var credentialsWindow = new CredentialsWindow();
            var vm = credentialsWindow.DataContext as CredentialsViewModel;
            vm.Login = "test";
            credentialsWindow.ShowDialog();
        }

        private void ShowAuthenticationCode(object sender, RoutedEventArgs e)
        {
            new TwoFactorWindow().ShowDialog();
        }
    }
}
