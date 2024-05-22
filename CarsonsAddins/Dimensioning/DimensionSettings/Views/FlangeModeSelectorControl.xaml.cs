using Autodesk.Revit.UI;
using CarsonsAddins;
using CarsonsAddins.UICommands;
using CarsonsAddins.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for FlangeModeSelectorControl.xaml
    /// </summary>
    public partial class FlangeModeSelectorControl : UserControl
    {
        private CollectionViewSource collectionViewSource;
        private FlangeModeSelectorViewModel viewModel;
        public FlangeModeSelectorControl()
        {
            InitializeComponent();
            collectionViewSource = FindResource("FlangeItemsCollection") as CollectionViewSource;
            collectionViewSource.GroupDescriptions.Add(new GroupByFlangeModeProperty(DimensioningUtils.FlangeDimensionMode.Exact));
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("Mode", System.ComponentModel.ListSortDirection.Descending));
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
            ModeTextColumn.ItemsSource = Enum.GetValues(typeof(DimensioningUtils.FlangeDimensionMode));
            
        }
        public void Init(FlangeModeSelectorViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            collectionViewSource.Source = viewModel.Items;
            collectionViewSource.GroupDescriptions.Clear();
            collectionViewSource.GroupDescriptions.Add(new GroupByFlangeModeProperty(viewModel.DefaultMode));
        }

        private void SetDefaultMode_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem menuItem)) return;
            if (!(menuItem.Parent is ContextMenu contextMenu)) return;
            if (!(contextMenu.DataContext is CollectionViewGroup viewGroup)) return;
            string groupName = viewGroup.Name.ToString();
            DimensioningUtils.FlangeDimensionMode mode;
            if (Enum.TryParse(groupName, out mode) && viewModel.DefaultMode != mode) 
            {
                viewModel.DefaultMode = mode;    
                collectionViewSource.GroupDescriptions.Clear();
                collectionViewSource.GroupDescriptions.Add(new GroupByFlangeModeProperty(mode));
            }
        }
    }
    class GroupByFlangeModeProperty : PropertyGroupDescription
    {
        private DimensioningUtils.FlangeDimensionMode defaultMode = DimensioningUtils.FlangeDimensionMode.Exact;
        public GroupByFlangeModeProperty(DimensioningUtils.FlangeDimensionMode defaultMode) : base("Mode") 
        {
            this.defaultMode = defaultMode;
        }
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            var baseItem = base.GroupNameFromItem(item, level, culture);
            if (!(baseItem is DimensioningUtils.FlangeDimensionMode mode)) return "Null";
            return GetGroupName(mode);
        }
        public string GetGroupName(DimensioningUtils.FlangeDimensionMode mode)
        {
            DimensioningUtils.FlangeDimensionMode displayMode = (mode == DimensioningUtils.FlangeDimensionMode.Default) ? defaultMode : mode;
            return displayMode.ToString() + ((displayMode == defaultMode) ? " ( default )" : "");
        }
    }
}
