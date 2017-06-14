using GitHub.Shared.ViewModels.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test.Validation
{
    [TestClass]
    public class ModelValidatorTests
    {
        [TestMethod]
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

            Assert.IsFalse(modelValidator.IsValid);

            validatableObject.SomeStringProperty = "valid";

            Assert.IsFalse(modelValidator.IsValid);

            validatableObject.AnotherStringProperty = "valid";

            Assert.IsTrue(modelValidator.IsValid);

            validatableObject.AnotherStringProperty = "";

            Assert.IsFalse(modelValidator.IsValid);
        }
    }
}
