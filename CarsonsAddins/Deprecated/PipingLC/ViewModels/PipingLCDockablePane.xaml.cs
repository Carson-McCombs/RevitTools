using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CarsonsAddins;
using CarsonsAddins.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration.Assemblies;
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

namespace CarsonsAddins
{
    /// <summary>
    /// Warning, not updated in a long time, presumed to be unusable
    /// </summary>
    public partial class PipingLCDockablePane : Page, IDockablePaneProvider, ISettingsUIComponent, ISettingsUpdaterComponent
    {
        public const string FolderName = "Automation";
        public const bool IsWIP = true;
        //private readonly PipingLCUpdater updater;
        public delegate void ToggleLCUpdaterEvent(bool enabled);
        public event ToggleLCUpdaterEvent ToggleLCUpdater;
        UIDocument uidoc = null;
        Document doc = null;
        //PipingSystemLC selectedSystem = null;
        ObservableCollection<PipingSystemLC> pipingSystemLCs;
        public PipingLCDockablePane()
        {
            InitializeComponent();
        }
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("OpenPreferencesWindow", "Open Preferences Window", assembly.Location, "CarsonsAddins.ShowPreferenceWindow")
            {
                AvailabilityClassName = typeof(Setup.Availablity.Availability_ProjectDocumentsOnly).FullName,
                ToolTip = "Opens Dockable Preference Window"
            };
            return pushButtonData;
        }
        public void RegisterUpdater(AddInId addinId)
        {
            //updater = new PipingLCUpdater(addinId);
        }

        public void UnregisterUpdater()
        {
            //updater.Unregister();
        }
        public void Init(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            doc = uidoc.Document;
            Init();
        }
        private void Init()
        {
            pipingSystemLCs = new ObservableCollection<PipingSystemLC>();
            Transaction trans = new Transaction(doc);
            trans.Start("Get All Pipe Types");
            try
            {
                PipingSystemLC.DefaultPipeTypeNames = new List<string>();
                List<Element> fams = Utils.DatabaseUtils.GetAllPipeTypeFamilies(doc);
                foreach (Element fam in fams)
                {
                    PipingSystemLC.DefaultPipeTypeNames.Add(fam.Name);
                }
                
                if (!string.IsNullOrWhiteSpace(MySettings.Default.LC_Preferences)) LoadFromSettings();
                else LoadFromDB();

                trans.Commit();

            }
            catch
            {
                trans.RollBack();

            }
            //PipeSystemControl.ItemsSource = pipingSystemLCs.FirstOrDefault().PipeTypeLCs;
            //PSSelectorControl.Inject(pipingSystemLCs);
            PipingSystemSelectorControl.SelectPipingSystemEvent += OnSelectPipingSystem;
            //pipingSystemLCUpdater = new PipingLCUpdater(this, doc);
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right,
                //TabBehind = DockablePanes.BuiltInDockablePanes.
            };
            
           
        }
        private void SaveToSettings()
        {
            MySettings.Default.LC_Preferences = JsonConvert.SerializeObject(pipingSystemLCs.ToList());
            MySettings.Default.Save();
        }
        private void LoadFromDB()
        {
            List<Element> elems = Utils.DatabaseUtils.GetAllPipeSystemFamilies(doc);
            foreach (Element elem in elems)
            {
                PipingSystemLC ps = new PipingSystemLC(elem.Name);

                pipingSystemLCs.Add(ps);
            }
            PipeSystemControl.ItemsSource = pipingSystemLCs.FirstOrDefault().PipeTypeLCs;
            PSSelectorControl.Init(pipingSystemLCs);
        }
        private void LoadFromSettings() 
        {
            pipingSystemLCs = new ObservableCollection<PipingSystemLC>(JsonConvert.DeserializeObject<List<PipingSystemLC>>(MySettings.Default.LC_Preferences));
            PipeSystemControl.ItemsSource = pipingSystemLCs.FirstOrDefault().PipeTypeLCs;
            PSSelectorControl.Init(pipingSystemLCs);

        }
        public void OnSelectPipingSystem(PipingSystemLC system)
        {
            if (system == null)
            {
                TaskDialog.Show("ERROR WHEN SELECTING PIPING SYSTEM", "TRIED TO SELECT NULL ITEM");
                return;
            }
            
            //PipeSystemControl.DataContext = system;
            PipeSystemControl.ItemsSource = system.PipeTypeLCs;
            //TaskDialog.Show("SELECTED PIPING SYSTEM", system.ToString());
            //selectedSystem = system;
        }

        public PipeTypeLC GetPipeTypeLC(Element elem)
        {
            string systemName = elem.LookupParameter("System Type").AsValueString();
            string familyName = elem.LookupParameter("Type").AsValueString();
            
            for (int i = 0; i < pipingSystemLCs.Count; i++)
            {
                if (pipingSystemLCs[i].PipingSystemName != systemName) continue;
                //TaskDialog.Show("GETTING FAMILY LC", pipingSystemLCs[i].ToString());
                return (pipingSystemLCs[i].GetPipeTypeLC(familyName));
            }
            
            return default;
        }
        public void SetToPipeTypeLC(Element elem)
        {

            PipeTypeLC pf = GetPipeTypeLC(elem);
            if (pf.Enabled == false) return;

            //Parameter coatingParam = elem.LookupParameter("C&B_Coating");
            //Parameter liningParam = elem.LookupParameter("SYS LINING");

            //bool ct = coatingParam.Set(pf.Coating);
            //bool ln = liningParam.Set(pf.Lining);
            //TaskDialog.Show("Set LC Status", "Coating ( " + ct.ToString() + " ), Lining ( " + ln.ToString() + " )");




        }
        public void SetToPipeTypeLCs(List<ElementId> elementIds)
        {
            for (int i = 0; i < elementIds.Count;i++)
            {
                Element elem = doc.GetElement(elementIds[i]);
                if (elem == null) continue;
                SetToPipeTypeLC(elem);
            }
        }
        
        private void SaveButton(object sender, RoutedEventArgs e)
        {
            SaveToSettings();
        }

        

    }
    
}
public class PipeTypeLC
{
    private bool enabled = false;
    public bool Enabled
    {
        get
        {
            return enabled;
        }
        set
        {
            if (enabled != value)
                enabled = value;
        }
    }
    private readonly string pipeTypeName = "default_pipe_type";
    public string PipeTypeName { get { return pipeTypeName; } }

    private string lining = "default_lining";
    public string Lining { get { return lining; } set { if (lining != value) lining = value; } }

    private string coating = "default_coating";


    public string Coating { get { return coating; } set { if (coating != value) coating = value; } }
    public PipeTypeLC(string pipeTypeName, string lining, string coating)
    {
        this.pipeTypeName = pipeTypeName;
        this.lining = lining;
        this.coating = coating;
    }
    public override string ToString()
    {
        return pipeTypeName + " -> " + lining + ", " + coating;
    }


}
public class PipingSystemLC
{
    public static List<string> DefaultPipeTypeNames;
    private readonly string pipingSystemName = "default_piping_system";
    public string PipingSystemName { get {  return pipingSystemName; } }
    private readonly ObservableCollection<PipeTypeLC> pipeTypeLCs;
public ObservableCollection<PipeTypeLC> PipeTypeLCs {  get { return pipeTypeLCs; } }
    public PipingSystemLC(string pipingSystemName)
    {
        this.pipingSystemName = pipingSystemName;
        pipeTypeLCs = new ObservableCollection<PipeTypeLC>(GetDefaultPipeTypeLCs());
       
    }
    private static List<PipeTypeLC> GetDefaultPipeTypeLCs()
    {
        List<PipeTypeLC> pipingFamilyLCs = new List<PipeTypeLC>();

        foreach (string name in DefaultPipeTypeNames)
        {
            pipingFamilyLCs.Add(new PipeTypeLC(name,"",""));
        }
        return pipingFamilyLCs;
    }


    public PipeTypeLC GetPipeTypeLC(string familyName)
    {
        for (int i = 0; i < pipeTypeLCs.Count; i++)
        {
            if (pipeTypeLCs[i].PipeTypeName == familyName) return pipeTypeLCs[i];
        }
        return default;
    }

    public override string ToString()
    {
        string s = PipingSystemName + '\n';
        foreach (PipeTypeLC fam in pipeTypeLCs)
        {
            s += fam.ToString() + '\n';
        }
        return s;
    }
}