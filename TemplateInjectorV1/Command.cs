// =============================
// TemplateInjectorV1 — Load Multiple Families from a Server Directory
// Revit 2024 | WPF UI | Manual Transaction
//
// Files in this single snippet:
// 1) Command.cs
// 2) UI/WPF_Interface.xaml
// 3) UI/WPF_Interface.xaml.cs
// 4) Core/FamilyLoader.cs
// 5) Core/MyFamilyLoadOptions.cs
// 6) Models/FamilyListItem.cs
//
// Notes:
// - Drop each part into matching folders/namespaces or keep flat; namespaces are consistent.
// - Requires references: RevitAPI.dll, RevitAPIUI.dll, PresentationFramework, WindowsBase, System.Xaml, System.Windows.Forms
// - The dialog runs modally (ShowDialog). Transactions are created per-family under a TransactionGroup.
// - MyFamilyLoadOptions controls overwrite behavior; exposed via checkboxes in UI.
// - Scans *.rfa in the selected root (optionally recursive). Supports Select All/None, Reload, and Import.
// - Safe for 2024 API. Adjust rootDirectory default as needed.
//
// =====================================
// 1) Command.cs
// =====================================
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace TemplateInjectorV1
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc?.Document;
            if (doc == null)
            {
                message = "No active document.";
                return Result.Failed;
            }

            // TODO: set your default server directory here
            string rootDirectory = @"I:\\Practice Standards\\Interior Templates\\Community Market Sector\\Revit Resources";

            try
            {
                var win = new WPF_Interface(rootDirectory, doc);
                win.Owner = System.Windows.Application.Current?.MainWindow; // Nice-to-have
                win.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("TemplateInjectorV1", $"Unexpected error: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}