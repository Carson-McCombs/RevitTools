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

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for MyApplicationSettingsWindow.xaml
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


        [Transaction(TransactionMode.Manual)]
        public class ShowSettingsWindow : IExternalCommand
        {
            private MyApplicationSettingsWindow instance;

            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                try
                {
                    if (instance == null) instance = new MyApplicationSettingsWindow();
                    //MyApplicationSettings.Instance.InitComponentStates(Assembly.GetExecutingAssembly());
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
    public class MyApplicationSettings
    {
        public static MyApplicationSettings Instance;
        public ObservableCollection<ComponentState> settingsState = new ObservableCollection<ComponentState>();
        //private Dictionary<string, int> componentsByName = new Dictionary<string, int>();

        public List<ComponentState> InitComponentStates(Assembly assembly)
        {
            //List<ComponentState> states;
            if (!string.IsNullOrEmpty(MySettings.Default.ComponentState_Preferences)) LoadFromDB(assembly);
            else LoadFromDefault(assembly);
            return settingsState.ToList();
        }
        /*public bool GetState(string componentName)
        {
            if (!componentsByName.ContainsKey(componentName)) return false;
            return settingsState[componentsByName[componentName]].IsEnabled;
        }*/
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
        private void LoadFromDefault(Assembly assembly)
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
        public void LoadFromDB(Assembly assembly)
        {
            try
            {
                settingsState.Clear();
                if (!string.IsNullOrWhiteSpace(MySettings.Default.ComponentState_Preferences))
                {
                    List<ComponentState> states = JsonConvert.DeserializeObject<List<ComponentState>>(MySettings.Default.ComponentState_Preferences);
                    //componentsByName = GetComponentIndexDictionary(states);
                    foreach (ComponentState state in states)
                    {
                        settingsState.Add(state);
                    }
                    //settingsState = new ObservableCollection<ComponentState>(states);
                }
                else
                {
                    LoadFromDefault(assembly);
                }
            }catch (Exception ex)
            {
                TaskDialog.Show("ERROR LOADING FROM DB", ex.Message);
                LoadFromDefault(assembly);
            }
            

            
        }
    }

    public interface ISettingsComponent
    {
        //ComponentInfo info { get; set; }
        PushButtonData RegisterButton(Assembly assembly);
    }
    public interface ISettingsUIComponent : ISettingsComponent 
    {
        void Init(UIDocument uidoc);
        

    }
    public interface ISettingUpdaterComponent : ISettingsComponent
    { 
        void RegisterUpdater(AddInId addinId);
        void UnregisterUpdater();
    }



    //class ComponentInfo 
    //{

    //    public string ComponentName;

    //    public string ComponentType;
    //    public string ClassPath;
    //    public ComponentInfo(Type t) 
    //    {

    //        ComponentName = t.Name;
    //        ClassPath = t.FullName;
    //        ComponentType = t.BaseType.Name;
    //    }
    //}

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
        /*public static void RegisterComponent<T>(Assembly assembly)
        {
            Type componentType = typeof(T);
            if (!(componentType is ISettingsComponent)) return;
            componentType.GetMethod(componentType.FullName + ".RegisterButton()")?.Invoke(null, new object[]{ assembly });
        }*/
        

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(memberName));
            }
        }
        
    }


}
