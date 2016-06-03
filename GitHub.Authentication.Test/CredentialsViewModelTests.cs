using GitHub.Authentication.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test
{
    [TestClass]
    public class CredentialsViewModelTests
    {
        [TestMethod]
        public void ValidatesLoginAndPassword()
        {
            var viewModel = new CredentialsViewModel();
            Assert.IsFalse(viewModel.LoginValidator.ValidationResult.IsValid);
            Assert.IsFalse(viewModel.PasswordValidator.ValidationResult.IsValid);

            viewModel.Login = "Tyrion";
            viewModel.Password = "staying alive";

            Assert.IsTrue(viewModel.LoginValidator.ValidationResult.IsValid);
            Assert.IsTrue(viewModel.PasswordValidator.ValidationResult.IsValid);
        }

        [TestMethod]
        public void IsValidWhenBothLoginAndPasswordIsValid()
        {
            var viewModel = new CredentialsViewModel();
            Assert.IsFalse(viewModel.ModelValidator.IsValid);
            viewModel.Login = "Tyrion";
            Assert.IsFalse(viewModel.ModelValidator.IsValid);

            viewModel.Password = "staying alive";

            Assert.IsTrue(viewModel.ModelValidator.IsValid);

            viewModel.Login = "";

            Assert.IsFalse(viewModel.ModelValidator.IsValid);
        }
    }
}
