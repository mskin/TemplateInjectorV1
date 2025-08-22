using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms; // FolderBrowserDialog

namespace TemplateInjectorV1
{
    public partial class WPF_Interface : Window
    {
        private readonly Document _doc;
        private readonly ObservableCollection<FolderNode> _folders = new ObservableCollection<FolderNode>();

        public WPF_Interface(string rootDirectory, Document doc)
        {
            InitializeComponent();
            _doc = doc;
            txtRoot.Text = rootDirectory ?? string.Empty;
            tvFolders.ItemsSource = _folders;
            ReloadTree();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.ShowNewFolderButton = false;
                if (Directory.Exists(txtRoot.Text)) dlg.SelectedPath = txtRoot.Text;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtRoot.Text = dlg.SelectedPath;
                    ReloadTree();
                }
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e) { ReloadTree(); }
        private void btnClose_Click(object sender, RoutedEventArgs e) { Close(); }

        private void ReloadTree()
        {
            _folders.Clear();
            lblCount.Text = string.Empty;

            string root = txtRoot.Text != null ? txtRoot.Text.Trim() : string.Empty;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;

            bool recursive = true;
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
            bool skipWorkshared = true;
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
                            catch { /* ignore and attempt load */ }
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
    }
}
