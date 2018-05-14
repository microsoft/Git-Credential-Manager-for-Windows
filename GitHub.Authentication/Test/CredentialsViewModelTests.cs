using GitHub.Authentication.ViewModels;
using Xunit;

namespace GitHub.Authentication.Test
{
    public class CredentialsViewModelTests
    {
        [Fact]
        public void ValidatesLoginAndPassword()
        {
            var viewModel = new CredentialsViewModel();
            Assert.False(viewModel.LoginValidator.ValidationResult.IsValid);
            Assert.False(viewModel.PasswordValidator.ValidationResult.IsValid);

            viewModel.Login = "Tyrion";
            viewModel.Password = "staying alive";

            Assert.True(viewModel.LoginValidator.ValidationResult.IsValid);
            Assert.True(viewModel.PasswordValidator.ValidationResult.IsValid);
        }

        [Fact]
        public void IsValidWhenBothLoginAndPasswordIsValid()
        {
            var viewModel = new CredentialsViewModel();
            Assert.False(viewModel.ModelValidator.IsValid);
            viewModel.Login = "Tyrion";
            Assert.False(viewModel.ModelValidator.IsValid);

            viewModel.Password = "staying alive";

            Assert.True(viewModel.ModelValidator.IsValid);

            viewModel.Login = "";

            Assert.False(viewModel.ModelValidator.IsValid);
        }
    }
}
