using System.Collections.Generic;
using GitHub.Shared.ViewModels.Validation;
using Xunit;

namespace GitHub.Authentication.Test.Validation
{
    public class PropertyValidatorTests
    {
        [Fact]
        public void ValidationResultReturnsUnvalidatedIfNoValidators()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator.For(validatableObject, o => o.SomeStringProperty);

            var result = validator.ValidationResult;

            Assert.Equal(PropertyValidationResult.Unvalidated, result);
        }

        [Fact]
        public void ValidationResultReturnsSuccessIfValidatorsPass()
        {
            var validatableObject = new ValidatableTestObject();
            // Validator validates that the property is equal to the string "Inigo Montoya".
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .ValidIfTrue(value => value == "Inigo Montoya", "Error occurred!");
            validatableObject.SomeStringProperty = "Inigo Montoya";

            var result = validator.ValidationResult;

            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidationResultReturnsFailureIfAnyValidatorsFail()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Required("String may not be null or empty")
                .ValidIfTrue(value => value == "Inigo Montoya", "Error occurred!")
                .ValidIfTrue(value => value == "My name is not Inigo Montoya", "Doh!");
            validatableObject.SomeStringProperty = "My name is not Inigo Montoya";

            var result = validator.ValidationResult;

            Assert.False(result.IsValid);
            Assert.Equal("Error occurred!", result.Message);
        }

        [Fact]
        public void ValidatorsRunInOrderAndStopWhenInvalid()
        {
            List<int> results = new List<int>();
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .ValidIfTrue(value => { results.Add(0); return true; }, "Error occurred!")
                .ValidIfTrue(value => { results.Add(1); return true; }, "Error occurred!")
                .ValidIfTrue(value => { results.Add(2); return true; }, "Error occurred!")
                .ValidIfTrue(value => { results.Add(3); return false; }, "Error occurred!")
                .ValidIfTrue(value => { results.Add(4); return true; }, "Error occurred!");
            validatableObject.SomeStringProperty = "My name is not Inigo Montoya";

            var result = validator.ValidationResult;

            Assert.False(result.IsValid);
            Assert.Equal("Error occurred!", result.Message);
            Assert.Equal(4, results.Count);
            for (int i = 0; i < 4; i++) Assert.Equal(i, results[i]);
        }

        [Fact]
        public void ValidationResultNotifiesWhenValidationStateChanges()
        {
            var validatableObject = new ValidatableTestObject();
            // Validator validates that the property is equal to the string "Inigo Montoya".
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .ValidIfTrue(value => value == "Inigo Montoya", "Error occurred!");
            PropertyValidationResult validationResult = null;
            validator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(validator.ValidationResult))
                    validationResult = validator.ValidationResult;
            };
            Assert.Null(validationResult); // Precondition

            validatableObject.SomeStringProperty = "not empty";

            Assert.Equal(validationResult, validator.ValidationResult);
            Assert.False(validationResult.IsValid);
        }
    }
}
