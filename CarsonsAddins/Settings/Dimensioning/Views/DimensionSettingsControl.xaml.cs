using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for DimensionSettingsControl.xaml
    /// </summary>
    public partial class DimensionSettingsControl : UserControl
    {
        private ViewModels.DimensionSettingsViewModel viewModel;

        public DimensionSettingsControl()
        {
            InitializeComponent();
        }
        public void Init(ViewModels.DimensionSettingsViewModel viewModel)
        {
            this.viewModel = viewModel;
            if (viewModel == null) return;
            DimensionTypeSelector.Init(viewModel.dimensionTypeSelectorViewModel);
            GraphicsStyleList.Init(viewModel.graphicStylesSelectorViewModel);
            FlangeModeSelector.Init(viewModel.flangeModeSelectorViewModel);
        }
    }
}
