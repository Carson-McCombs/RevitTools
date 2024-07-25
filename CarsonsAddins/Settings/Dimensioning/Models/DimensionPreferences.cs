using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using CarsonsAddins.Settings.Dimensioning.ViewModels;
using CarsonsAddins.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CarsonsAddins.Settings.Dimensioning.Models
{
    public class DimensionPreferences
    {
        public static DimensionPreferences Instance;
        public bool foundAllDimensionTypes => primaryDimensionType != null && secondaryPipeDimensionType != null && secondaryAccessoryDimensionType != null && secondaryFittingDimensionType != null && secondaryOtherDimensionType != null;
        public DimensionType primaryDimensionType;
        public DimensionType secondaryPipeDimensionType;
        public DimensionType secondaryAccessoryDimensionType;
        public DimensionType secondaryFittingDimensionType;
        public DimensionType secondaryOtherDimensionType;
        public List<GraphicsStyle> linesStyles = new List<GraphicsStyle>();
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
        public DimensionPreferencesSave GetDimensionStyleNames()
        {
            return new DimensionPreferencesSave
            {
                primaryDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
                secondaryPipeDimensionTypeName = secondaryPipeDimensionType?.Name ?? string.Empty,
                secondaryAccessoryDimensionTypeName = secondaryAccessoryDimensionType?.Name ?? string.Empty,
                secondaryFittingDimensionTypeName = secondaryFittingDimensionType?.Name ?? string.Empty,
                secondaryOtherDimensionTypeName = secondaryOtherDimensionType?.Name ?? string.Empty,
                graphicsStyleNames = linesStyles?.Select(style => style.Name).Distinct().ToArray(),
                flangeModeItems = flangeModeItems?.Where(item => item.Mode != FlangeDimensionMode.Default).ToArray()
            };
        }
        public DimensionPreferencesSave GetDimensionStyleNames(string[] graphicStyleNames)
        {
            return new DimensionPreferencesSave
            {
                primaryDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
                secondaryPipeDimensionTypeName = secondaryPipeDimensionType?.Name ?? string.Empty,
                secondaryAccessoryDimensionTypeName = secondaryAccessoryDimensionType?.Name ?? string.Empty,
                secondaryFittingDimensionTypeName = secondaryFittingDimensionType?.Name ?? string.Empty,
                secondaryOtherDimensionTypeName = secondaryOtherDimensionType?.Name ?? string.Empty,
                graphicsStyleNames = graphicStyleNames,
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
        public static DimensionPreferences CreateFromPreferences(Document doc)
        {
            BuiltInCategory[] pipingCategories = new BuiltInCategory[] { 
                BuiltInCategory.OST_PipeCurves, 
                BuiltInCategory.OST_PipeFitting, 
                BuiltInCategory.OST_PipeAccessory, 
                BuiltInCategory.OST_MechanicalEquipment, 
                BuiltInCategory.OST_PipeCurvesCenterLine, 
                BuiltInCategory.OST_PipeFittingCenterLine, 
                BuiltInCategory.OST_CenterLines, 
                BuiltInCategory.OST_ReferenceLines 
            };
            DimensionType[] dimensionTypes = new FilteredElementCollector(doc)
                .WhereElementIsElementType().OfClass(typeof(DimensionType))
                .ToElements().Cast<DimensionType>()
                .Where(dt => DimensionStyleType.Linear.Equals(dt.StyleType))
                .ToArray();
            GraphicsStyle[] allGraphicStyles = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>()
                .Where(gs => pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Id.IntegerValue) || ((gs.GraphicsStyleCategory.Parent != null) && pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Parent.Id.IntegerValue)))
                .ToArray();
            Family[] familes = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .ToElements().Cast<Family>()
                .ToArray();
            Family[] allFlangeFamilySymbols = new FilteredElementCollector(doc)
                .OfClass(typeof(Family)).Cast<Family>()
                .Where(family => (BuiltInCategory.OST_PipeFitting == (BuiltInCategory)family.FamilyCategory.Id.IntegerValue) && ElementCheckUtils.FlangePartTypes.Contains((PartType)family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE).AsInteger()))
                .ToArray();
            return LoadFromPreferences(dimensionTypes, allGraphicStyles, allFlangeFamilySymbols);
        }
        public static DimensionPreferences LoadFromPreferences(DimensionType[] dimensionTypes, GraphicsStyle[] allGraphicStyles, Family[] allFlangeFamilySymbols) //requires that Load Dimension Types has been called first
        {
            
            DimensionPreferences dimensionPreferences = new DimensionPreferences();
            if (dimensionTypes == null || dimensionTypes.Length == 0) return dimensionPreferences;

            try
            {
                if (!string.IsNullOrWhiteSpace(MySettings.Default.DimensionStyles_Preferences))
                {
                    DimensionPreferencesSave dimensionStyleNames = JsonConvert.DeserializeObject<DimensionPreferencesSave>(MySettings.Default.DimensionStyles_Preferences);


                    foreach (DimensionType dimensionType in dimensionTypes)
                    {
                        if (dimensionType.Name == dimensionStyleNames.primaryDimensionTypeName) dimensionPreferences.primaryDimensionType = dimensionType;
                        if (dimensionType.Name == dimensionStyleNames.secondaryPipeDimensionTypeName) dimensionPreferences.secondaryPipeDimensionType = dimensionType;
                        if (dimensionType.Name == dimensionStyleNames.secondaryAccessoryDimensionTypeName) dimensionPreferences.secondaryAccessoryDimensionType = dimensionType;
                        if (dimensionType.Name == dimensionStyleNames.secondaryFittingDimensionTypeName) dimensionPreferences.secondaryFittingDimensionType = dimensionType;
                        if (dimensionType.Name == dimensionStyleNames.secondaryOtherDimensionTypeName) dimensionPreferences.secondaryOtherDimensionType = dimensionType;
                        if (dimensionPreferences.foundAllDimensionTypes) break;
                    }
                    if (dimensionStyleNames.graphicsStyleNames != null)
                    {

                        foreach (GraphicsStyle graphicsStyle in allGraphicStyles)
                        {
                            if (dimensionStyleNames.graphicsStyleNames.Contains(graphicsStyle.Name)) dimensionPreferences.linesStyles.Add(graphicsStyle);
                        }
                    };
                    if (dimensionStyleNames.flangeModeItems != null) dimensionPreferences.flangeModeItems = new List<FlangeModeItem>(dimensionStyleNames.flangeModeItems);
                }


                int[] flangeIds = dimensionPreferences.flangeModeItems.Select(item => item.elementId).ToArray();
                FlangeModeItem[] defaultFlangeModeItems = allFlangeFamilySymbols.Where(family => !flangeIds.Contains(family.Id.IntegerValue)).Select(family => new FlangeModeItem(family.Id.IntegerValue, family.Name, FlangeDimensionMode.Default)).ToArray();
                dimensionPreferences.flangeModeItems.AddRange(defaultFlangeModeItems);
                return dimensionPreferences;

            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error Loading Dimension Styles from DB", ex.Message);
                return new DimensionPreferences();
            }
        }

        public void Save()
        {
            DimensionPreferencesSave dimensionStyleNames = GetDimensionStyleNames();
            try
            {
                MySettings.Default.DimensionStyles_Preferences = JsonConvert.SerializeObject(dimensionStyleNames);
                MySettings.Default.Save();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error Saving Dimension Settings", ex.Message);
            }

        }
    }
}