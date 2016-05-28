using System.ComponentModel;
using System.Windows;
using GitHub.Authentication.ViewModels;

namespace GitHub.Authentication
{
    /// <summary>
    /// Interaction logic for CredentialsWindow.xaml
    /// </summary>
    public partial class CredentialsWindow : Window
    {
        public CredentialsWindow()
        {
            DataContextChanged += (s, e) =>
            {
                var oldViewModel = e.OldValue as CredentialsViewModel;
                if (oldViewModel != null)
                {
                    oldViewModel.PropertyChanged -= HandleCredentialResult;
                }
                ViewModel = e.NewValue as CredentialsViewModel;
                if (ViewModel != null)
                {
                    ViewModel.PropertyChanged += HandleCredentialResult;
                }
            };

            InitializeComponent();
        }

        public CredentialsViewModel ViewModel
        {
            get { return DataContext as CredentialsViewModel; }
            set { DataContext = value; }
        }

        void HandleCredentialResult(object sender, PropertyChangedEventArgs e)
        {
            var viewModel = sender as CredentialsViewModel;
            if (viewModel == null) return;
            if (e.PropertyName == nameof(CredentialsViewModel.Result))
            {
                if (viewModel.Result != AuthenticationDialogResult.None)
                {
                    Close();
                }
            }
        }
    }
}
