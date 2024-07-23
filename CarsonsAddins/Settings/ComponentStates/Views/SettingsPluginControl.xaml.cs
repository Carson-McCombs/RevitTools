using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Settings.ComponentStates.Models;
using CarsonsAddins.Settings.ComponentStates.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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

namespace CarsonsAddins.Settings.ComponentStates.Views
{
    /// <summary>
    /// Interaction logic for SettingsPluginControl.xaml
    /// </summary>
    public partial class SettingsPluginControl : UserControl
    {
        private SettingsPluginViewModel viewModel;
        public SettingsPluginControl()
        {
            InitializeComponent();
        }

        public void Init(SettingsPluginViewModel viewModel)
        {
            this.viewModel = viewModel;
            SettingsDataGrid.ItemsSource = viewModel.componentStates;
        }

        /// <summary>
        /// When the "Apply" button is pressed, saves the current SettingState to Revit.
        /// </summary>
        private void ApplyButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.Save();
            TaskDialog.Show("Preferences Updated", "Please restart Revit to apply changes to preferences.");
        }

        /// <summary>
        /// When the "Revert" button is pressed, reloads SettingsState to the state which is saved within Revit.
        /// </summary>
        private void RevertButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.LoadFromDB();
        }

    }
}
