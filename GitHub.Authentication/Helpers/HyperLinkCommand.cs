using System;
using System.Diagnostics;
using System.Windows.Input;

namespace GitHub.Authentication.Helpers
{
    public class HyperLinkCommand : ICommand
    {
        bool _isEnabled = true;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _isEnabled;
        }

        public void Execute(object parameter)
        {
            var commandParameter = parameter as string;
            if (string.IsNullOrWhiteSpace(commandParameter)) return;

            Uri navigateUrl;

            if (Uri.TryCreate(commandParameter, UriKind.Absolute, out navigateUrl))
            {
                NavigateUrl(navigateUrl);
            }
        }

        void NavigateUrl(Uri navigateUrl)
        {
            if (CanExecute(navigateUrl))
            {
                Process.Start(new ProcessStartInfo(navigateUrl.AbsoluteUri));
            }
        }
    }
}
