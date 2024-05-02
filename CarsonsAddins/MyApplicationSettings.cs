using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    /// <summary>
    /// Stores the Settings state, either by loading it in from the user's preferences or setting the state to their default values. Manipulates the SettingsState of the Addin.
    /// </summary>
    public class MyApplicationSettings
    {
        public static MyApplicationSettings Instance;
        public ObservableCollection<ComponentState> settingsState = new ObservableCollection<ComponentState>();

        /// <summary>
        /// For each ISettingsComponent in the assembly, attempts to retrieve a ComponentState from preferences. If this is null, create a new ComponentState.
        /// </summary>
        /// <param name="assembly">The assembly in which the addin is located.</param>
        /// <returns></returns>
        public List<ComponentState> InitComponentStates(Assembly assembly)
        {
            if (!string.IsNullOrEmpty(MySettings.Default.ComponentState_Preferences)) LoadFromDB(assembly);
            else SetToDefault(assembly);
            return settingsState.ToList();
        }

        private Dictionary<string, bool> GetNameIsEnabledDictionary() 
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            foreach (ComponentState state in settingsState)
            {
                dictionary.Add(state.ComponentName, state.IsEnabled);
            }
            
            return dictionary;
        }

        /// <summary>
        /// Saves the current settingsState as a JSON within the user preferences in Revit.
        /// </summary>
        public void SaveToDB()
        {
            if (settingsState == null || settingsState.Count == 0) return;
            MySettings.Default.ComponentState_Preferences = JsonConvert.SerializeObject(GetNameIsEnabledDictionary());
            MySettings.Default.Save();
        }

        /// <summary>
        /// Using the assembly, retrieves all classes that extend the ISettingsComponent interface.
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        private List<Type> FindComponentsInNamespace(Assembly assembly)
        {
            List<Type> componentTypes = new List<Type>();
            Type componentBaseType = typeof(ISettingsComponent);
            string namespaceString = typeof(CarsonsAddinsApplication).Namespace;
            foreach (Type type in assembly.GetTypes())
            {
                if (type.Namespace != namespaceString) continue;
                if (type.IsInterface) continue;
                if (type.GetInterfaces().Contains(componentBaseType)) componentTypes.Add(type);
            }

            return componentTypes;
        }

        /// <summary>
        /// Uses reflection to determine which of the components found within the namespace should be enabled and loaded in by default ( determined by the field: "public const bool IsWIP" ).  
        /// Unfortunately, this causes all ISettingsComponents to require the ISWIP field without it being stated in the inherited class.  
        /// The IsWIP Field should be const such that the value is loaded and can be evaluated at compile time, this is also because the field never changes between sessions of Revit(unless the user changes addin versions).
        /// </summary>
        /// <param name="assembly">The current Addin Assembly</param>
        private void SetToDefault(Assembly assembly)
        {
            settingsState.Clear();

            List<Type> types = FindComponentsInNamespace(assembly);
            string log = "";
            foreach (Type type in types)
            {

                try
                {
                    bool? isWIP = type.GetField("IsWIP", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetRawConstantValue() as bool?;
                    bool isEnabled = isWIP == null || !(bool)isWIP;
                    settingsState.Add(new ComponentState(type, isEnabled, !isEnabled));
                }
                catch
                {
                    log = log + type.FullName + '\n';
                }

            }
            if (!string.IsNullOrEmpty(log)) TaskDialog.Show("Types Missing IsWIP Check", log);
        }

        /// <summary>
        /// Attempts to load the ComponentState preferences ( which store what commands are enabled and disabled at application start ), and if it cannot, it then sets the settings to default ( based on the WIP or Work-in-Progress const in each class ).
        /// </summary>
        /// <param name="assembly">The current Addin Assembly</param>
        public void LoadFromDB(Assembly assembly)
        {
            try
            {
                settingsState.Clear();
                if (!string.IsNullOrWhiteSpace(MySettings.Default.ComponentState_Preferences))
                {
                    Dictionary<string,bool> stateDictionary = JsonConvert.DeserializeObject<Dictionary<string, bool>>(MySettings.Default.ComponentState_Preferences);
                    SetToDefault(assembly);
                    foreach (ComponentState state in settingsState)
                    {
                        if (stateDictionary.ContainsKey(state.ComponentName)) state.IsEnabled = stateDictionary[state.ComponentName]; 
                    }
                }
                else
                {
                    SetToDefault(assembly);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("ERROR LOADING FROM DB", ex.Message);
                SetToDefault(assembly);
            }



        }
    }
}
