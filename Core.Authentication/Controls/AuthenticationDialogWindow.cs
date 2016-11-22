using System.ComponentModel;
using System.Windows;
using Core.Authentication.ViewModels;

namespace Core.Controls
{
    public abstract class AuthenticationDialogWindow : Window
    {
        public AuthenticationDialogWindow()
        {
            DataContextChanged += (s, e) =>
            {
                var oldViewModel = e.OldValue as ViewModel;
                if (oldViewModel != null)
                {
                    oldViewModel.PropertyChanged -= HandleDialogResult;
                }
                DataContext = e.NewValue;
                if (DataContext != null)
                {
                    ((ViewModel)DataContext).PropertyChanged += HandleDialogResult;
                }
            };
        }

        private void HandleDialogResult(object sender, PropertyChangedEventArgs e)
        {
            var viewModel = sender as DialogViewModel;
            if (viewModel == null) return;
            if (e.PropertyName == nameof(DialogViewModel.Result))
            {
                if (viewModel.Result != AuthenticationDialogResult.None)
                {
                    Close();
                }
            }
        }
    }
}
