using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
// Alias WinForms to avoid type name clashes with Revit's Application/View
using WinForms = System.Windows.Forms;

namespace TemplateInjectorV1
{
    public partial class WPF_Interface : Window
    {
        private readonly Document _doc;

        // Families tree
        private readonly ObservableCollection<FolderNode> _folders = new ObservableCollection<FolderNode>();

        // Views tree
        private readonly ObservableCollection<ViewsFolderNode> _viewFolders = new ObservableCollection<ViewsFolderNode>();

        public WPF_Interface(string rootDirectory, Document doc)
        {
            InitializeComponent();
            _doc = doc;

            txtRoot.Text = rootDirectory ?? string.Empty;

            // Families
            tvFolders.ItemsSource = _folders;

            // Views
            tvViews.ItemsSource = _viewFolders;
            // lazy-load the views when an RVT node expands
            tvViews.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(ViewsTreeItem_Expanded));

            ReloadAll();
        }

        // ---------------- Common UI ----------------

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new WinForms.FolderBrowserDialog())
            {
                dlg.ShowNewFolderButton = false;
                if (Directory.Exists(txtRoot.Text)) dlg.SelectedPath = txtRoot.Text;

                if (dlg.ShowDialog() == WinForms.DialogResult.OK)
                {
                    txtRoot.Text = dlg.SelectedPath;
                    ReloadAll();
                }
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e) => ReloadAll();

        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void ReloadAll()
        {
            ReloadFamiliesTree();
            ReloadViewsTree();
        }

        // ======================= Families =======================

        private void ReloadFamiliesTree()
        {
            _folders.Clear();
            lblCount.Text = string.Empty;

            string root = txtRoot.Text != null ? txtRoot.Text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;

            bool recursive = true; // default ON (checkbox removed)
            var roots = FamilyLoader.BuildFamilyTreeRoots(root, recursive);
            foreach (var r in roots) _folders.Add(r);

            int total = _folders.Sum(f => f.TotalFamilyCount);
            lblCount.Text = total + " (*.rfa)";
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var folder = fe != null ? fe.Tag as FolderNode : null;
            if (folder != null) folder.SelectAll();
        }

        private void btnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var folder = fe != null ? fe.Tag as FolderNode : null;
            if (folder != null) folder.SelectNone();
        }

        private void btnGlobalSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var root in _folders) root.SelectAll();
        }

        private void btnGlobalSelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var root in _folders) root.SelectNone();
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            var selected = _folders.SelectMany(f => f.GetSelectedFamilies()).ToList();
            if (selected.Count == 0)
            {
                TaskDialog.Show("TemplateInjectorV1", "No families selected.");
                return;
            }

            bool overwriteFamily = chkOverwriteFamily.IsChecked == true;
            bool overwriteParams = chkOverwriteParams.IsChecked == true;
            bool skipWorkshared = true; // default ON (checkbox removed)

            var opts = new MyFamilyLoadOptions(overwriteFamily, overwriteParams);

            int success = 0, skipped = 0, failed = 0;
            var log = new System.Text.StringBuilder();

            using (var tg = new TransactionGroup(_doc, "Load Families (TemplateInjectorV1)"))
            {
                tg.Start();

                foreach (var item in selected)
                {
                    try
                    {
                        if (!File.Exists(item.FullPath))
                        {
                            failed++;
                            log.AppendLine("Missing file: " + item.FullPath);
                            continue;
                        }

                        // Avoid central/copy popups if desired
                        if (skipWorkshared)
                        {
                            try
                            {
                                var info = BasicFileInfo.Extract(item.FullPath);
                                if (info != null && info.IsWorkshared)
                                {
                                    skipped++;
                                    log.AppendLine("Skipped workshared: " + item.FileName);
                                    continue;
                                }
                            }
                            catch { /* best effort only */ }
                        }

                        using (var t = new Transaction(_doc, "Load: " + item.FileName))
                        {
                            t.Start();

                            Family fam;
                            bool loaded = _doc.LoadFamily(item.FullPath, opts, out fam);

                            if (loaded)
                            {
                                success++;
                                log.AppendLine("OK: " + item.FileName);
                                t.Commit();
                            }
                            else
                            {
                                skipped++;
                                log.AppendLine("Skipped (unchanged or not loaded): " + item.FileName);
                                t.RollBack();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        log.AppendLine("FAILED: " + item.FileName + " — " + ex.Message);
                    }
                }

                tg.Assimilate();
            }

            TaskDialog.Show("TemplateInjectorV1",
                "Import complete.\nSuccess: " + success + "\nSkipped: " + skipped + "\nFailed: " + failed + "\n\nDetails:\n" + log);
        }

        // ======================= Views =======================

        private void ReloadViewsTree()
        {
            _viewFolders.Clear();
            lblViewsCount.Text = string.Empty;

            string root = txtRoot.Text != null ? txtRoot.Text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;

            bool recursive = true; // default ON
            var roots = FamilyLoader.BuildRvtTreeRoots(root, recursive);
            foreach (var r in roots) _viewFolders.Add(r);

            int modelCount = _viewFolders.Sum(f => f.TotalModelCount);
            lblViewsCount.Text = modelCount + " (*.rvt)";
        }

        // Lazy-load model views when expanded
        private void ViewsTreeItem_Expanded(object sender, RoutedEventArgs e)
        {
            var tvi = e.OriginalSource as TreeViewItem;
            if (tvi == null) return;

            var model = tvi.DataContext as RvtModelNode;
            if (model != null) EnsureViewsLoaded(model);
        }

        private void EnsureViewsLoaded(RvtModelNode model)
        {
            if (model.IsLoaded) return;

            Document sdoc = null;
            try
            {
                Autodesk.Revit.ApplicationServices.Application app = _doc.Application;
                sdoc = app.OpenDocumentFile(model.FullPath);

                // Drafting views
                var drafting = new FilteredElementCollector(sdoc)
                               .OfClass(typeof(Autodesk.Revit.DB.ViewDrafting))
                               .Cast<Autodesk.Revit.DB.View>()
                               .Where(v => !v.IsTemplate);

                // Legends
                var legends = new FilteredElementCollector(sdoc)
                              .OfClass(typeof(Autodesk.Revit.DB.View))
                              .Cast<Autodesk.Revit.DB.View>()
                              .Where(v => !v.IsTemplate && v.ViewType == Autodesk.Revit.DB.ViewType.Legend);

                // Schedules
                var schedules = new FilteredElementCollector(sdoc)
                                .OfClass(typeof(Autodesk.Revit.DB.ViewSchedule))
                                .Cast<Autodesk.Revit.DB.ViewSchedule>()
                                .Where(v => !v.IsTemplate);

                foreach (var v in drafting)
                {
                    model.Views.Add(new ViewListItem
                    {
                        IsSelected = true,
                        Name = v.Name,
                        Kind = "Drafting",
                        UniqueId = v.UniqueId,
                        SourcePath = model.FullPath
                    });
                }
                foreach (var v in legends)
                {
                    model.Views.Add(new ViewListItem
                    {
                        IsSelected = true,
                        Name = v.Name,
                        Kind = "Legend",
                        UniqueId = v.UniqueId,
                        SourcePath = model.FullPath
                    });
                }
                foreach (var v in schedules)
                {
                    model.Views.Add(new ViewListItem
                    {
                        IsSelected = true,
                        Name = v.Name,
                        Kind = "Schedule",
                        UniqueId = v.UniqueId,
                        SourcePath = model.FullPath
                    });
                }

                model.IsLoaded = true;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("TemplateInjectorV1", "Failed to read views from:\n" + model.FileName + "\n\n" + ex.Message);
            }
            finally
            {
                if (sdoc != null) sdoc.Close(false);
            }
        }

        // Per-folder actions (Views)
        private void btnViewsFolderSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var node = fe != null ? fe.Tag as ViewsFolderNode : null;
            if (node == null) return;

            foreach (var m in node.Models)
            {
                EnsureViewsLoaded(m);
                m.SelectAll();
            }
        }

        private void btnViewsFolderSelectNone_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var node = fe != null ? fe.Tag as ViewsFolderNode : null;
            if (node == null) return;

            foreach (var m in node.Models)
            {
                EnsureViewsLoaded(m);
                m.SelectNone();
            }
        }

        // Per-model actions
        private void btnRvtSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var model = fe != null ? fe.Tag as RvtModelNode : null;
            if (model == null) return;

            EnsureViewsLoaded(model);
            model.SelectAll();
        }

        private void btnRvtSelectNone_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var model = fe != null ? fe.Tag as RvtModelNode : null;
            if (model == null) return;

            EnsureViewsLoaded(model);
            model.SelectNone();
        }

        // Global actions (Views)
      

        private void btnImportViews_Click(object sender, RoutedEventArgs e)
        {
            // Group selected views by their source RVT path
            var selected = _viewFolders
                .SelectMany(f => f.GetSelectedViews())
                .GroupBy(v => v.SourcePath)
                .ToList();

            if (selected.Count == 0)
            {
                TaskDialog.Show("TemplateInjectorV1", "No views selected.");
                return;
            }

            int success = 0, skipped = 0, failed = 0;
            var log = new System.Text.StringBuilder();

            using (var tg = new TransactionGroup(_doc, "Import Views (TemplateInjectorV1)"))
            {
                tg.Start();

                foreach (var group in selected)
                {
                    Document sdoc = null;
                    try
                    {
                        Autodesk.Revit.ApplicationServices.Application app = _doc.Application;
                        sdoc = app.OpenDocumentFile(group.Key);

                        // cache existing names to handle duplicates
                        var existingNames = new HashSet<string>(
                            new FilteredElementCollector(_doc)
                                .OfClass(typeof(Autodesk.Revit.DB.View))
                                .Cast<Autodesk.Revit.DB.View>()
                                .Where(v => !v.IsTemplate)
                                .Select(v => v.Name));

                        foreach (var item in group)
                        {
                            try
                            {
                                var src = sdoc.GetElement(item.UniqueId) as Autodesk.Revit.DB.View;
                                if (src == null)
                                {
                                    skipped++;
                                    log.AppendLine("Missing in source: " + item.Name + " (" + System.IO.Path.GetFileName(group.Key) + ")");
                                    continue;
                                }

                                using (var t = new Transaction(_doc, "Import view: " + item.Name))
                                {
                                    t.Start();

                                    var ids = new List<ElementId> { src.Id };
                                    var pasted = ElementTransformUtils.CopyElements(sdoc, ids, _doc, Transform.Identity, new CopyPasteOptions());

                                    if (pasted != null && pasted.Count > 0)
                                    {
                                        var newView = _doc.GetElement(pasted.First()) as Autodesk.Revit.DB.View;
                                        if (newView != null)
                                        {
                                            string desired = item.Name;
                                            if (existingNames.Contains(desired))
                                            {
                                                var unique = MakeUniqueName(desired, existingNames);
                                                try { newView.Name = unique; } catch { /* read-only names (rare) */ }
                                                existingNames.Add(unique);
                                            }
                                            else
                                            {
                                                existingNames.Add(desired);
                                            }

                                            success++;
                                            t.Commit();
                                        }
                                        else
                                        {
                                            skipped++;
                                            t.RollBack();
                                        }
                                    }
                                    else
                                    {
                                        skipped++;
                                        t.RollBack();
                                    }
                                }
                            }
                            catch (Exception ex2)
                            {
                                failed++;
                                log.AppendLine("FAILED: " + item.Name + " — " + ex2.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed += group.Count();
                        log.AppendLine("FAILED to open model: " + System.IO.Path.GetFileName(group.Key) + " — " + ex.Message);
                    }
                    finally
                    {
                        if (sdoc != null) sdoc.Close(false);
                    }
                }

                tg.Assimilate();
            }

            TaskDialog.Show("TemplateInjectorV1",
                "View import complete.\nSuccess: " + success + "\nSkipped: " + skipped + "\nFailed: " + failed + "\n\nDetails:\n" + log);
        }

        private static string MakeUniqueName(string baseName, HashSet<string> existing)
        {
            string candidate = baseName + " (2)";
            int i = 3;
            while (existing.Contains(candidate))
            {
                candidate = baseName + " (" + i + ")";
                i++;
            }
            return candidate;
        }
    }
}
