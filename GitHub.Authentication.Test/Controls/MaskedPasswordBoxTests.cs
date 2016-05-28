using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHub.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test.Controls
{
    [TestClass]
    public class MaskedPasswordBoxTests
    {
        [TestMethod]
        public void TextReplacedWithMaskCharacterAndPasswordPropertyContainsRealPassword()
        {
            var passwordTextInput = new MaskedPasswordBox();
            // Set the base Text property. The one that entering text into the UI would set.
            ((PromptTextBox)passwordTextInput).Text = "secr3t!";

            Assert.AreEqual("●●●●●●●", ((PromptTextBox)passwordTextInput).Text);
            Assert.AreEqual("secr3t!", passwordTextInput.Password);
        }
    }
}
