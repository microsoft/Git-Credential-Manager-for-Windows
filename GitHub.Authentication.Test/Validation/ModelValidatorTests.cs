using GitHub.Shared.ViewModels.Validation;
using Xunit;

namespace GitHub.Authentication.Test.Validation
{
    public class ModelValidatorTests
    {
        [Fact]
        public void IsValidWhenAllValidatorsAreValid()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Required("Error occurred!");
            var anotherValidator = PropertyValidator
                .For(validatableObject, o => o.AnotherStringProperty)
                .Required("Error occurred!");
            var modelValidator = new ModelValidator(validator, anotherValidator);

            Assert.False(modelValidator.IsValid);

            validatableObject.SomeStringProperty = "valid";

            Assert.False(modelValidator.IsValid);

            validatableObject.AnotherStringProperty = "valid";

            Assert.True(modelValidator.IsValid);

            validatableObject.AnotherStringProperty = "";

            Assert.False(modelValidator.IsValid);
        }
    }
}
