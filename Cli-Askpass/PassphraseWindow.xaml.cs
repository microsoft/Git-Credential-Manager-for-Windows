using System.Windows;
using System.Windows.Input;

namespace Microsoft.Alm.Gui
{
    /// <summary>
    /// Interaction logic for CredentialControl.xaml
    /// </summary>
    [System.Runtime.InteropServices.ComVisible(false)]
    public partial class PassphraseWindow : Window
    {
        public const string HintText = "SSH Passphrase";

        internal PassphraseWindow(string resource)
        {
            InitializeComponent();

            _resource = resource;

            Loaded += (sender, args) =>
            {

                var style = Resources["FadedLabelStyle"] as Style;

                _textboxAdorner = new PasswordBoxHintAdorner(PassphrasePasswordBox, HintText, style, IsAdornerVisible);
            };

            DataContext = this;
        }

        public bool Cancelled
        {
            get { return _cancelled; }
        }
        private bool _cancelled;

        public string Passphrase
        {
            get { return _passphrase; }
        }
        private string _passphrase;

        public string Resource
        {
            get { return _resource; }
        }
        private readonly string _resource;

        private PasswordBoxHintAdorner _textboxAdorner;

        protected override void OnKeyUp(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    Success();
                    break;

                case Key.Escape:
                    Failure();
                    break;

                default:
                    base.OnKeyUp(e);
                    break;
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            Keyboard.Focus(PassphrasePasswordBox);
        }

        protected void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Failure();
            Close();
        }

        protected void MoreInfoLabel_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = Cli.Program.DefinitionUrlPassphrase,
                UseShellExecute = true,
            });
        }

        protected void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Success();
            Close();
        }

        private void Failure()
        {
            _cancelled = true;
            _passphrase = null;
        }

        private Visibility IsAdornerVisible()
        {
            return (PassphrasePasswordBox.IsFocused || PassphrasePasswordBox.Password.Length > 0)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void Success()
        {
            string passphrase = PassphrasePasswordBox.Password;

            if (string.IsNullOrWhiteSpace(passphrase))
            {
                _cancelled = false;
                _passphrase = null;
            }
            else
            {
                _cancelled = false;
                _passphrase = passphrase;
            }
        }
    }
}
