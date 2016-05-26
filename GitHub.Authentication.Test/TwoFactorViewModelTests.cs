using GitHub.Authentication.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test
{
    [TestClass]
    public class TwoFactorViewModelTests
    {
        [TestMethod]
        public void IsValid_IsTrueWhenAuthenticationCodeIsSixCharacters()
        {
            var vm = new TwoFactorViewModel();
            vm.AuthenticationCode = "012345";
            Assert.IsTrue(vm.IsValid);
        }

        [TestMethod]
        public void IsValid_IsFalseWhenAuthenticationCodeIsLessThanSixCharacters()
        {
            var vm = new TwoFactorViewModel();
            vm.AuthenticationCode = "01234";
            Assert.IsFalse(vm.IsValid);
        }
    }
}
