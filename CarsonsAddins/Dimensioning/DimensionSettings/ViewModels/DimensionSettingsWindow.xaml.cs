using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins;
using CarsonsAddins.Properties;
using CarsonsAddins.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static CarsonsAddins.Utils.DimensioningUtils;

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for DimensionSettingsWindow.xaml
    /// </summary>
    public partial class DimensionSettingsWindow : Window, ISettingsUIComponent, INotifyPropertyChanged
    {
        public const string FolderName = "Dimensioning";
        public const bool IsWIP = false;
        public event PropertyChangedEventHandler PropertyChanged;
        private DimensionType[] dimensionTypes;
        private GraphicsStyle[] graphicStyles;
        private Family[] allFlangeFamilySymbols;
        public static DimensionStyles DimensionStylesSettings;
        private UIDocument uidoc;
        public DimensionSettingsWindow()
        {
            
            InitializeComponent();
            IsVisibleChanged += DimensionSettingsWindow_IsVisibleChanged;
        }
        public PushButtonData RegisterButton(Assembly assembly)
        {
            return new PushButtonData("Dimension Pipeline Settings", "Dimension Pipeline Settings", assembly.Location, typeof(GenericCommands.ShowWindow<DimensionSettingsWindow>).FullName)
            {
                AvailabilityClassName = typeof(Setup.Availablity.Availability_ProjectDocumentsOnly).FullName,
                ToolTip = "Settings Window for the Dimension Pipeline Command"
            };
        }

        private void DimensionSettingsWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == System.Windows.Visibility.Hidden) SaveToDB();
            //else if (Visibility == System.Windows.Visibility.Visible) Refresh();
        }

        public void Init(UIDocument uidoc)
        { 
            this.uidoc = uidoc;
            Refresh();
        }
        private void Refresh()
        {
            if (uidoc == null) return;
            DimensionStylesSettings = new DimensionStyles();
            LoadFromRevit(); 
            LoadFromPreferences();
            InitControls();
        }
        public void LoadFromRevit()
        {
            BuiltInCategory[] pipingCategories = new BuiltInCategory[] { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_PipeCurvesCenterLine, BuiltInCategory.OST_PipeFittingCenterLine, BuiltInCategory.OST_CenterLines, BuiltInCategory.OST_ReferenceLines };
            dimensionTypes = new FilteredElementCollector(uidoc.Document).WhereElementIsElementType().OfClass(typeof(DimensionType)).ToElements().Cast<DimensionType>().Where(dt => DimensionStyleType.Linear.Equals(dt.StyleType)).ToArray();
            graphicStyles = new FilteredElementCollector(uidoc.Document).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().Where(gs => pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Id.IntegerValue) || ((gs.GraphicsStyleCategory.Parent != null) && pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Parent.Id.IntegerValue))).ToArray();
            Family[] familes = new FilteredElementCollector(uidoc.Document).OfClass(typeof(Family)).ToElements().Cast<Family>().ToArray();
            allFlangeFamilySymbols = new FilteredElementCollector(uidoc.Document).OfClass(typeof(Family)).Cast<Family>().Where(family => (BuiltInCategory.OST_PipeFitting == (BuiltInCategory) family.FamilyCategory.Id.IntegerValue) && ElementCheckUtils.FlangePartTypes.Contains((PartType)family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE).AsInteger())).ToArray();
        }


            private void LoadFromPreferences() //requires that Load Dimension Types has been called first
        {
            DimensionStylesSettings = new DimensionStyles();
            
            if (dimensionTypes == null || dimensionTypes.Length == 0) return;
            
            try
            {
                if (!string.IsNullOrWhiteSpace(MySettings.Default.DimensionStyles_Preferences))
                {
                    DimensionSettingsModel dimensionStyleNames = JsonConvert.DeserializeObject<DimensionSettingsModel>(MySettings.Default.DimensionStyles_Preferences);


                    foreach (DimensionType dimensionType in dimensionTypes)
                    {
                        if (dimensionType.Name == dimensionStyleNames.primaryDimensionTypeName) DimensionStylesSettings.primaryDimensionType = dimensionType;
                        if (dimensionType.Name == dimensionStyleNames.secondaryPipeDimensionTypeName) DimensionStylesSettings.secondaryPipeDimensionType = dimensionType;
                        if (dimensionType.Name == dimensionStyleNames.secondaryAccessoryDimensionTypeName) DimensionStylesSettings.secondaryAccessoryDimensionType = dimensionType;
                        if (dimensionType.Name == dimensionStyleNames.secondaryFittingDimensionTypeName) DimensionStylesSettings.secondaryFittingDimensionType = dimensionType;
                        if (dimensionType.Name == dimensionStyleNames.secondaryOtherDimensionTypeName) DimensionStylesSettings.secondaryOtherDimensionType = dimensionType;
                        if (DimensionStylesSettings.foundAllDimensionTypes) break;
                    }
                    if (dimensionStyleNames.centerlineStyleNames != null)
                    {

                        foreach (GraphicsStyle graphicsStyle in graphicStyles)
                        {
                            if (dimensionStyleNames.centerlineStyleNames.Contains(graphicsStyle.Name)) DimensionStylesSettings.centerlineStyles.Add(graphicsStyle);
                        }
                    };
                    if (dimensionStyleNames.flangeModeItems != null) DimensionStylesSettings.flangeModeItems = new List<FlangeModeItem>(dimensionStyleNames.flangeModeItems);
                }

                
                int[] flangeIds = DimensionStylesSettings.flangeModeItems.Select(item => item.elementId).ToArray();
                FlangeModeItem[] defaultFlangeModeItems = allFlangeFamilySymbols.Where(family => !flangeIds.Contains(family.Id.IntegerValue)).Select(family => new FlangeModeItem(family.Id, family.Name, FlangeDimensionMode.Default)).ToArray();
                DimensionStylesSettings.flangeModeItems.AddRange(defaultFlangeModeItems);


            }
            catch (Exception ex) 
            {
                TaskDialog.Show("Error Loading Dimension Styles from DB", ex.Message);
            }


        }
        private void InitControls()
        {
            DimensionTypeSelector.Init(dimensionTypes, ref DimensionStylesSettings);
            GraphicsStyleList.Init(graphicStyles, ref DimensionStylesSettings.centerlineStyles);
            FlangeModeSelector.Init(new FlangeModeSelectorViewModel(ref DimensionStylesSettings.flangeModeItems));
            //DimensionPreviewControl.AddPreviewControlWithCustomView(uidoc.Document);
        }
        private void SaveToDB() 
        {
            if (DimensionStylesSettings == null) return;
            DimensionSettingsModel dimensionStyleNames = DimensionStylesSettings.GetDimensionStyleNames(GraphicsStyleList.SelectedGraphicStyleNames.ToArray());
            try
            {
                MySettings.Default.DimensionStyles_Preferences = JsonConvert.SerializeObject(dimensionStyleNames);
                MySettings.Default.Save();
            } catch (Exception ex)
            {
                TaskDialog.Show("Error Saving Dimension Settings", ex.Message);
            }
            
        }
        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }


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

    public struct DimensionSettingsModel
    {
        public string primaryDimensionTypeName;
        public string secondaryPipeDimensionTypeName;
        public string secondaryAccessoryDimensionTypeName;
        public string secondaryFittingDimensionTypeName;
        public string secondaryOtherDimensionTypeName;
        public string[] centerlineStyleNames;
        public FlangeModeItem[] flangeModeItems;
    }

}
