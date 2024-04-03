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
using System.Configuration;
using System.Configuration.Assemblies;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
using static CarsonsAddins.PipeEndPrepWindow;
using static CarsonsAddins.Util;

namespace CarsonsAddins
{
    /// <summary>
    /// Tracks Pipes and their connections, allowing their "Pipe End Prep" Parameter to be filled automatically. The "Pipe End Prep" is based on the connecting flanges/unions, but only tested in a flanged environment.
    /// </summary>
    public partial class PipeEndPrepWindow : Page, IDockablePaneProvider, ISettingsUIComponent, ISettingsUpdaterComponent
    {
        public const bool IsWIP = false;

        public delegate void ToggleEventUpdaterEvent(bool enabled);
        public event ToggleEventUpdaterEvent ToggleUpdater;
        private static PipingEndPrepUpdater updater;
        private bool enabled = false;
        private bool forceUpdate = false;
        UIDocument uidoc = null;
        Document doc = null;
        public ObservableCollection<PipeEndPrepPreferences> preferences;

        //ComponentInfo ISettingsComponent.info { get => new ComponentInfo(this); }

        public PipeEndPrepWindow()
        {
            InitializeComponent();
            
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("Pipe End Prep", "Pipe End Prep", assembly.Location, typeof(ShowDockablePane<PipeEndPrepWindow>).FullName);
            pushButtonData.ToolTip = "Opens Pipe End Prep Settings Window";
            return pushButtonData;
        }

        public void RegisterUpdater(AddInId addinId)
        {
            updater = new PipingEndPrepUpdater(addinId);
            updater.Link(this);
        }

        public void UnregisterUpdater()
        {
            updater.Unregister();
        }

        public void Init(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            doc = uidoc.Document;
            enabled = false;
            forceUpdate = false;
            Init();
        }
        private void Init()
        {

            if (doc == null) return;
            Transaction trans = new Transaction(doc);
            trans.Start("PopulatePEPFittings");
            try
            {
                //preferences = new ObservableCollection<PipeEndPrepPreferences>();
                //LoadFromDB();
                if (!LoadFromSettings()) LoadFromDB();


                trans.Commit();


            }
            catch (Exception ex)
            {
                TaskDialog.Show("PEP ERROR", ex.Message);
                trans.RollBack();
            }
            PipeEndPrepDataGrid.ItemsSource = preferences;
        }
        private bool LoadFromSettings()
        {
            if (MySettings.Default.PEP_Preferences == null) return false;
            preferences = new ObservableCollection<PipeEndPrepPreferences>(JsonConvert.DeserializeObject<List<PipeEndPrepPreferences>>(MySettings.Default.PEP_Preferences));
            PipeEndPrepDataGrid.ItemsSource = preferences;
            return true;
        }
        private void LoadFromDB()
        {
            preferences = new ObservableCollection<PipeEndPrepPreferences>();
            List<Element> fittings = GetAllPipeFittingFamilies(doc);
            foreach (Element element in fittings)
            {
                FamilySymbol familySymbol = element as FamilySymbol;

                if (!familySymbol.Family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE).AsInteger().Equals((int)PartType.PipeFlange)) continue;
                preferences.Add(new PipeEndPrepPreferences(familySymbol.Id.ToString(), familySymbol.FamilyName));
            }
            PipeEndPrepDataGrid.ItemsSource = preferences;
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
        public void UpdatePipeEndPrep(Pipe pipe)
        {
            if (!enabled) return;
            Parameter param = pipe.LookupParameter("Pipe End Prep");

            if (param == null) return; 
            bool isBlank = string.IsNullOrWhiteSpace(param.AsValueString());
            if (!isBlank && !forceUpdate) return;

            string pipeEndPrep = GetBothEndPreps(pipe);
            //string pipeEndPrep = GetConnectorNames(pipe);
            if (isBlank || !pipeEndPrep.Equals(param.AsValueString()))
            {
                param.Set(pipeEndPrep);
            }
        }
        
        public string GetBothEndPreps(Pipe pipe)
        {

            List<(BellOrSpigot, string)> endPrepData = new List<(BellOrSpigot, string)>();
            foreach (Connector connector in pipe.ConnectorManager.Connectors)
            {
                endPrepData.Add(GetEndPrep(pipe, connector));
            }
            bool reorder = false;
            if (endPrepData[0].Item1.Equals(endPrepData[1].Item1)) // same ending types (i.e. either as bell x bell or pe x pe or none x none)
            {
                if (endPrepData[0].Item2 != "PE") reorder = true;
                else reorder = endPrepData[0].Item2.CompareTo(endPrepData[1].Item2) > 0;

            }
            else
            {
                reorder = ((int)endPrepData[0].Item1 > (int)endPrepData[1].Item1); //reorders end preps such that a bell will always be before a spigot and a spigot will always be before a 'none' type

            }
            string firstEndPrep = reorder ? endPrepData[1].Item2 : endPrepData[0].Item2;
            string secondEndPrep = reorder ? endPrepData[0].Item2 : endPrepData[1].Item2;
            return firstEndPrep + " x " + secondEndPrep;
        }


        public (BellOrSpigot,string) GetEndPrep(Pipe pipe, Connector connector)
        {
            if (connector == null) return (BellOrSpigot.NONE, "NULL");
            FamilyInstance familyInstance = GetConnectedFamilyInstance(connector);
            if (familyInstance == null) return (BellOrSpigot.SPIGOT, "PE");
            PipeEndPrepPreferences prefs = GetPreferences(familyInstance);
            BellOrSpigot bos = GetPipeEnd(pipe, familyInstance);
            string pipeEndPrep = prefs.GetPipeEndPrep(bos);
            //if (bos.Equals(BellOrSpigot.SPIGOT))
            //{
            //    if(IsGaugedPE(pipe, connector, pipeEndPrep)) pipeEndPrep = "GPE";
            //}
            return (bos, pipeEndPrep);
        }

        
        
        
        
        private PipeEndPrepPreferences GetPreferences(Element element)
        {
            FamilyInstance fitting = element as FamilyInstance;
            if (fitting == null) return new PipeEndPrepPreferences(ElementId.InvalidElementId.ToString(), "NULL");

            ElementId elementId = fitting.Symbol.Id;
            foreach (PipeEndPrepPreferences pref in preferences)
            {
                if (pref.pipeTypeId.Equals(elementId.ToString())) return pref;
            }
            return new PipeEndPrepPreferences(ElementId.InvalidElementId.ToString(), "NULL");
        }



        private void ToggleUpdatePipeEndPrep(object sender, RoutedEventArgs e)
        {
            enabled = !enabled;
            TogglePEPButton.Content = (enabled) ? "DISABLE" : "ENABLE";
            PEPActivityLabel.Content = (enabled) ? "ACTIVE" :  "NOT ACTIVE";
            System.Windows.Media.Color color = (enabled) ? Colors.Green : Colors.Red;
            PEPActivityLabel.Foreground = new SolidColorBrush(color);

            //if (PipingEndPrepUpdater.tmp == null || !PipingEndPrepUpdater.tmp.IsValidObject) TaskDialog.Show("PEP ERROR", "UPDATER ADDIN ID IS NULL");
            //TaskDialog.Show("PEP", PipingEndPrepUpdater.tmp.GetAddInName() + '\n' + uidoc.Application.ActiveAddInId.GetAddInName());
            //if (ToggleUpdater.GetInvocationList().Count() == 0) TaskDialog.Show("Toggle PEP Updater Error", "Updater not linked.");
            ToggleUpdater?.Invoke(enabled);
            //updater?.ToggleEnabled(enabled);
        }



        private void ForceUpdate_Checked(object sender, RoutedEventArgs e)
        {
            forceUpdate = true;
        }

        private void ForceUpdate_Unchecked(object sender, RoutedEventArgs e)
        {
            forceUpdate = false;
            
        }

        private void PEPSaveButton_Click(object sender, RoutedEventArgs e)
        {
            MySettings.Default.PEP_Preferences = JsonConvert.SerializeObject(preferences);
            MySettings.Default.Save();
            //TaskDialog.Show("SAVING", MySettings.Default.PEP_Preferences);
            //MySettings.Default.Properties["PEPList"]
        }
        private void PEPRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFromSettings();

        }
        private void PEPDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFromDB();


        }

        
    }
    public class PipeEndPrepPreferences
    {
        public string pipeTypeId = "-1";
        private string pipeTypeName = "default_pipe_type";
        public string PipeTypeName { get { return pipeTypeName; } }
        private string bellEndPrep = "DEFAULT_BELL";
        public string BellEndPrep { get { return bellEndPrep; } set { if (!bellEndPrep.Equals(value)) { bellEndPrep = value; } } }
        private string spigotEndPrep = "DEFAULT_SPIGOT";
        public string SpigotEndPrep { get { return spigotEndPrep; } set { if (!spigotEndPrep.Equals(value)) { spigotEndPrep = value; } } }

        public PipeEndPrepPreferences(string pipeTypeId, string pipeTypeName)
        {
            this.pipeTypeId = pipeTypeId;
            this.pipeTypeName = pipeTypeName;
        }
        [JsonConstructor]
        public PipeEndPrepPreferences(string pipeTypeId, string pipeTypeName, string bellEndPrep, string spigotEndPrep)
        {
            this.pipeTypeId = pipeTypeId;
            this.pipeTypeName = pipeTypeName;
            this.bellEndPrep = bellEndPrep;
            this.spigotEndPrep = spigotEndPrep;
        }
        public string GetPipeEndPrep(BellOrSpigot bellOrSpigot)
        {
            if (bellOrSpigot.Equals(BellOrSpigot.SPIGOT)) return spigotEndPrep;
            if (bellOrSpigot.Equals(BellOrSpigot.BELL)) return bellEndPrep;
            return " NULL ";

        }
    }
}

#region OLDCODE
//private List<string> GetConnectorNameList(Pipe pipe)
//{
//    List<Connector> allConnectorRefs = GetAllConnectorRefs(pipe);
//    List<string> bells = new List<string>();
//    List<string> spigots = new List<string>();
//    foreach (Connector con in allConnectorRefs)
//    {
//        try
//        {
//            if (con.Owner == null) continue;
//            if (!con.Owner.IsValidObject) continue;

//            FamilyInstance fitting = con.Owner as FamilyInstance;

//            if (fitting.Symbol.Family.Id.Equals(ElementId.Parse("4584135")))
//            {
//                Connector next = TryGetConnected(con);
//                if (next == null) continue;
//                if (next.Owner == null) continue;
//                if (!next.Owner.IsValidObject) continue;
//                fitting = next.Owner as FamilyInstance;
//            }
//            if (fitting == null) continue;
//            PipeEndPrepPreferences prefs = GetPreferences(fitting);
//            BellOrSpigot bos = GetPipeEnd(pipe, fitting);
//            if (bos.Equals(BellOrSpigot.BELL)) bells.Add(prefs.GetPipeEndPrep(bos));
//            else if (bos.Equals(BellOrSpigot.SPIGOT))
//            {
//                string spigotEndPrep = prefs.GetPipeEndPrep(bos);
//                if (IsGaugedPE(pipe, con, spigotEndPrep)) spigotEndPrep = "GPE";
//                spigots.Add(spigotEndPrep);
//            };
//        }catch(Exception e)
//        {
//            List<string> strings = new List<string>();
//            strings.AddRange(bells);
//            strings.AddRange(spigots);
//            string s = "";
//            strings.ForEach(str => s = s + str);
//            TaskDialog.Show("PEP ERROR", "GetConnectorNameList: \n" + e.Message + "\n\n" + s);
//            continue;
//        }

//    }
//    List<string> connectionNames = new List<string>();
//    connectionNames.AddRange(bells);
//    connectionNames.AddRange(spigots);

//    return connectionNames;
//}
//private string GetConnectorNames(Pipe pipe)
//{
//    List<string> nameList = GetConnectorNameList(pipe);
//    if (nameList.Count == 0) return "PE x PE";
//    else if (nameList.Count == 1) return nameList[0] + " x ---";
//    else if (nameList.Count == 2) return nameList[0] + " x " + nameList[1];
//    string name = "";

//    foreach (string n in nameList)
//    {
//        name = name + n + " x ";// + '\n';
//    }
//    return name;

//}

//private Element GetConnectedElement(Connector connector)
//{
//    if (connector == null) return null;
//    int i = 0;
//    foreach (Connector c in connector.AllRefs)
//    {
//        if (i == 1) return c.Owner;
//        i++;
//    }
//    return null;
//}
#endregion