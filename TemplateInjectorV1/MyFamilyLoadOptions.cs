
// =====================================
// 5) Core/MyFamilyLoadOptions.cs
// =====================================
using Autodesk.Revit.DB;

namespace TemplateInjectorV1
{
    /// <summary>
    /// Controls how Revit handles families that already exist in the project and whether
    /// parameter values are overwritten when matching types are found.
    /// </summary>
    public class MyFamilyLoadOptions : IFamilyLoadOptions
    {
        private readonly bool _overwriteFamily;
        private readonly bool _overwriteParams;

        public MyFamilyLoadOptions(bool overwriteFamily, bool overwriteParameterValues)
        {
            _overwriteFamily = overwriteFamily;
            _overwriteParams = overwriteParameterValues;
        }

        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = _overwriteParams;
            // If family exists, return true to reload, false to skip
            return _overwriteFamily;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            // Prefer the family from the loaded file or the existing project copy depending on _overwriteFamily
            source = _overwriteFamily ? FamilySource.Family : FamilySource.Project;
            overwriteParameterValues = _overwriteParams;
            return true; // proceed
        }
    }
}
