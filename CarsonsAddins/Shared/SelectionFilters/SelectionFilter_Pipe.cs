using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using CarsonsAddins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.SelectionFilters
{
    /// <summary>
    /// A Selection Filter that only allows Pipe Elements
    /// </summary>
    public class SelectionFilter_Pipe : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return ElementCheckUtils.IsPipe(elem);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
