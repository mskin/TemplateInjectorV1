using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TemplateInjectorV1
{
    public class RvtModelNode
    {
        public string FileName { get; }
        public string FullPath { get; }
        public bool IsLoaded { get; set; }

        public ObservableCollection<ViewListItem> Views { get; }

        public RvtModelNode(string fileName, string fullPath)
        {
            FileName = fileName;
            FullPath = fullPath;
            Views = new ObservableCollection<ViewListItem>();
            IsLoaded = false;
        }

        public void SelectAll()
        {
            foreach (var v in Views) v.IsSelected = true;
        }

        public void SelectNone()
        {
            foreach (var v in Views) v.IsSelected = false;
        }

        public IEnumerable<ViewListItem> GetSelected() => Views.Where(v => v.IsSelected);
    }
}
