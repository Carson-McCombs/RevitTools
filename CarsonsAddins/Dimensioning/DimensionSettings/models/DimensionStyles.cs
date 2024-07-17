using CarsonsAddins.Utils;
using CarsonsAddins;
using static CarsonsAddins.Utils.DimensioningUtils;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using Autodesk.Revit.DB;

namespace CarsonsAddins.Dimensioning.DimensionSettings.Models
{
    public class DimensionStyles
    {
        public bool foundAllDimensionTypes => primaryDimensionType != null && secondaryPipeDimensionType != null && secondaryAccessoryDimensionType != null && secondaryFittingDimensionType != null && secondaryOtherDimensionType != null;
        public DimensionType primaryDimensionType;
        public DimensionType secondaryPipeDimensionType;
        public DimensionType secondaryAccessoryDimensionType;
        public DimensionType secondaryFittingDimensionType;
        public DimensionType secondaryOtherDimensionType;
        public List<GraphicsStyle> centerlineStyles = new List<GraphicsStyle>();
        public List<FlangeModeItem> flangeModeItems = new List<FlangeModeItem>();
        private Lookup<int, FlangeDimensionMode> flangeModeLookup => flangeModeItems.ToLookup(item => item.elementId, item => item.Mode) as Lookup<int, FlangeDimensionMode>;
        public FlangeDimensionMode defaultFlangeMode = FlangeDimensionMode.Exact;
        public DimensionType GetSecondaryDimensionType(BuiltInCategory builtInCategory)
        {
            switch (builtInCategory)
            {
                case BuiltInCategory.OST_PipeCurves: return secondaryPipeDimensionType;
                case BuiltInCategory.OST_PipeFitting: return secondaryFittingDimensionType;
                case BuiltInCategory.OST_PipeAccessory: return secondaryAccessoryDimensionType;
                default: return secondaryOtherDimensionType;
            }
        }
        public DimensionSettingsModel GetDimensionStyleNames()
        {
            return new DimensionSettingsModel
            {
                primaryDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
                secondaryPipeDimensionTypeName = secondaryPipeDimensionType?.Name ?? string.Empty,
                secondaryAccessoryDimensionTypeName = secondaryAccessoryDimensionType?.Name ?? string.Empty,
                secondaryFittingDimensionTypeName = secondaryFittingDimensionType?.Name ?? string.Empty,
                secondaryOtherDimensionTypeName = secondaryOtherDimensionType?.Name ?? string.Empty,
                centerlineStyleNames = centerlineStyles?.Select(style => style.Name).Distinct().ToArray(),
                flangeModeItems = flangeModeItems?.Where(item => item.Mode != FlangeDimensionMode.Default).ToArray()
            };
        }
        public DimensionSettingsModel GetDimensionStyleNames(string[] graphicStyleNames)
        {
            return new DimensionSettingsModel
            {
                primaryDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
                secondaryPipeDimensionTypeName = secondaryPipeDimensionType?.Name ?? string.Empty,
                secondaryAccessoryDimensionTypeName = secondaryAccessoryDimensionType?.Name ?? string.Empty,
                secondaryFittingDimensionTypeName = secondaryFittingDimensionType?.Name ?? string.Empty,
                secondaryOtherDimensionTypeName = secondaryOtherDimensionType?.Name ?? string.Empty,
                centerlineStyleNames = graphicStyleNames,
                flangeModeItems = flangeModeItems?.Where(item => item.Mode != FlangeDimensionMode.Default).ToArray()
            };
        }
        public FlangeDimensionMode GetMode(Element element)
        {
            if (!(element is FamilyInstance familyInstance) || !ElementCheckUtils.IsPipeFlange(familyInstance)) return FlangeDimensionMode.None;
            int familyId = familyInstance?.Symbol?.Family?.Id.IntegerValue ?? -1;
            if (familyId == -1) return FlangeDimensionMode.None;
            return flangeModeLookup[familyId].FirstOrDefault();
        }
    }
}