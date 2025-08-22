
// =====================================
// 7) Models/FamilyListItem.cs
// =====================================
using System.ComponentModel;

namespace TemplateInjectorV1
{
    public class FamilyListItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
        public string FileName { get; set; }
        public string Directory { get; set; }
        public string FullPath { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
