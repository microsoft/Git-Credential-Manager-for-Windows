using GitHub.Authentication.ViewModels;

namespace GitHub.Authentication.Test.Validation
{
    public class ValidatableTestObject : ViewModel
    {
        string _someStringProperty;
        public string SomeStringProperty
        {
            get { return _someStringProperty; }
            set
            {
                _someStringProperty = value;
                RaisePropertyChangedEvent(nameof(SomeStringProperty));
            }
        }

        string _anotherStringProperty;
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
