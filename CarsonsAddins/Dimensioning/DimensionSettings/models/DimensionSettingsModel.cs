using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Dimensioning.DimensionSettings.Models
{
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
