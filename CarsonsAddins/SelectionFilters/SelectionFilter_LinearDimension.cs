using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.SelectionFilters
{
    /// <summary>
    /// A Selection Filter that only allows linear dimensions to be selected.
    /// </summary>
    public class SelectionFilter_LinearDimension : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (!(elem is Dimension dimension)) return false;
            return (DimensionShape.Linear.Equals(dimension.DimensionShape));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
