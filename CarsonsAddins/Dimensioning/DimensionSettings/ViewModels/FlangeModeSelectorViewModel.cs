using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using CarsonsAddins.Utils;
using Newtonsoft.Json;
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
    public class FlangeModeItem
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public int elementId = -1;
        public string Name { get; set; } = "";
        public Utils.DimensioningUtils.FlangeDimensionMode Mode { get; set; } = Utils.DimensioningUtils.FlangeDimensionMode.Default;


        [JsonConstructor]
        public FlangeModeItem(int elementId, string name, Utils.DimensioningUtils.FlangeDimensionMode mode)
        {
            this.elementId = elementId;
            Name = name;
            Mode = mode;
        }
        public FlangeModeItem(ElementId elementId, string name, Utils.DimensioningUtils.FlangeDimensionMode mode)
        {
            this.elementId = elementId.IntegerValue;
            Name = name;
            Mode = mode;
        }
    }
}
