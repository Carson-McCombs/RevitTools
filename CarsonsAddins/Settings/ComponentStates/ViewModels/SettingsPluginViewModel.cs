using CarsonsAddins.Settings.ComponentStates.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Settings.ComponentStates.ViewModels
{
    public class SettingsPluginViewModel
    {
        public ObservableCollection<ComponentState> componentStates => ComponentStatePreferences.Instance.settingsState;
        public void Save() => ComponentStatePreferences.Instance.Save();
        public void LoadFromDB() => ComponentStatePreferences.Instance.LoadFromDB(Assembly.GetExecutingAssembly());
    }
}
