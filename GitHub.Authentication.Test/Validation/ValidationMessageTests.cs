using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GitHub.Shared.ViewModels;
using GitHub.Shared.ViewModels.Validation;
using GitHub.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test.Validation
{
    [TestClass]
    public class ValidationMessageTests
    {
        [TestMethod]
        public void DoesNotShowErrorWhenUnvalidated()
        {
            var viewModel = new ValidatableTestObject();
            var testValidator = PropertyValidator.For(viewModel, x => x.SomeStringProperty)
                .Required("Please enter some text");
            var validationMessage = new ValidationMessage();

            validationMessage.Validator = testValidator;

            Assert.IsFalse(validationMessage.ShowError);
        }

        [TestMethod]
        public void ShowsErrorWhenValidationResultIsInvalid()
        {
            var viewModel = new ValidatableTestObject();
            var testValidator = PropertyValidator.For(viewModel, x => x.SomeStringProperty)
                .Required("Please enter some text");
            var validationMessage = new ValidationMessage();
            validationMessage.Validator = testValidator;

            viewModel.SomeStringProperty = "valid";
            viewModel.SomeStringProperty = "";

            Assert.AreEqual(ValidationStatus.Invalid, testValidator.ValidationResult.Status);
            Assert.IsTrue(validationMessage.ShowError);
        }

        [TestMethod]
        public void EndToEndTestShowsErrorMessageWhenReactiveValidatorIsNotValid()
        {
            var textBox = new TextBox();
            var viewModel = new ValidatableTestObject();
            var testValidator = PropertyValidator.For(viewModel, x => x.SomeStringProperty)
                .Required("Please enter some text");
            var validationMessage = new ValidationMessage();
            BindControlToViewModel(viewModel, nameof(viewModel.SomeStringProperty), textBox, TextBox.TextProperty);

            validationMessage.Validator = testValidator;

            Assert.IsFalse(validationMessage.ShowError);

            textBox.Text = "x";
            Assert.AreEqual("x", viewModel.SomeStringProperty);
            textBox.Text = "";
            Assert.AreEqual("", viewModel.SomeStringProperty);

            Assert.IsFalse(testValidator.ValidationResult.IsValid);
            Assert.IsTrue(validationMessage.ShowError);
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
