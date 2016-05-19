using System.Windows;
using GitHub.Authentication.ViewModels;

namespace GitHub_Authentication
{
    public partial class TwoFactorWindow : Window
    {
        public TwoFactorWindow()
        {
            InitializeComponent();
        }

        public TwoFactorViewModel ViewModel
        {
            get
            {
                return twoFactorControl.DataContext as TwoFactorViewModel;
            }
        }
    }
}
