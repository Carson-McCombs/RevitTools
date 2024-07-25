using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Settings.ComponentStates.ViewModels;
using CarsonsAddins.Settings.Dimensioning.ViewModels;
using CarsonsAddins.Settings.Main.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Settings.Main.ViewModels
{
    public class MyApplicationSettingsViewModel
    {
        public DimensionSettingsViewModel dimensionSettingsViewModel;
        public SettingsPluginViewModel pluginsViewModel = new SettingsPluginViewModel();
        public MyApplicationSettingsViewModel(DimensionSettingsViewModel dimensionSettingsViewModel, SettingsPluginViewModel pluginsViewModel)
        {
            this.dimensionSettingsViewModel = dimensionSettingsViewModel;
            this.pluginsViewModel = pluginsViewModel;
        }
        public MyApplicationSettingsViewModel(DimensionSettingsViewModel dimensionSettingsViewModel)
        {
            this.dimensionSettingsViewModel = dimensionSettingsViewModel;
        }
        public void Save()
        {
            dimensionSettingsViewModel.Save();
            pluginsViewModel.Save();
        }
    }
}
