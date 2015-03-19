using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public partial class CredentialForm : Form
    {
        public CredentialForm(Uri targetUri)
        {
            InitializeComponent();

            _uriLabel.Text = targetUri.AbsoluteUri;
        }

        public string Password { get; private set; }
        public string Username { get; private set; }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.Password = _passwordTextbox.Text;
            this.Username = _usernameTextbox.Text;
            this.Close();
        }
    }
}
