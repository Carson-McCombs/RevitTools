using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
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
        public static DimensionStyles DimensionStylesSettings;

        
        public DimensionSettingsWindow()
        {
            
            InitializeComponent();
        }

        public void Init(UIDocument uidoc)
        {
            DimensionStylesSettings = new DimensionStyles();
            BuiltInCategory[] pipingCategories = new BuiltInCategory[] {BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_PipeCurvesCenterLine, BuiltInCategory.OST_PipeFittingCenterLine, BuiltInCategory.OST_CenterLines};
            dimensionTypes = new FilteredElementCollector(uidoc.Document).WhereElementIsElementType().OfClass(typeof(DimensionType)).ToElements().Cast<DimensionType>().Where(dt => DimensionStyleType.Linear.Equals(dt.StyleType)).ToArray();
            graphicStyles = new FilteredElementCollector(uidoc.Document).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().Where(gs => pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Id.IntegerValue) || ((gs.GraphicsStyleCategory.Parent != null) && pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Parent.Id.IntegerValue))).ToArray();
            LoadFromDB();
            DimensionTypeSelector.Init(dimensionTypes, ref DimensionStylesSettings);
            GraphicsStyleList.Init(graphicStyles, ref DimensionStylesSettings.centerlineStyles);
        }
        public PushButtonData RegisterButton(Assembly assembly)
        {
            return new PushButtonData("Dimension Pipeline Settings", "Dimension Pipeline Settings", assembly.Location, typeof(GenericCommands.ShowWindow<DimensionSettingsWindow>).FullName)
            {
                AvailabilityClassName = typeof(Setup.CommandAvailability.Availability_ProjectDocumentsOnly).FullName,
                ToolTip = "Settings Window for the Dimension Pipeline Command"
            };
        }
        private void LoadFromDB() //requires that Load Dimension Types has been called first
        {
            DimensionStylesSettings = new DimensionStyles();
            
            if (dimensionTypes == null || dimensionTypes.Length == 0) return;
            if (string.IsNullOrWhiteSpace(MySettings.Default.DimensionStyles_Preferences)) return;

            try
            {
                DimensionStyleNames dimensionStyleNames = JsonConvert.DeserializeObject<DimensionStyleNames>(MySettings.Default.DimensionStyles_Preferences);
                

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

            }
            catch (Exception ex) 
            {
                TaskDialog.Show("Error Loading Dimension Styles from DB", ex.Message);
            }


        }

        
        private void SaveToDB() 
        {
            if (DimensionStylesSettings == null) return;
            DimensionStyleNames dimensionStyleNames = DimensionStylesSettings.GetDimensionStyleNames();
            try
            {
                MySettings.Default.DimensionStyles_Preferences = JsonConvert.SerializeObject(dimensionStyleNames);
                MySettings.Default.Save();
            } catch (Exception ex)
            {
                TaskDialog.Show("Error Saving Dimension Settings", ex.Message);
            }
            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Hide();
            SaveToDB();
            e.Cancel = true;
        }

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
    
    
    

}
