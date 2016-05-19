using System.Windows.Input;
using GitHub.Authentication.Helpers;
using GitHub.Authentication.Properties;

namespace GitHub.Authentication.ViewModels
{
    /// <summary>
    /// Simple view model for the GitHub Two Factor dialog.
    /// </summary>
    public class TwoFactorViewModel : ViewModel
    {
        /// <summary>
        /// This is used by the GitHub.Authentication test application
        /// </summary>
        public TwoFactorViewModel() : this(false) { }

        /// <summary>
        /// This construc
        /// </summary>
        /// <param name="isSms">True if the 2fa authentication code is sent via SMS</param>
        public TwoFactorViewModel(bool isSms)
        {
            OkCommand = new ActionCommand(_ => Result = TwoFactorResult.Ok);

            IsSms = isSms;
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

        public bool IsSms
        {
            get; private set;
        }

        public string Description
        {
            get
            {
                return IsSms
                    ? Resources.TwoFactorSms
                    : Resources.OpenTwoFactorAuthAppText;
            }
        }

        TwoFactorResult _result = TwoFactorResult.None;
        public TwoFactorResult Result
        {
            get { return _result; }
            set
            {
                _result = value;
                RaisePropertyChangedEvent(nameof(Result));
            }
        }

        public ICommand LearnMoreCommand { get; }
            = new HyperLinkCommand();

        public ICommand OkCommand { get; private set; }
    }

    public enum TwoFactorResult
    {
        None,
        Ok,
        Cancel
    }
}
