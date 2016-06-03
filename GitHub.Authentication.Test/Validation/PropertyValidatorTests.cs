using System.Collections.Generic;
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
                .ValidIfTrue(value => value == "Inigo Montoya", "Error occurred!");
            validatableObject.SomeStringProperty = "Inigo Montoya";

            var result = validator.ValidationResult;

            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
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

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Error occurred!", result.Message);
        }

        [TestMethod]
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

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Error occurred!", result.Message);
            Assert.AreEqual(4, results.Count);
            for (int i = 0; i < 4; i++) Assert.AreEqual(i, results[i]);
        }

        [TestMethod]
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
                if(e.PropertyName == nameof(validator.ValidationResult))
                    validationResult = validator.ValidationResult;
            };
            Assert.IsNull(validationResult); // Precondition

            validatableObject.SomeStringProperty = "not empty";

            Assert.AreEqual(validationResult, validator.ValidationResult);
            Assert.IsFalse(validationResult.IsValid);
        }
    }
}
