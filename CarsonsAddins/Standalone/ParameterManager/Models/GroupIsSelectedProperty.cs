using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CarsonsAddins.Standalone.ParameterManager.Models
{
    class GroupIsSelectedProperty : PropertyGroupDescription
    {
        public GroupIsSelectedProperty() : base("IsSelected") { }
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var baseItem = base.GroupNameFromItem(item, level, culture);
            if (!(baseItem is bool isSelected)) return "NULL";
            return GetGroupName(isSelected);
        }
        public static string GetGroupName(bool isSelected)
        {
            return isSelected ? "Selected" : "Not Selected";
        }
    }
}
