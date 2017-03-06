using System.Windows.Input;
using Atlassian.Shared.Authentication.Helpers;
using Atlassian.Shared.Authentication.ViewModels;
using Atlassian.Shared.Authentication.ViewModels.Validation;
using Atlassian.Bitbucket.Authentication.Properties;

namespace Atlassian.Bitbucket.Authentication.ViewModels
{
    public class CredentialsViewModel : DialogViewModel
    {
        public CredentialsViewModel() : this(string.Empty)
        {
            // without this default constructor get nullreferenceexceptions during binding
            // i guess 'cos the view is built before the 'official' viewmodel and hence generates it own viewmodel while building?
        }
        public CredentialsViewModel(string username)
        {
            LoginCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Ok);
            CancelCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Cancel);

            LoginValidator = PropertyValidator.For(this, x => x.Login)
                .Required(Resources.LoginRequired);

            PasswordValidator = PropertyValidator.For(this, x => x.Password)
                .Required(Resources.PasswordRequired);

            ModelValidator = new ModelValidator(LoginValidator, PasswordValidator);
            ModelValidator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ModelValidator.IsValid))
                {
                    IsValid = ModelValidator.IsValid;
                }
            };

            // set last to allow validator to run
            if (!string.IsNullOrWhiteSpace(username))
            {
                Login = username;
            }
        }

        private string _login;
        /// <summary>
        /// Bitbucket login which is either the user name or email address.
        /// </summary>
        public string Login
        {
            get { return _login; }
            set
            {
                _login = value;
                RaisePropertyChangedEvent(nameof(Login));
            }
        }

        public PropertyValidator<string> LoginValidator { get; }

        private string _password;
        /// <summary>
        /// Bitbucket login which is either the user name or email address.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                // Hack: Because we're binding one way to source, we need to
                // skip the initial value that's sent when the binding is setup
                // by the XAML
                if (_password == null && value == null) return;
                _password = value;
                RaisePropertyChangedEvent(nameof(Password));
            }
        }

        public PropertyValidator<string> PasswordValidator { get; }

        public ModelValidator ModelValidator { get; }

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand HyperLinkCommand { get; } = new HyperLinkCommand();
        public ICommand ForgotPasswordCommand { get; } = new HyperLinkCommand();
    }
}
