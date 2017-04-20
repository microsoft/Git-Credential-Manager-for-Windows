using System;

namespace Atlassian.Shared.Authentication.ViewModels.Validation
{
    public static class PropertyValidatorExtensions
    {
        public static PropertyValidator<string> Required(this PropertyValidator<string> validator, string errorMessage)
        {
            return validator.ValidIfTrue(value => !string.IsNullOrEmpty(value), errorMessage);
        }

        public static PropertyValidator<TProperty> ValidIfTrue<TProperty>(
            this PropertyValidator<TProperty> validator,
            Func<TProperty, bool> predicate,
            string errorMessage)
        {
            return new PropertyValidator<TProperty>(validator, value => predicate(value)
                ? PropertyValidationResult.Success
                : new PropertyValidationResult(ValidationStatus.Invalid, errorMessage));
        }
    }
}
