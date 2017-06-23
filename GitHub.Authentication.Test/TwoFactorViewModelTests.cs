using GitHub.Authentication.ViewModels;
using Xunit;

namespace GitHub.Authentication.Test
{
    public class TwoFactorViewModelTests
    {
        [Fact]
        public void IsValidIsTrueWhenAuthenticationCodeIsSixCharacters()
        {
            var vm = new TwoFactorViewModel();
            vm.AuthenticationCode = "012345";
            Assert.True(vm.IsValid);
        }

        [Fact]
        public void IsValidIsFalseWhenAuthenticationCodeIsLessThanSixCharacters()
        {
            var vm = new TwoFactorViewModel();
            vm.AuthenticationCode = "01234";
            Assert.False(vm.IsValid);
        }
    }
}
