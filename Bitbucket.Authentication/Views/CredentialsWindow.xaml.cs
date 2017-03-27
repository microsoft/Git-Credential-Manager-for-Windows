using Atlassian.Bitbucket.Authentication.ViewModels;
using Atlassian.Shared.Controls;

namespace Atlassian.Bitbucket.Authentication.Views
{
    /// <summary>
    /// Interaction logic for CredentialsWindow.xaml
    /// </summary>
    public partial class CredentialsWindow : AuthenticationDialogWindow
    {
        public CredentialsWindow()
        {
            InitializeComponent();
        }

        public CredentialsViewModel ViewModel
        {
            get { return DataContext as CredentialsViewModel; }
        }
    }
}
