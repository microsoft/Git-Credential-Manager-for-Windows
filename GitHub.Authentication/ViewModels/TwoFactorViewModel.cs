namespace GitHub.Authentication.ViewModels
{
    public class TwoFactorViewModel : ViewModel
    {
        public TwoFactorViewModel()
        {
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AuthenticationCode))
                {
                    IsValid = AuthenticationCode.Length == 6;
                }
            };
        }

        bool _isValid;
        public bool IsValid
        {
            get { return _isValid; }
            set
            {
                _isValid = value;
                RaisePropertyChangedEvent(nameof(IsValid));
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
    }
}
