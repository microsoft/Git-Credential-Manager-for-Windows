using System.Windows.Input;
using GitHub.Authentication.Helpers;
using GitHub.Authentication.Properties;
using GitHub.Authentication.ViewModels.Validation;

namespace GitHub.Authentication.ViewModels
{
    public class CredentialsViewModel : DialogViewModel
    {
        public CredentialsViewModel()
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
        }

        string _login;
        /// <summary>
        /// GitHub login which is either the user name or email address.
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

        string _password;
        /// <summary>
        /// GitHub login which is either the user name or email address.
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
        public ICommand SignUpCommand { get; } = new HyperLinkCommand();
    }
}
