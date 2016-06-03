using GitHub.Authentication.ViewModels;
using GitHub.UI;

namespace GitHub.Authentication
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
