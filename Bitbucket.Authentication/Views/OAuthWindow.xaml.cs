

using Bitbucket.Authentication.ViewModels;
using Core.Controls;

namespace Bitbucket.Authentication.Views
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
