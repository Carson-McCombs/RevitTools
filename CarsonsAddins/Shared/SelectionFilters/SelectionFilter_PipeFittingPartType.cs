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
    /// A Selection Filter that only allows a specific PartType
    /// </summary>
    public class SelectionFilter_PipeFittingPartType : ISelectionFilter
    {
        private readonly PartType[] partTypes;
        public SelectionFilter_PipeFittingPartType(params PartType[] partTypes)
        {
            this.partTypes = partTypes;
        }

        public bool AllowElement(Element elem)
        {
            if (elem == null) return false;
            if (ElementCheckUtils.IsPipe(elem)) return false;
            FamilyInstance familyInstance = elem as FamilyInstance;
            PartType pt = ElementCheckUtils.GetPartType(familyInstance);
            return (partTypes.Contains(pt));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
