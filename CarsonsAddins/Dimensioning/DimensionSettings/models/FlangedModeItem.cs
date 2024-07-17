using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace CarsonsAddins.Dimensioning.DimensionSettings.Models
{
    
    public class FlangeModeItem
    {
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
    }
}
