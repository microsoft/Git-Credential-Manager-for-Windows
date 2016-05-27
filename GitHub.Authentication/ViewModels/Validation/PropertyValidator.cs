using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitHub.Authentication.ViewModels.Validation
{
    public abstract class PropertyValidator : ViewModel
    {
        public static PropertyValidator<TObject, TProperty> For<TObject, TProperty>(TObject source, Expression<Func<TObject, TProperty>> property)
            where TObject : INotifyPropertyChanged
        {
            return new PropertyValidator<TObject, TProperty>(source, property);
        }

        PropertyValidationResult _validationResult = PropertyValidationResult.Unvalidated;
        /// <summary>
        /// The current validation result for this validator.
        /// </summary>
        public PropertyValidationResult ValidationResult
        {
            get
            {
                return _validationResult;
            }
            protected set
            {
                _validationResult = value;
                RaisePropertyChangedEvent(nameof(ValidationResult));
            }
        }
    }

    public class PropertyValidator<TObject, TProperty> : PropertyValidator where TObject : INotifyPropertyChanged
    {
        // List of validators applied to this property.
        readonly List<Func<TProperty, PropertyValidationResult>> _validators =
            new List<Func<TProperty, PropertyValidationResult>>();

        public PropertyValidator(TObject source, Expression<Func<TObject, TProperty>> propertyExpression)
        {
            var compiledProperty = propertyExpression.Compile();
            var propertyInfo = GetPropertyInfo(propertyExpression);
            source.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == propertyInfo.Name)
                {
                    var currentValue = compiledProperty(source);
                    ValidationResult = ValidateAll(currentValue);
                }
            };
        }

        /// <summary>
        /// Adds a validator for this property that simply calls a predicate and returns the
        /// error message if the predicate returns false for the property value.
        /// </summary>
        /// <param name="predicateWithMessage"></param>
        /// <returns></returns>
        public PropertyValidator<TObject, TProperty> Add(Func<TProperty, string> predicateWithMessage)
        {
            _validators.Add(propertyValue => Validate(propertyValue, predicateWithMessage));
            return this;
        }

        public PropertyValidator<TObject, TProperty> IfTrue(Func<TProperty, bool> predicate, string errorMessage)
        {
            return Add(predicate, errorMessage);
        }

        PropertyValidator<TObject, TProperty> Add(Func<TProperty, bool> predicate, string errorMessage)
        {
            return Add(x => predicate(x) ? errorMessage : null);
        }

        PropertyValidationResult ValidateAll(TProperty currentValue)
        {
            var currentValidators = _validators.ToList(); // Make sure we don't mutate the list while validating.

            if (!currentValidators.Any())
            {
                return PropertyValidationResult.Unvalidated;
            }

            var result = currentValidators
                .Select(validator => validator(currentValue))
                .FirstOrDefault(x => x.Status == ValidationStatus.Invalid);

            return result ?? PropertyValidationResult.Success;
        }

        static PropertyValidationResult Validate(TProperty value, Func<TProperty, string> predicateWithMessage)
        {
            var result = predicateWithMessage(value);

            if (String.IsNullOrEmpty(result))
                return PropertyValidationResult.Success;

            return new PropertyValidationResult(ValidationStatus.Invalid, result);
        }

        static PropertyInfo GetPropertyInfo(Expression<Func<TObject, TProperty>> propertyExpression)
        {
            var member = propertyExpression.Body as MemberExpression;
            Debug.Assert(member != null, "Property expression doesn't refer to a member.");

            var propertyInfo = member.Member as PropertyInfo;
            Debug.Assert(propertyInfo != null, "Property expression does not refer to a property.");

            var propertyType = typeof(TObject);

            Debug.Assert(propertyType == propertyInfo.ReflectedType
                || propertyType.IsSubclassOf(propertyInfo.ReflectedType), "Property expression is not of the specified type");

            return propertyInfo;
        }
    }
}
