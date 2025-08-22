using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TemplateInjectorV1
{
    public class ViewsFolderNode
    {
        public string Name { get; }
        public string FullPath { get; }
        public ObservableCollection<ViewsFolderNode> Children { get; }
        public ObservableCollection<RvtModelNode> Models { get; }

        // combined list for the TreeView (models first, then subfolders)
        public ObservableCollection<object> Nodes { get; }

        public ViewsFolderNode(string name, string fullPath,
                               IEnumerable<RvtModelNode> models,
                               IEnumerable<ViewsFolderNode> children)
        {
            Name = name;
            FullPath = fullPath;
            Models = new ObservableCollection<RvtModelNode>(models ?? Enumerable.Empty<RvtModelNode>());
            Children = new ObservableCollection<ViewsFolderNode>(children ?? Enumerable.Empty<ViewsFolderNode>());

            Nodes = new ObservableCollection<object>();
            foreach (var m in Models) Nodes.Add(m);
            foreach (var c in Children) Nodes.Add(c);
        }

        public int TotalModelCount => Models.Count + Children.Sum(c => c.TotalModelCount);

        public IEnumerable<ViewListItem> GetSelectedViews()
        {
            foreach (var m in Models) foreach (var v in m.GetSelected()) yield return v;
            foreach (var c in Children) foreach (var v in c.GetSelectedViews()) yield return v;
        }
    }
}
