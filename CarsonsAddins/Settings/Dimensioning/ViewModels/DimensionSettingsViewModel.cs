using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using CarsonsAddins.Settings.Dimensioning.Models;
using CarsonsAddins.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CarsonsAddins.Settings.Dimensioning.ViewModels
{
    public class DimensionSettingsViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public DimensionTypeSelectorViewModel dimensionTypeSelectorViewModel;
        public GraphicStylesSelectorViewModel graphicStylesSelectorViewModel;
        public FlangeModeSelectorViewModel flangeModeSelectorViewModel;

        private UIDocument uidoc;

        public DimensionType[] dimensionTypes;
        private GraphicsStyle[] allGraphicStyles;
        private Family[] allFlangeFamilySymbols;
        public DimensionSettingsViewModel(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            Refresh();
            dimensionTypeSelectorViewModel = new DimensionTypeSelectorViewModel(ref dimensionTypes, ref DimensionPreferences.Instance);
            graphicStylesSelectorViewModel = new GraphicStylesSelectorViewModel(ref allGraphicStyles, ref DimensionPreferences.Instance.linesStyles);
            flangeModeSelectorViewModel = new FlangeModeSelectorViewModel(ref DimensionPreferences.Instance.flangeModeItems);
        }
        public void Refresh()
        {
            if (uidoc == null) return;
            
            LoadFromRevit();
            DimensionPreferences.Instance = DimensionPreferences.LoadFromPreferences(dimensionTypes, allGraphicStyles, allFlangeFamilySymbols);
        }
        public void LoadFromRevit()
        {
            BuiltInCategory[] pipingCategories = new BuiltInCategory[] { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_PipeCurvesCenterLine, BuiltInCategory.OST_PipeFittingCenterLine, BuiltInCategory.OST_CenterLines, BuiltInCategory.OST_ReferenceLines };
            dimensionTypes = new FilteredElementCollector(uidoc.Document).WhereElementIsElementType().OfClass(typeof(DimensionType)).ToElements().Cast<DimensionType>().Where(dt => DimensionStyleType.Linear.Equals(dt.StyleType)).ToArray();
            allGraphicStyles = new FilteredElementCollector(uidoc.Document).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().Where(gs => pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Id.IntegerValue) || ((gs.GraphicsStyleCategory.Parent != null) && pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Parent.Id.IntegerValue))).ToArray();
            Family[] familes = new FilteredElementCollector(uidoc.Document).OfClass(typeof(Family)).ToElements().Cast<Family>().ToArray();
            allFlangeFamilySymbols = new FilteredElementCollector(uidoc.Document).OfClass(typeof(Family)).Cast<Family>().Where(family => (BuiltInCategory.OST_PipeFitting == (BuiltInCategory)family.FamilyCategory.Id.IntegerValue) && ElementCheckUtils.FlangePartTypes.Contains((PartType)family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE).AsInteger())).ToArray();
        }
        public void Save() => DimensionPreferences.Instance.Save();

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}
