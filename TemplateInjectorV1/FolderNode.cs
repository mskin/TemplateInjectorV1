using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace TemplateInjectorV1
{
    public class FolderNode : INotifyPropertyChanged
    {
        public string Name { get; }
        public string FullPath { get; }
        public ObservableCollection<FolderNode> Children { get; }
        public ObservableCollection<FamilyListItem> Families { get; }

        // NEW: a concrete collection the TreeView can expand reliably
        public ObservableCollection<object> Nodes { get; }

        public FolderNode(string name, string fullPath,
                          IEnumerable<FamilyListItem> families,
                          IEnumerable<FolderNode> children)
        {
            Name = name;
            FullPath = fullPath;

            Families = new ObservableCollection<FamilyListItem>(families ?? Enumerable.Empty<FamilyListItem>());
            Children = new ObservableCollection<FolderNode>(children ?? Enumerable.Empty<FolderNode>());

            foreach (var f in Families)
                f.PropertyChanged += (s, e) => OnPropertyChanged(nameof(SelectedFamilyCount));

            // Build the combined list once; order = families first, then subfolders
            Nodes = new ObservableCollection<object>();
            foreach (var f in Families) Nodes.Add(f);
            foreach (var c in Children) Nodes.Add(c);
        }

        public int TotalFamilyCount => Families.Count + Children.Sum(c => c.TotalFamilyCount);
        public int SelectedFamilyCount => Families.Count(f => f.IsSelected) + Children.Sum(c => c.SelectedFamilyCount);

        public void SelectAll()
        {
            foreach (var f in Families) f.IsSelected = true;
            foreach (var c in Children) c.SelectAll();
            OnPropertyChanged(nameof(SelectedFamilyCount));
        }

        public void SelectNone()
        {
            foreach (var f in Families) f.IsSelected = false;
            foreach (var c in Children) c.SelectNone();
            OnPropertyChanged(nameof(SelectedFamilyCount));
        }

        public IEnumerable<FamilyListItem> GetSelectedFamilies()
        {
            foreach (var f in Families.Where(x => x.IsSelected)) yield return f;
            foreach (var c in Children) foreach (var f in c.GetSelectedFamilies()) yield return f;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            var h = PropertyChanged;
            if (h != null) h(this, new PropertyChangedEventArgs(name));
        }
    }
}
