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
    /// A Selection Filter that can be set to allow one, some, or all Piping Elements ( i.e. Pipes, Flanges, Bends/Junctions, and Accessories ).
    /// </summary>
    public class SelectionFilter_PipingElements : ISelectionFilter
    {
        private readonly bool allowPipes = true;
        private readonly bool allowFlanges = true;
        private readonly bool allowBends = true;
        private readonly bool allowOtherFittings = true;

        private readonly bool allowAccessories = true;
        private readonly bool linearOnly = true;
        public SelectionFilter_PipingElements(bool allowPipes, bool allowFlanges, bool allowBends, bool allowOtherFittings, bool allowAccessories)
        {
            this.allowPipes = allowPipes;
            this.allowFlanges = allowFlanges;
            this.allowBends = allowBends;
            this.allowOtherFittings = allowOtherFittings;
            this.allowAccessories = allowAccessories;
        }
        public SelectionFilter_PipingElements(bool allowPipes, bool allowFlanges, bool allowBends, bool allowOtherFittings, bool allowAccessories, bool linearOnly)
        {
            this.allowPipes = allowPipes;
            this.allowFlanges = allowFlanges;
            this.allowBends = allowBends;
            this.allowOtherFittings = allowOtherFittings;
            this.allowAccessories = allowAccessories;
            this.linearOnly = linearOnly;
        }

        public bool AllowElement(Element elem)
        {
            if (elem == null || elem.Category == null) return false;
            if (linearOnly && !GeometryUtils.IsLinearElement(elem)) return false;
            if (allowPipes && ElementCheckUtils.IsPipe(elem)) { return true; }
            if (allowAccessories && ElementCheckUtils.IsPipeAccessory(elem)) return true;
            if (elem.Category.BuiltInCategory.Equals(BuiltInCategory.OST_PipeFitting))
            {
                if (!(elem is FamilyInstance)) return false;
                FamilyInstance familyInstance = elem as FamilyInstance;
                PartType partType = ElementCheckUtils.GetPartType(familyInstance);
                if (ElementCheckUtils.FlangePartTypes.Contains(partType)) return allowFlanges;
                else if (ElementCheckUtils.BendPartTypes.Contains(partType)) return allowBends;
                else return allowOtherFittings;

            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
