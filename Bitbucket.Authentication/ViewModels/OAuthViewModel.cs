using System.Windows.Input;
using Atlassian.Shared.Authentication.Helpers;
using Atlassian.Shared.Authentication.ViewModels;

namespace Atlassian.Bitbucket.Authentication.ViewModels
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
