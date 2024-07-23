using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace CarsonsAddins.Settings.Dimensioning.Models
{
    public enum FlangeDimensionMode { None, Default, Exact, Partial, Negate }
    public class FlangeModeItem
    {
        public int elementId = -1;
        public string Name { get; set; } = "";
        public FlangeDimensionMode Mode { get; set; } = FlangeDimensionMode.Default;


        [JsonConstructor]
        public FlangeModeItem(int elementId, string name, FlangeDimensionMode mode)
        {
            this.elementId = elementId;
            Name = name;
            Mode = mode;
        }
    }
}
