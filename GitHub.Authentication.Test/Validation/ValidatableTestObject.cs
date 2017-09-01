using GitHub.Shared.ViewModels;

namespace GitHub.Authentication.Test.Validation
{
    public class ValidatableTestObject : ViewModel
    {
        private string _someStringProperty;

        public string SomeStringProperty
        {
            get { return _someStringProperty; }
            set
            {
                _someStringProperty = value;
                RaisePropertyChangedEvent(nameof(SomeStringProperty));
            }
        }

        private string _anotherStringProperty;

        public string AnotherStringProperty
        {
            get { return _anotherStringProperty; }
            set
            {
                _anotherStringProperty = value;
                RaisePropertyChangedEvent(nameof(AnotherStringProperty));
            }
        }
    }
}
