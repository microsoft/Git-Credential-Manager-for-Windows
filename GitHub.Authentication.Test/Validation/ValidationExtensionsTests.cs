using GitHub.Authentication.ViewModels.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test.Validation
{
    [TestClass]
    public class ValidationExtensionsTests
    {
        [TestMethod]
        public void RequiredIsUnvalidatedAndNotValidIfPropertyNeverChanges()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Required("Please provide a value for SomeStringProperty!");

            var result = validator.ValidationResult;

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(PropertyValidationResult.Unvalidated, result);
        }

        [TestMethod]
        public void RequiredValidatesPropertyNotNull()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Required("Please provide a value for SomeStringProperty!");
            validatableObject.SomeStringProperty = "";
            validatableObject.SomeStringProperty = null;

            var result = validator.ValidationResult;

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Please provide a value for SomeStringProperty!", result.Message);
        }

        [TestMethod]
        public void RequiredValidatesPropertyNotEmpty()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .Required("Please provide a value for SomeStringProperty!");
            validatableObject.SomeStringProperty = "";

            var result = validator.ValidationResult;

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Please provide a value for SomeStringProperty!", result.Message);
        }
    }
}
