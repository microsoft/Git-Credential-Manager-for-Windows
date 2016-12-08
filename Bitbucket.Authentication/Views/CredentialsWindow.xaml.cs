

using Bitbucket.Authentication.ViewModels;
using Core.Controls;

namespace Bitbucket.Authentication.Views
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
