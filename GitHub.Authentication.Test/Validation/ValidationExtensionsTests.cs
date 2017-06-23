using GitHub.Shared.ViewModels.Validation;
using Xunit;

namespace GitHub.Authentication.Test.Validation
{
    public class ValidationExtensionsTests
    {
        [Fact]
        public void RequiredIsUnvalidatedAndNotValidIfPropertyNeverChanges()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Required("Please provide a value for SomeStringProperty!");

            var result = validator.ValidationResult;

            Assert.False(result.IsValid);
            Assert.Equal(PropertyValidationResult.Unvalidated, result);
        }

        [Fact]
        public void RequiredValidatesPropertyNotNull()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Required("Please provide a value for SomeStringProperty!");
            validatableObject.SomeStringProperty = "";
            validatableObject.SomeStringProperty = null;

            var result = validator.ValidationResult;

            Assert.False(result.IsValid);
            Assert.Equal("Please provide a value for SomeStringProperty!", result.Message);
        }

        [Fact]
        public void RequiredValidatesPropertyNotEmpty()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Required("Please provide a value for SomeStringProperty!");
            validatableObject.SomeStringProperty = "";

            var result = validator.ValidationResult;

            Assert.False(result.IsValid);
            Assert.Equal("Please provide a value for SomeStringProperty!", result.Message);
        }
    }
}
