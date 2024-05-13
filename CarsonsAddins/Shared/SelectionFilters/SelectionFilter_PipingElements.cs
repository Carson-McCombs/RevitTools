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
        private readonly bool invert = false;
        private readonly bool allowPipes = true;
        private readonly bool allowFlanges = true;
        private readonly bool allowElbows = true;
        private readonly bool allowJunctions = true;
        private readonly bool allowOtherFittings = true;

        private readonly bool allowAccessories = true;
        private readonly bool linearOnly = true;
        public SelectionFilter_PipingElements(bool invert)
        {
            this.invert = invert;
        }
        public SelectionFilter_PipingElements(bool invert, bool allowPipes, bool allowFlanges, bool allowElbows, bool allowJunctions, bool allowOtherFittings, bool allowAccessories)
        {
            this.invert = invert;
            this.allowPipes = allowPipes;
            this.allowFlanges = allowFlanges;
            this.allowElbows = allowElbows;
            this.allowJunctions = allowJunctions;
            this.allowOtherFittings = allowOtherFittings;
            this.allowAccessories = allowAccessories;
        }
        public SelectionFilter_PipingElements(bool invert, bool allowPipes, bool allowFlanges, bool allowElbows, bool allowJunctions, bool allowOtherFittings, bool allowAccessories, bool linearOnly)
        {
            this.invert = invert;
            this.allowPipes = allowPipes;
            this.allowFlanges = allowFlanges;
            this.allowElbows = allowElbows;
            this.allowJunctions = allowJunctions;
            this.allowOtherFittings = allowOtherFittings;
            this.allowAccessories = allowAccessories;
            this.linearOnly = linearOnly;
        }

        public bool AllowElement(Element elem)
        {
            if (elem == null || elem.Category == null) return invert;
            if (linearOnly && !ConnectionUtils.HasParallelConnectors(elem)) return invert;
            if (allowPipes && ElementCheckUtils.IsPipe(elem)) { return !invert; }
            if (allowAccessories && ElementCheckUtils.IsPipeAccessory(elem)) return !invert;
            if (BuiltInCategory.OST_PipeFitting.Equals((BuiltInCategory)(elem.Category.Id.IntegerValue)))
            {
                if (!(elem is FamilyInstance)) return invert;
                FamilyInstance familyInstance = elem as FamilyInstance;
                PartType partType = ElementCheckUtils.GetPartType(familyInstance);
                if (ElementCheckUtils.FlangePartTypes.Contains(partType)) return allowFlanges ^ invert;
                else if (PartType.Elbow.Equals(partType)) return allowElbows ^ invert;
                else if (ElementCheckUtils.JunctionPartTypes.Contains(partType)) return allowJunctions  ^ invert;
                else return allowOtherFittings;

            }
            return invert;
        }
        public bool AllowElement(Element elem, Connector connector)
        {
            if (elem == null || elem.Category == null) return invert;
            if (linearOnly && ConnectionUtils.GetParallelConnector(connector) == null) return invert;
            if (allowPipes && ElementCheckUtils.IsPipe(elem)) { return !invert; }
            if (allowAccessories && ElementCheckUtils.IsPipeAccessory(elem)) return !invert;
            if (BuiltInCategory.OST_PipeFitting.Equals((BuiltInCategory)(elem.Category.Id.IntegerValue)))
            {
                if (!(elem is FamilyInstance)) return invert;
                FamilyInstance familyInstance = elem as FamilyInstance;
                PartType partType = ElementCheckUtils.GetPartType(familyInstance);
                if (ElementCheckUtils.FlangePartTypes.Contains(partType)) return allowFlanges ^ invert;
                else if (PartType.Elbow.Equals(partType)) return allowElbows ^ invert;
                else if (ElementCheckUtils.JunctionPartTypes.Contains(partType)) return allowJunctions ^ invert;
                else return allowOtherFittings;

            }
            return invert;
        }
        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}
