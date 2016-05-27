using System.Security;
using GitHub.Authentication.Properties;
using GitHub.Authentication.ViewModels.Validation;

namespace GitHub.Authentication.ViewModels
{
    public class CredentialsViewModel : ViewModel
    {
        public CredentialsViewModel()
        {
            LoginValidator = PropertyValidator.For(this, x => x.Login)
                .Required(Resources.LoginRequired);

            PasswordValidator = PropertyValidator.For(this, x => x.Password)
                .Required(Resources.PasswordRequired);

            ModelValidator = new ModelValidator(LoginValidator, PasswordValidator);
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

        public PropertyValidator<string> LoginValidator { get; private set; }

        string _password;
        /// <summary>
        /// GitHub login which is either the user name or email address.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                RaisePropertyChangedEvent(nameof(Password));
            }
        }

        public PropertyValidator<string> PasswordValidator { get; private set; }

        public ModelValidator ModelValidator { get; private set; }
    }
}
