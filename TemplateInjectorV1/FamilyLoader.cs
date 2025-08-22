using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TemplateInjectorV1
{
    public static class FamilyLoader
    {
        // -------- Families (RFA) --------
        public static List<FolderNode> BuildFamilyTreeRoots(string root, bool recursive)
        {
            var roots = new List<FolderNode>();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return roots;

            if (!recursive)
            {
                var node = BuildFamilyFolderNode(root, false);
                if (node != null) roots.Add(node);
                return roots;
            }

            var first = BuildFamilyFolderNode(root, true);
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

        private static FolderNode BuildFamilyFolderNode(string folderPath, bool recurse)
        {
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
                    var child = BuildFamilyFolderNode(sub, true);
                    if (child != null) children.Add(child);
                }
            }

            if (families.Count == 0 && children.Count == 0) return null;

            var name = System.IO.Path.GetFileName(folderPath);
            return new FolderNode(name, folderPath, families, children);
        }

        private static bool IsInTempFolder(string filePath)
        {
            var parent = System.IO.Path.GetDirectoryName(filePath);
            var name = parent != null ? System.IO.Path.GetFileName(parent) : string.Empty;
            if (string.IsNullOrEmpty(name)) return false;
            return name.StartsWith("~$") || name.Equals("_backup", System.StringComparison.OrdinalIgnoreCase);
        }

        // -------- Views (RVT) --------
        public static List<ViewsFolderNode> BuildRvtTreeRoots(string root, bool recursive)
        {
            var roots = new List<ViewsFolderNode>();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return roots;

            if (!recursive)
            {
                var node = BuildViewsFolderNode(root, false);
                if (node != null) roots.Add(node);
                return roots;
            }

            var first = BuildViewsFolderNode(root, true);
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
        private static bool IsCurrentRevitVersion(string filePath)
        {
            try
            {
                var info = BasicFileInfo.Extract(filePath);
                // True iff file was saved in the same major Revit version as the one running now
                return info != null && info.IsSavedInCurrentVersion;
            }
            catch
            {
                // If we can't read the header, exclude it (safer default)
                return false;
            }
        }
        private static ViewsFolderNode BuildViewsFolderNode(string folderPath, bool recurse)
        {
            // collect RVT files (not templates)
            var models = Directory.EnumerateFiles(folderPath, "*.rvt", SearchOption.TopDirectoryOnly)
                          .Where(p => !IsInTempFolder(p))
                          .Where(IsCurrentRevitVersion)   // ← filter by running Revit version
                          .Select(p => new RvtModelNode(Path.GetFileName(p), p))
                          .ToList();

            var children = new List<ViewsFolderNode>();
            if (recurse)
            {
                foreach (var sub in Directory.EnumerateDirectories(folderPath))
                {
                    var child = BuildViewsFolderNode(sub, true);
                    if (child != null) children.Add(child);
                }
            }

            // prune empty
            if (models.Count == 0 && children.Count == 0) return null;

            var name = System.IO.Path.GetFileName(folderPath);
            return new ViewsFolderNode(name, folderPath, models, children);
        }
    }
}
