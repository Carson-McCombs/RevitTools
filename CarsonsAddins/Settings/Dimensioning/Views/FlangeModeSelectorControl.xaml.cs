using Autodesk.Revit.UI;
using CarsonsAddins;
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

namespace CarsonsAddins.Settings.Dimensioning.Views
{
    /// <summary>
    /// Interaction logic for FlangeModeSelectorControl.xaml
    /// </summary>
    public partial class FlangeModeSelectorControl : UserControl
    {
        private CollectionViewSource collectionViewSource;
        private ViewModels.FlangeModeSelectorViewModel viewModel;
        public FlangeModeSelectorControl()
        {
            InitializeComponent();
            collectionViewSource = FindResource("FlangeItemsCollection") as CollectionViewSource;
            collectionViewSource.GroupDescriptions.Add(new GroupByFlangeModeProperty(Models.FlangeDimensionMode.Exact));
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("Mode", System.ComponentModel.ListSortDirection.Descending));
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));
            ModeTextColumn.ItemsSource = Enum.GetValues(typeof(Models.FlangeDimensionMode));
            
        }
        public void Init(ViewModels.FlangeModeSelectorViewModel viewModel)
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
            Models.FlangeDimensionMode mode;
            if (Enum.TryParse(groupName, out mode) && viewModel.DefaultMode != mode) 
            {
                viewModel.DefaultMode = mode;    
                collectionViewSource.GroupDescriptions.Clear();
                collectionViewSource.GroupDescriptions.Add(new GroupByFlangeModeProperty(mode));
            }
        }
        class GroupByFlangeModeProperty : PropertyGroupDescription
        {
            private Models.FlangeDimensionMode defaultMode = Models.FlangeDimensionMode.Exact;
            public GroupByFlangeModeProperty(Models.FlangeDimensionMode defaultMode) : base("Mode")
            {
                this.defaultMode = defaultMode;
            }
            public override object GroupNameFromItem(object item, int level, CultureInfo culture)
            {
                var baseItem = base.GroupNameFromItem(item, level, culture);
                if (!(baseItem is Models.FlangeDimensionMode mode)) return "Null";
                return GetGroupName(mode);
            }
            public string GetGroupName(Models.FlangeDimensionMode mode)
            {
                if (mode != Models.FlangeDimensionMode.Default) return mode.ToString();
                return "Default ( " + defaultMode.ToString() + " )";
            }
        }
    }
    
}
