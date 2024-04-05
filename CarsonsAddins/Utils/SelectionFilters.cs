using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    public static class SelectionFilters
    {

        /// <summary>
        /// A Selection Filter that only allows a specific PartType
        /// </summary>
        public class SelectionFilter_PipeFittingPartType : ISelectionFilter
        {
            private readonly PartType partType = PartType.Undefined;
            public SelectionFilter_PipeFittingPartType(PartType partType)
            {
                this.partType = partType;
            }
            public bool AllowElement(Element elem)
            {
                if (elem == null) return false;
                if (ElementCheckUtils.IsPipe(elem)) return false;
                FamilyInstance familyInstance = elem as FamilyInstance;
                PartType pt = ElementCheckUtils.GetPartType(familyInstance);
                return (partType.Equals(pt));
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

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
}
