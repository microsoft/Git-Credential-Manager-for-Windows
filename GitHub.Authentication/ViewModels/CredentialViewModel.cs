using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Authentication.ViewModels
{
    public class CredentialViewModel : ViewModel
    {
        string _login;
        /// <summary>
        /// The user's login or email address.
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

        SecureString _password;
        /// <summary>
        /// The user's password.
        /// </summary>
        public SecureString Password
        {
            get { return _password; }
            set
            {
                _password = value;
                RaisePropertyChangedEvent(nameof(Password));
            }
        }
    }
}
