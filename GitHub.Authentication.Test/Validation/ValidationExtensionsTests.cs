using GitHub.Authentication.ViewModels.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test.Validation
{
    [TestClass]
    public class ValidationExtensionsTests
    {
        [TestMethod]
        public void IfNullOrEmptyIsUnvalidatedAndNotValidIfPropertyNeverChanges()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .IfNullOrEmpty("Please provide a value for SomeStringProperty!");

            var result = validator.ValidationResult;

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(PropertyValidationResult.Unvalidated, result);
        }

        [TestMethod]
        public void IfNullOrEmptyValidatesPropertyNotNull()
        {
            var validatableObject = new ValidatableTestObject();
            var validator = PropertyValidator
                .For(validatableObject, o => o.SomeStringProperty)
                .IfNullOrEmpty("Please provide a value for SomeStringProperty!");
            validatableObject.SomeStringProperty = "";

            var result = validator.ValidationResult;

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Please provide a value for SomeStringProperty!", result.Message);
        }
    }
}
