using CarsonsAddins.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Reflection;
using Autodesk.Revit.UI;
using System.Configuration.Assemblies;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using System.CodeDom;
using System.Threading;
using Autodesk.Revit.DB.ExtensibleStorage;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace CarsonsAddins
{

    /// <summary>
    /// Controls the loading, setting, and saving of ComponentState information. There is one ComponentState per Command and each State can be enabled and disabled within the Window UI.
    /// Note: This class and command are always registered and active within the application. Also, I might should move the ISettingsComponent hierarchy to their own class to save space and increase readability.
    /// </summary>
    public partial class MyApplicationSettingsWindow : Window
    {
        public MyApplicationSettingsWindow()
        {
            InitializeComponent();
        }
        
        public static PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("Carsons Addins Settings", "Carson's Addins Settings", assembly.Location, typeof(ShowSettingsWindow).FullName);
            pushButtonData.ToolTip = "Opens Carson's Addins Settings Window";
            return pushButtonData;
        }
        
        private void ApplyButtonClick(object sender, RoutedEventArgs e)
        {
            MyApplicationSettings.Instance.SaveToDB();
        }
        private void RevertButtonClick(object sender, RoutedEventArgs e)
        {
            MyApplicationSettings.Instance.LoadFromDB(Assembly.GetExecutingAssembly());

        }

        /// <summary>
        /// Shows the SettingsWindow. The ShowSettingsWindow class is needed so that the MyApplicationSettingsWindow doesn't need extend the ISettingsComponent to prevent feedback loops on register
        /// </summary>
        [Transaction(TransactionMode.Manual)]
        public class ShowSettingsWindow : IExternalCommand
        {
            private MyApplicationSettingsWindow instance;

            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                try
                {
                    if (instance == null) instance = new MyApplicationSettingsWindow();
                    instance.SettingsDataGrid.ItemsSource = MyApplicationSettings.Instance.settingsState;
                    instance.Show();

                    return Result.Succeeded;

                }
                catch (Exception e)
                {
                    TaskDialog.Show("Error Showing Settings Window", e.Message);
                    return Result.Failed;
                }

            }
        }

    }
    /// <summary>
    /// Stores the Settings state, either by loading it in from the user's preferences or setting the state to their default values
    /// </summary>
    public class MyApplicationSettings
    {
        public static MyApplicationSettings Instance;
        public ObservableCollection<ComponentState> settingsState = new ObservableCollection<ComponentState>();


        public List<ComponentState> InitComponentStates(Assembly assembly)
        {
            //List<ComponentState> states;
            SetToDefault(assembly);
            //if (!string.IsNullOrEmpty(MySettings.Default.ComponentState_Preferences)) LoadFromDB(assembly);
            //else SetToDefault(assembly);
            return settingsState.ToList();
        }
        public void SaveToDB()
        {
            if (settingsState == null || settingsState.Count == 0) return;
            MySettings.Default.ComponentState_Preferences = JsonConvert.SerializeObject(settingsState.ToList());
            MySettings.Default.Save();
        }
        private List<Type> FindComponentsInNamespace(Assembly assembly)
        {
            List<Type> componentTypes = new List<Type>();
            Type componentBaseType = typeof(ISettingsComponent);
            string namespaceString = typeof(ThisApplication).Namespace;
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
                    bool isEnabled = (isWIP != null) ? !(bool)isWIP : true;
                    settingsState.Add(new ComponentState(type, isEnabled, !isEnabled));
                }
                catch (Exception ex)
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
                    List<ComponentState> states = JsonConvert.DeserializeObject<List<ComponentState>>(MySettings.Default.ComponentState_Preferences);
                    foreach (ComponentState state in states)
                    {
                        settingsState.Add(state);
                    }
                }
                else
                {
                    SetToDefault(assembly);
                }
            }catch (Exception ex)
            {
                TaskDialog.Show("ERROR LOADING FROM DB", ex.Message);
                SetToDefault(assembly);
            }
            

            
        }
    }


    /*
     * Purpose: 
     *  ->To store data related to loading in commands as ribbon buttons and where those buttons are located
     * -Pro: 
     *  -> Can easily enable and disable commands ( and their updaters ) without having to recompile.
     *  -> Can easily add new commands with less code
     *  -> Abstracts away how and which commands should be loaded and registered
     *  -> Abstracts away a command and their dependent buttons, updaters, and windows
     * -Cons: 
     *  -> Now each command ( with a button ) must implement ISettingsComponent and have a const bool IsWIP to be loaded in properly. This is unless the command is just set to always be loaded in, such as the open settings window command.
     */

    /// <summary>
    /// All commands that require a button on the ribbon panel, and are created by this Addin should implement this interface.
    /// </summary>
    public interface ISettingsComponent
    {
        PushButtonData RegisterButton(Assembly assembly);
    }

    /// <summary>
    /// All commands that require a button on the ribbon panel, are created by this Addin, and have dedicated XAML UI ( such as a Window or Dockable Pane ) should implement this interface.
    /// </summary>
    public interface ISettingsUIComponent : ISettingsComponent 
    {
        void Init(UIDocument uidoc);
    }
    
    /// <summary>
    /// All commands that require a button on the ribbon panel, are created by this addin, and have their own corresponding Updater should implement this interface.
    /// </summary>
    public interface ISettingUpdaterComponent : ISettingsComponent
    { 
        void RegisterUpdater(AddInId addinId);
        void UnregisterUpdater();
    }

    /// <summary>
    /// Stores information on all the classes that extend the ISettingsComponent interface and are located within the Assembly. 
    /// In other words, all classes with commands and UI should have their type information stored here. 
    /// The information stored is used with reflection to more easily register components to Revit. 
    /// It is also used to set the default state on whether or not Revit will register and load each command as a button by checking whether or not it is WIP. 
    /// </summary>
    public class ComponentState : INotifyPropertyChanged

    {
        public string ParentName;
        public Type ComponentType;
        public string ComponentName
        {
            get 
            {
                if (ComponentType == null) return "Null Type";
                return ComponentType.Name + ((IsWIP) ? " ( WIP )" : "");
            }
        }
        public bool IsWIP = false;
        
        //public ComponentInfo Info { get; set; }
        private bool isEnabled;
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (value == isEnabled) return;
                isEnabled = value;
                OnNotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ComponentState(Type componentType, bool isEnabled, bool isWIP) 
        { 
            
            ComponentType = componentType;
            IsEnabled = isEnabled;
            IsWIP = isWIP;
        }
        /* Below was considered to be held within the ComponentState class such that a more generic method of registering a component ( a class and their respective buttons, updates, and windows ), 
         * but the added complexity using lowered readability. Thus just loading in the Command's buttons, updaters, windows, etc. are now located within the ThisApplication class
         * 
         * public static void RegisterComponent<T>(Assembly assembly)
        {
            Type componentType = typeof(T);
            if (!(componentType is ISettingsComponent)) return;
            componentType.GetMethod(componentType.FullName + ".RegisterButton()")?.Invoke(null, new object[]{ assembly });
        }*/

    /*
     * This is used for databinding to the Settings Window, detecting when the user changes a detail of the ComponentStates ( i.e. enabling / disabling a component )
     */
    protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(memberName));
            }
        }
        
    }


}
