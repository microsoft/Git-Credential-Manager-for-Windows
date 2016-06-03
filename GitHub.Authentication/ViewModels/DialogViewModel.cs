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
    }
}
