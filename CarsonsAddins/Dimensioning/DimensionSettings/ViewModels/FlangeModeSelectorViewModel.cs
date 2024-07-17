using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Dimensioning.DimensionSettings.Models;
using CarsonsAddins.Properties;
using CarsonsAddins.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    public class FlangeModeSelectorViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private DimensioningUtils.FlangeDimensionMode defaultMode = DimensioningUtils.FlangeDimensionMode.Exact;
        public DimensioningUtils.FlangeDimensionMode DefaultMode
        {
            get => defaultMode;
            set
            {
                if (value == DimensioningUtils.FlangeDimensionMode.Default || value == defaultMode) return;
                defaultMode = value;
                OnNotifyPropertyChanged();
            }
        }
        private List<FlangeModeItem> items = new List<FlangeModeItem>();
        public List<FlangeModeItem> Items
        {
            get => items;
            set
            {
                if (value == null || items == value) return;
                items = value;
                OnNotifyPropertyChanged();
            }
        }
        
        public FlangeModeSelectorViewModel( ref List<FlangeModeItem> items)
        {
            Items = items;
        }
        

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
    
}
