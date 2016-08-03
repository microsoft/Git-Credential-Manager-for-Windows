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
        public const string MoreInformationUrl = "http://www.visualstudio.com/";

        internal PassphraseWindow(string resource)
        {
            InitializeComponent();

            _resource = resource;

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

            Keyboard.Focus(PassphraseTextBox);
        }

        protected void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Failure();
        }

        protected void MoreInfoLabel_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = MoreInformationUrl,
                UseShellExecute = true,
            });
        }

        protected void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Success();
        }

        private void Failure()
        {
            _cancelled = true;
            _passphrase = null;
        }

        private void Success()
        {
            string passphrase = PassphraseTextBox.Text;

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
