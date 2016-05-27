using System.ComponentModel;

namespace GitHub.Authentication.ViewModels.Validation
{
    public static class PropertyValidatorExtensions
    {
        public static PropertyValidator<TObject, string> IfNullOrEmpty<TObject>(this PropertyValidator<TObject, string> validator, string errorMessage)
            where TObject : INotifyPropertyChanged
        {
            return validator.IfTrue(string.IsNullOrEmpty, errorMessage);
        }
    }
}
