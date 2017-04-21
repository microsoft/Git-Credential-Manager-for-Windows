using GitHub.Authentication.ViewModels;
using GitHub.UI;

namespace GitHub.Authentication
{
    public partial class TwoFactorWindow: AuthenticationDialogWindow
    {
        public TwoFactorWindow()
        {
            InitializeComponent();
        }

        public TwoFactorViewModel ViewModel
        {
            get { return DataContext as TwoFactorViewModel; }
        }
    }
}
