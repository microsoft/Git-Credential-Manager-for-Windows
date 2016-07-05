using GitHub.Authentication.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test
{
    [TestClass]
    public class TwoFactorViewModelTests
    {
        [TestMethod]
        public void IsValidIsTrueWhenAuthenticationCodeIsSixCharacters()
        {
            var vm = new TwoFactorViewModel();
            vm.AuthenticationCode = "012345";
            Assert.IsTrue(vm.IsValid);
        }

        [TestMethod]
        public void IsValidIsFalseWhenAuthenticationCodeIsLessThanSixCharacters()
        {
            var vm = new TwoFactorViewModel();
            vm.AuthenticationCode = "01234";
            Assert.IsFalse(vm.IsValid);
        }
    }
}
