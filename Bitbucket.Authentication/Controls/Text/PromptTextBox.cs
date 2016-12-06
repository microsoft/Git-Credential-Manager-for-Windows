using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Bitbucket.Authentication.Controls.Text
{
    public class PromptTextBox : TextBox
    {
        public static readonly DependencyProperty PromptTextProperty =
            DependencyProperty.Register(nameof(PromptText), typeof(string), typeof(PromptTextBox), new UIPropertyMetadata(""));

        [Localizability(LocalizationCategory.Text)]
        [DefaultValue("")]
        public string PromptText
        {
            get { return (string)GetValue(PromptTextProperty); }
            set { SetValue(PromptTextProperty, value); }
        }
    }
}
