using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GitHub.Shared.ViewModels;
using GitHub.Shared.ViewModels.Validation;
using GitHub.UI;
using Xunit;

namespace GitHub.Authentication.Test.Validation
{
    public class ValidationMessageTests
    {
        [WpfFact]
        public void DoesNotShowErrorWhenUnvalidated()
        {
            var viewModel = new ValidatableTestObject();
            var testValidator = PropertyValidator.For(viewModel, x => x.SomeStringProperty)
                .Required("Please enter some text");
            var validationMessage = new ValidationMessage();

            validationMessage.Validator = testValidator;

            Assert.False(validationMessage.ShowError);
        }

        [WpfFact]
        public void ShowsErrorWhenValidationResultIsInvalid()
        {
            var viewModel = new ValidatableTestObject();
            var testValidator = PropertyValidator.For(viewModel, x => x.SomeStringProperty)
                .Required("Please enter some text");
            var validationMessage = new ValidationMessage();
            validationMessage.Validator = testValidator;

            viewModel.SomeStringProperty = "valid";
            viewModel.SomeStringProperty = "";

            Assert.Equal(ValidationStatus.Invalid, testValidator.ValidationResult.Status);
            Assert.True(validationMessage.ShowError);
        }

        [WpfFact]
        public void EndToEndTestShowsErrorMessageWhenReactiveValidatorIsNotValid()
        {
            var textBox = new TextBox();
            var viewModel = new ValidatableTestObject();
            var testValidator = PropertyValidator.For(viewModel, x => x.SomeStringProperty)
                .Required("Please enter some text");
            var validationMessage = new ValidationMessage();
            BindControlToViewModel(viewModel, nameof(viewModel.SomeStringProperty), textBox, TextBox.TextProperty);

            validationMessage.Validator = testValidator;

            Assert.False(validationMessage.ShowError);

            textBox.Text = "x";
            Assert.Equal("x", viewModel.SomeStringProperty);
            textBox.Text = "";
            Assert.Equal("", viewModel.SomeStringProperty);

            Assert.False(testValidator.ValidationResult.IsValid);
            Assert.True(validationMessage.ShowError);
        }

        private void BindControlToViewModel(ViewModel viewModel, string viewModelProperty, FrameworkElement control, DependencyProperty controlProperty)
        {
            var binding = new Binding
            {
                Source = viewModel,
                Path = new PropertyPath(viewModelProperty),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            control.SetBinding(controlProperty, binding);
        }
    }
}
