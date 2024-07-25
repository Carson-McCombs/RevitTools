using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CarsonsAddins.Standalone.ParameterManager.Models
{
    class GroupParameterValueProperty : PropertyGroupDescription
    {
        public GroupParameterValueProperty(string propertyName) : base(propertyName)
        {
            PropertyName = propertyName;
        }

        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var baseItem = base.GroupNameFromItem(item, level, culture);
            ParameterCell parameterCell = baseItem as ParameterCell;
            return GetGroupName(parameterCell);
        }

        public static string GetGroupName(ParameterCell parameterCell)
        {
            if (parameterCell.IsNull) return "No Parameter";
            else if (string.IsNullOrEmpty(parameterCell.ParameterValue)) return "Empty";
            return parameterCell.ParameterValue;
        }
        public static string GetGroupName(string parameterName)
        {
            return (string.IsNullOrEmpty(parameterName)) ? "Empty" : parameterName;
        }

    }
}
