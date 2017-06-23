using GitHub.Shared.Helpers;
using Xunit;

namespace GitHub.Authentication.Test
{
    public class ActionCommandTests
    {
        [Fact]
        public void CanExecuteIsTrueByDefault()
        {
            var command = new ActionCommand(_ => { });
            Assert.True(command.CanExecute(null));
        }

        [Fact]
        public void CanExecuteReturnsFalseWhenIsEnabledIsFalse()
        {
            var command = new ActionCommand(_ => { }) { IsEnabled = false };
            Assert.False(command.CanExecute(null));
        }

        [Fact]
        public void ExecuteCallsActionWhenExecuted()
        {
            var parameter = new object();
            object suppliedParameter = null;
            var command = new ActionCommand(_ => { suppliedParameter = parameter; }) { IsEnabled = true };
            command.Execute(parameter);

            Assert.Same(parameter, suppliedParameter);
        }
    }
}
