namespace GitHub.Authentication.ViewModels
{
    public class TwoFactorViewModel : ViewModel
    {
        bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                RaisePropertyChangedEvent(nameof(IsBusy));
            }
        }

        string _authenticationCode;
        public string AuthenticationCode
        {
            get { return _authenticationCode; }
            set
            {
                _authenticationCode = value;
                RaisePropertyChangedEvent(nameof(AuthenticationCode));
            }
        }

        bool _AuthenticationCodeSent;
        public bool AuthenticationCodeSent
        {
            get { return _AuthenticationCodeSent; }
            private set
            {
                _AuthenticationCodeSent = value;
                RaisePropertyChangedEvent(nameof(AuthenticationCodeSent));
            }
        }

        bool _isSms;
        public bool IsSms
        {
            get { return _isSms; }
            private set
            {
                _isSms = value;
                RaisePropertyChangedEvent(nameof(IsSms));
            }
        }

        string _description;
        public string Description
        {
            get { return _description; }
            private set
            {
                _description = value;
                RaisePropertyChangedEvent(nameof(Description));
            }
        }

        bool _showErrorMessage;
        public bool ShowErrorMessage
        {
            get { return _showErrorMessage; }
            private set
            {
                _showErrorMessage = value;
                RaisePropertyChangedEvent(nameof(ShowErrorMessage));
            }
        }
    }
}
