using System.ComponentModel;

namespace TemplateInjectorV1
{
    public class ViewListItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                var h = PropertyChanged;
                if (h != null) h(this, new PropertyChangedEventArgs("IsSelected"));
            }
        }

        public string Name { get; set; }        // view name in source
        public string Kind { get; set; }        // Drafting / Legend / Schedule
        public string UniqueId { get; set; }    // source view UniqueId
        public string SourcePath { get; set; }  // source RVT path

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
