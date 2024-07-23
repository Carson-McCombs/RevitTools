using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using CarsonsAddins.Settings.Dimensioning.Models;
using CarsonsAddins.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Settings.Dimensioning.ViewModels
{
    public class FlangeModeSelectorViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private FlangeDimensionMode defaultMode = FlangeDimensionMode.Exact;
        public FlangeDimensionMode DefaultMode
        {
            get => defaultMode;
            set
            {
                if (value == FlangeDimensionMode.Default || value == defaultMode) return;
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
        
        public FlangeModeSelectorViewModel(ref List<FlangeModeItem> items)
        {
            Items = items;
        }
        

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
    
}
