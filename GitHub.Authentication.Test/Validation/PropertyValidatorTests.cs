using System.Diagnostics;
using GitHub.Authentication.ViewModels.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test.Validation
{
    [TestClass]
    public class PropertyValidatorTests
    {
        [TestMethod]
        public void ValidationResultReturnsUnvalidatedIfNoValidators()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator.For(validatableObject, o => o.SomeStringProperty);

            var result = validator.ValidationResult;

            Assert.AreEqual(PropertyValidationResult.Unvalidated, result);
        }

        [TestMethod]
        public void ValidationResultReturnsSuccessIfValidatorsPass()
        {
            var validatableObject = new ValidatableTestObject();
            // Validator validates that the property is equal to the string "Inigo Montoya".
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Add(value => value != "Inigo Montoya" ? "Error occurred!" : null);
            validatableObject.SomeStringProperty = "Inigo Montoya";

            var result = validator.ValidationResult;

            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void ValidationResultReturnsFailureIfValidatorsFail()
        {
            var validatableObject = new ValidatableTestObject();
            // Validator validates that the property is equal to the string "Inigo Montoya".
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Add(value => value != "Inigo Montoya" ? "Error occurred!" : null);
            validatableObject.SomeStringProperty = "My name is not Inigo Montoya";

            var result = validator.ValidationResult;

            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void ValidationResultNotifiesWhenValidationStateChanges()
        {
            var validatableObject = new ValidatableTestObject();
            // Validator validates that the property is equal to the string "Inigo Montoya".
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Add(value => value != "Inigo Montoya" ? "Error occurred!" : null);
            PropertyValidationResult validationResult = null;
            validator.PropertyChanged += (s, e) =>
            {
                Debug.Assert(e.PropertyName == nameof(validator.ValidationResult), "There's only one property that could change");
                validationResult = validator.ValidationResult;
            };
            Assert.IsNull(validationResult); // Precondition

            validatableObject.SomeStringProperty = "not empty";

            Assert.AreEqual(validationResult, validator.ValidationResult);
            Assert.IsFalse(validationResult.IsValid);
        }
    }
}
