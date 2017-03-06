

using Atlassian.Bitbucket.Authentication.ViewModels;
using Atlassian.Shared.Controls;

namespace Atlassian.Bitbucket.Authentication.Views
{
    public partial class OAuthWindow : AuthenticationDialogWindow
    {
        public OAuthWindow()
        {
            InitializeComponent();
        }

        public OAuthViewModel ViewModel
        {
            get { return DataContext as OAuthViewModel; }
        }
    }
}
