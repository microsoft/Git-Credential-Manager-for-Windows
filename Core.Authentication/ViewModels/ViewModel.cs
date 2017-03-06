using System.ComponentModel;

namespace Core.Authentication.ViewModels
{
    /// <summary>
    /// Rather than bring in all the overhead of an MVVM framework, we'll just do the
    /// simplest possible thing.
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChangedEvent(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
