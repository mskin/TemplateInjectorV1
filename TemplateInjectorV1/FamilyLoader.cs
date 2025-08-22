using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TemplateInjectorV1
{
    public static class FamilyLoader
    {
        public static List<FolderNode> BuildFamilyTreeRoots(string root, bool recursive)
        {
            var roots = new List<FolderNode>();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return roots;

            if (!recursive)
            {
                var node = BuildFolderNode(root, false);
                if (node != null) roots.Add(node);
                return roots;
            }

            var first = BuildFolderNode(root, true);
            if (first != null)
            {
                if (first.Children.Count > 0)
                {
                    foreach (var c in first.Children) roots.Add(c);
                }
                else
                {
                    roots.Add(first);
                }
            }
            return roots;
        }

        private static FolderNode BuildFolderNode(string folderPath, bool recurse)
        {
            // Only .rfa; .rvt/.rte/.rft ignored automatically
            var families = Directory.EnumerateFiles(folderPath, "*.rfa", SearchOption.TopDirectoryOnly)
                                    .Where(p => !IsInTempFolder(p))
                                    .Select(p => new FamilyListItem
                                    {
                                        FullPath = p,
                                        FileName = System.IO.Path.GetFileName(p),
                                        Directory = System.IO.Path.GetDirectoryName(p) ?? string.Empty,
                                        IsSelected = true
                                    })
                                    .ToList();

            var children = new List<FolderNode>();
            if (recurse)
            {
                foreach (var sub in Directory.EnumerateDirectories(folderPath))
                {
                    var child = BuildFolderNode(sub, true);
                    if (child != null) children.Add(child);
                }
            }

            if (families.Count == 0 && children.Count == 0) return null;

            var node = new FolderNode(System.IO.Path.GetFileName(folderPath), folderPath, families, children);
            return node;
        }

        private static bool IsInTempFolder(string filePath)
        {
            var parent = System.IO.Path.GetDirectoryName(filePath);
            var name = parent != null ? System.IO.Path.GetFileName(parent) : string.Empty;
            if (string.IsNullOrEmpty(name)) return false;

            // Skip common junk/backup folders
            return name.StartsWith("~$") || name.Equals("_backup", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
