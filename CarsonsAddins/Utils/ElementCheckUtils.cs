using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    /// <summary>
    /// Used to determine an element's type and properties.
    /// </summary>
    public static class ElementCheckUtils
    {
        /// <summary>
        /// Gets the Part Type of the MEP Fitting
        /// </summary>
        /// <param name="fitting">Family Instance of a MEP Fitting</param>
        /// <returns>Returns the Part Type of the MEP Fitting. Will return PartType Undefined if the Family Instance is null, the Family Instance Symbol, or Symbol Family is null.</returns>
        public static PartType GetPartType(FamilyInstance fitting)
        {
            if (fitting == null) return PartType.Undefined;
            if (fitting.Symbol == null) return PartType.Undefined;
            if (fitting.Symbol.Family == null) return PartType.Undefined;
            Parameter param = fitting.Symbol.Family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
            if (param == null) return PartType.Undefined;
            return (PartType)param.AsInteger();
        }

        /// <summary>
        /// Checks if the Element provided is a Family Instance with the Pipe Fitting Category and the PartType Flange, Union, or Multiport.
        /// </summary>
        /// <param name="element">Element to be checked.</param>
        /// <returns>Returns true if the element is an MEP Flange or Multiport. Returns false otherwise.</returns>

        public static bool IsPipeFlange(Element element)
        {
            if (element == null) return false;
            if (!(element is FamilyInstance)) return false;
            if (!BuiltInCategory.OST_PipeFitting.Equals(element.Category.BuiltInCategory)) return false;
            return FlangePartTypes.Contains(GetPartType(element as FamilyInstance));
        }

        /// <summary>
        /// Checks if the Element provided is a Family Instance with the PartType of a Pipe Bend or Junction (i.e. Tee, Wye, Cross, etc. )
        /// </summary>
        /// <param name="element">Element to be checked.</param>
        /// <returns>Returns true if the element is a Family Instance with the PartType Elbow, Tee, Wye, Lateral Tee, Cross, or Lateral Cross. Returns false otherwise. </returns>
        public static bool IsPipeBend(Element element)
        {
            if (element == null) return false;
            if (!(element is FamilyInstance)) return false;
            return BendPartTypes.Contains(GetPartType(element as FamilyInstance));
        }

        /// <summary>
        /// Checks if the Element provided is a Pipe Accessory with the corresponding Category.
        /// </summary>
        /// <param name="element">Element to be checked.</param>
        /// <returns>Returns true if the Element is a Pipe Accessory. Returns false otherwise.</returns>
        public static bool IsPipeAccessory(Element element)
        {
            if (element == null) return false;
            if (element.Category == null) return false;
            return (BuiltInCategory.OST_PipeAccessory.Equals(element.Category.BuiltInCategory));
        }

        /// <summary>
        /// Checks if the Element provided is a Pipe with the corresponding Category.
        /// </summary>
        /// <param name="element">Element to be checked.</param>
        /// <returns>Returns true if the Element is a Pipe. Returns false otherwise.</returns>
        public static bool IsPipe(Element element)
        {
            if (element == null) return false;
            if (element.Category == null) return false;
            return (BuiltInCategory.OST_PipeCurves.Equals(element.Category.BuiltInCategory));
        }

        /// <summary>
        /// Part Types that are considered a "Flange" ( Flange, Union, MultiPort ).
        /// </summary>
        public static readonly List<PartType> FlangePartTypes = new List<PartType>() { PartType.PipeFlange, PartType.Union, PartType.MultiPort };

        /// <summary>
        /// Part Types that are considered a "Bend" ( or junction )
        /// </summary>
        public static readonly List<PartType> BendPartTypes = new List<PartType>() { PartType.Elbow, PartType.Cross, PartType.Tee, PartType.Wye, PartType.LateralTee, PartType.LateralCross };
    }
}
