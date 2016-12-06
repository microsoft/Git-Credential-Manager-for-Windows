using Core.Authentication.Helpers;
using Core.Authentication.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Bitbucket.Authentication.ViewModels
{
    public class OAuthViewModel : DialogViewModel
    {
        private bool _resultType;

        public OAuthViewModel() : this(false) { }

        public OAuthViewModel(bool resultType)
        {
            this._resultType = resultType;

            OkCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Ok);
            CancelCommand = new ActionCommand(_ => Result = AuthenticationDialogResult.Cancel);

            // just a notification dialog so its always valid.
            IsValid = true;
        }

        public ICommand LearnMoreCommand { get; }
    = new HyperLinkCommand();

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }
    }
}
