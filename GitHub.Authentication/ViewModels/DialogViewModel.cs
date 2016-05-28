namespace GitHub.Authentication.ViewModels
{
    public class DialogViewModel : ViewModel
    {
        AuthenticationDialogResult _result = AuthenticationDialogResult.None;
        public AuthenticationDialogResult Result
        {
            get { return _result; }
            protected set
            {
                _result = value;
                RaisePropertyChangedEvent(nameof(Result));
            }
        }
    }
}
