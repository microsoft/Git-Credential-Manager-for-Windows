using GitHub.Authentication.Helpers;

namespace GitHub.Authentication.ViewModels
{
    /// <summary>
    /// Simple view model for the GitHub Two Factor dialog.
    /// </summary>
    public class TwoFactorViewModel : ViewModel
    {
        public TwoFactorViewModel()
        {
            PropertyChanged += (s, e) =>
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

        /// <summary>
        /// The Two-factor authentication code the user types in.
        /// </summary>
        public string AuthenticationCode
        {
            get { return _authenticationCode; }
            set
            {
                _authenticationCode = value;
                RaisePropertyChangedEvent(nameof(AuthenticationCode));
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

        public HyperLinkCommand LearnMoreCommand { get; }
            = new HyperLinkCommand();  
    }
}
