using Autodesk.Revit.DB;

namespace CarsonsAddins
{
    public interface ISelectionFilter_IsWithinList
    {
        bool AllowElement(Element elem);
        bool AllowReference(Reference reference, XYZ position);
    }
}