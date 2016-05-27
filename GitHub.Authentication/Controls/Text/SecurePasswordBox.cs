using System.Diagnostics;
using System.Globalization;
using System.Windows.Controls;

namespace GitHub.UI
{
    /// <summary>
    ///   More secure PasswordBox based on TextBox control
    ///   http://blogs.ugidotnet.org/leonardo
    /// </summary>
    public class SecurePasswordBox : PromptTextBox
    {
        // Fake char to display in Visual Tree
        const char pwdChar = '●';

        // flag used to bypass OnTextChanged
        bool dirtyBaseText;

        /// <summary>
        ///   Only copy of real password
        /// </summary>
        /// <remarks>
        ///   For more security use System.Security.SecureString type instead
        /// </remarks>
        string password = string.Empty;

        /// <summary>
        ///   Provide access to base.Text without call OnTextChanged
        /// </summary>
        protected string BaseText
        {
            get { return base.Text; }
            set
            {
                dirtyBaseText = true;
                base.Text = value;
                dirtyBaseText = false;
            }
        }

        /// <summary>
        ///   Clean Password
        /// </summary>
        public new string Text
        {
            get { return password; }
            set
            {
                password = value ?? string.Empty;
                BaseText = new string(pwdChar, password.Length);
            }
        }

        /// <summary>
        ///   TextChanged event handler for secure storing of password into Visual Tree,
        ///   text is replaced with pwdChar chars, clean text is kept in
        ///   Text property (CLR property not snoopable without mod)
        /// </summary>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (dirtyBaseText)
                return;

            string currentText = BaseText;

            int selStart = SelectionStart;
            if (password != null && currentText.Length < password.Length)
            {
                // Remove deleted chars
                password = password.Remove(selStart, password.Length - currentText.Length);
            }
            if (!string.IsNullOrEmpty(currentText))
            {
                for (int i = 0; i < currentText.Length; i++)
                {
                    if (currentText[i] != pwdChar)
                    {
                        Debug.Assert(password != null, "Password can't be null here");
                        // Replace or insert char
                        string currentCharacter = currentText[i].ToString(CultureInfo.InvariantCulture);
                        password = BaseText.Length == password.Length ? password.Remove(i, 1).Insert(i, currentCharacter) : password.Insert(i, currentCharacter);
                    }
                }
                Debug.Assert(password != null, "Password can't be null here");
                BaseText = new string(pwdChar, password.Length);
                SelectionStart = selStart;
            }
            base.OnTextChanged(e);
        }
    }
}
