
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Settings.Dimensioning.Models
{
    public struct DimensionPreferencesSave
    {
        public string primaryDimensionTypeName;
        public string secondaryPipeDimensionTypeName;
        public string secondaryAccessoryDimensionTypeName;
        public string secondaryFittingDimensionTypeName;
        public string secondaryOtherDimensionTypeName;
        public string[] graphicsStyleNames;
        public FlangeModeItem[] flangeModeItems;
    }
}
