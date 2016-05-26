using System.ComponentModel;
using System.Windows;
using GitHub.Authentication.ViewModels;

namespace GitHub_Authentication
{
    public partial class TwoFactorWindow : Window
    {
        public TwoFactorWindow()
        {
            DataContextChanged += (s, e) =>
            {
                var oldViewModel = e.OldValue as TwoFactorViewModel;
                if (oldViewModel != null)
                {
                    oldViewModel.PropertyChanged -= HandleTwoFactorResult;
                }
                ViewModel = e.NewValue as TwoFactorViewModel;
                if (ViewModel != null)
                {
                    ViewModel.PropertyChanged += HandleTwoFactorResult;
                }
            };

            InitializeComponent();
        }

        public TwoFactorViewModel ViewModel
        {
            get { return DataContext as TwoFactorViewModel; }
            set { DataContext = value; }
        }

        void HandleTwoFactorResult(object sender, PropertyChangedEventArgs e)
        {
            var viewModel = sender as TwoFactorViewModel;
            if (viewModel == null) return;
            if (e.PropertyName == nameof(TwoFactorViewModel.Result))
            {
                if (viewModel.Result != TwoFactorResult.None)
                {
                    Close();
                }
            }
        }
    }
}
