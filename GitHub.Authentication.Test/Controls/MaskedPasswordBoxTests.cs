using GitHub.Shared.Controls;
using Xunit;

namespace GitHub.Authentication.Test.Controls
{
    public class MaskedPasswordBoxTests
    {
        [WpfFact]
        public void TextReplacedWithMaskCharacterAndPasswordPropertyContainsRealPassword()
        {
            var passwordTextInput = new MaskedPasswordBox();
            // Set the base Text property. The one that entering text into the UI would set.
            ((PromptTextBox)passwordTextInput).Text = "secr3t!";

            Assert.Equal("●●●●●●●", ((PromptTextBox)passwordTextInput).Text);
            Assert.Equal("secr3t!", passwordTextInput.Password);
        }
    }
}
