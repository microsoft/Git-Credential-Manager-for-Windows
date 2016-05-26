using GitHub.Authentication.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test
{
    [TestClass]
    public class ActionCommandTests
    {
        [TestMethod]
        public void CanExecute_IsTrueByDefault()
        {
            var command = new ActionCommand(_ => { });
            Assert.IsTrue(command.CanExecute(null));
        }

        [TestMethod]
        public void CanExecute_ReturnsFalseWhenIsEnabledIsFalse()
        {
            var command = new ActionCommand(_ => { }) { IsEnabled = false };
            Assert.IsFalse(command.CanExecute(null));
        }

        [TestMethod]
        public void Execute_CallsActionWhenExecuted()
        {
            var parameter = new object();
            object suppliedParameter = null;
            var command = new ActionCommand(_ => { suppliedParameter = parameter; }) { IsEnabled = true };
            command.Execute(parameter);

            Assert.AreSame(parameter, suppliedParameter);
        }
    }
}
