using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for DimensionSettingsWindow.xaml
    /// </summary>
    public partial class DimensionSettingsWindow : Window, ISettingsUIComponent
    {
      
        private ObservableCollection<DimensionType> dimensionTypes = new ObservableCollection<DimensionType>();
        
        private DimensionStyles dimensionStyles = new DimensionStyles();

        public DimensionSettingsWindow()
        {
            InitializeComponent();
        }

        public void Init(UIDocument uidoc)
        {
            
        }
        public void LoadDimensionTypes(Document doc)
        {
            if (doc == null) return;
            dimensionTypes.Clear();
            new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).ToElements().Cast<DimensionType>();
        }
        public void TryGetDimensionStyles(Document doc)
        {
            dimensionStyles = new DimensionStyles();
            if (doc == null) return;
            
        }
        private void LoadFromDB(UIDocument uidoc)
        {
            if (MySettings.Default.DimensionStyles_Preferences == null) return;
            //try
            //{
               // dimensionTypes = new ObservableCollection<PipeEndPrepPreferences>(JsonConvert.DeserializeObject<List<PipeEndPrepPreferences>>(MySettings.Default.PEP_Preferences));
            //}
        }
        public void SaveToDB() 
        {
            MySettings.Default.DimensionStyles_Preferences = JsonConvert.SerializeObject(dimensionStyles.GetDimensionStyleNames());s
        }
        public PushButtonData RegisterButton(Assembly assembly)
        {
            throw new NotImplementedException();
        }
    }
    public struct DimensionStyleNames
    {
        public string primaryDimensionTypeName;
        public string secondaryPipeDimensionTypeName;
        public string secondaryAccessoryDimensionTypeName;
        public string secondaryFittingDimensionTypeName;
        public string secondaryOtherDimensionTypeName;
    }
    public struct DimensionStyles
    {
        public DimensionType primaryDimensionType;
        public DimensionType secondaryPipeDimensionType;
        public DimensionType secondaryAccessoryDimensionType;
        public DimensionType secondaryFittingDimensionType;
        public DimensionType secondaryOtherDimensionType;
        public DimensionType GetSecondaryDimensionType(BuiltInCategory builtInCategory)
        {
            switch(builtInCategory)
            {
                case BuiltInCategory.OST_PipeCurves: return secondaryPipeDimensionType;
                case BuiltInCategory.OST_PipeAccessory: return secondaryAccessoryDimensionType;
                case BuiltInCategory.OST_PipeFitting: return secondaryFittingDimensionType;
                default: return secondaryOtherDimensionType;
            }
        }
        public DimensionStyleNames GetDimensionStyleNames()
        {
            return new DimensionStyleNames
            {
                primaryDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
                secondaryPipeDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
                secondaryAccessoryDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
                secondaryFittingDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
                secondaryOtherDimensionTypeName = primaryDimensionType?.Name ?? string.Empty,
            };
        }
        
    }
}
