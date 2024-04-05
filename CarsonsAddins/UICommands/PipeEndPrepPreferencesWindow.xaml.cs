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
using static CarsonsAddins.Utils.ConnectionUtils;

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
        Document doc = null;
        public ObservableCollection<PipeEndPrepPreferences> preferences;

        public PipeEndPrepWindow()
        {
            InitializeComponent();
            
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("Pipe End Prep", "Pipe End Prep", assembly.Location, typeof(GenericCommands.ShowDockablePane<PipeEndPrepWindow>).FullName)
            {
                ToolTip = "Opens Pipe End Prep Settings Window"
            };
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
            List<Element> fittings = Utils.DatabaseUtils.GetAllPipeFittingFamilies(doc);
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
        
        //Checks if the pipe end prep order should be reversed
        private bool CheckIfReorder((BellOrSpigot, string) endPrepA, (BellOrSpigot, string) endPrepB)
        {
            if (endPrepA.Item1.Equals(endPrepB.Item1)) // same ending types (i.e. either as bell x bell or pe x pe or none x none)
            {
                if (endPrepA.Item2 == "PE") return true;
                else if (endPrepB.Item2 == "PE") return false;
                else return endPrepA.Item2.CompareTo(endPrepB.Item2) > 0;

            }
            return ((int)endPrepA.Item1 > (int)endPrepB.Item1); //reorders end preps such that a bell will always be before a spigot and a spigot will always be before a 'none' type
        }

        public string GetBothEndPreps(Pipe pipe)
        {

            List<(BellOrSpigot, string)> endPrepData = new List<(BellOrSpigot, string)>();
            foreach (Connector connector in pipe.ConnectorManager.Connectors)
            {
                
                endPrepData.Add(GetEndPrep(connector));
            }
            bool reorder = CheckIfReorder(endPrepData[0], endPrepData[1]);
            
            string firstEndPrep = reorder ? endPrepData[1].Item2 : endPrepData[0].Item2;
            string secondEndPrep = reorder ? endPrepData[0].Item2 : endPrepData[1].Item2;
            return firstEndPrep + " x " + secondEndPrep;
        }


        public (BellOrSpigot,string) GetEndPrep(Connector connector)
        {
            if (connector == null) return (BellOrSpigot.NONE, "NULL");
            (FamilyInstance, Connector) connection = GetConnectedFamilyInstanceWithConnector(connector);
            if (connection.Item1 == null) return (BellOrSpigot.SPIGOT, "PE");
            PipeEndPrepPreferences prefs = GetPreferences(connection.Item1);
            BellOrSpigot bos = GetPipeEnd(connection.Item2);
            string pipeEndPrep = prefs.GetPipeEndPrep(bos);
            return (bos, pipeEndPrep);
        }

        
        
        
        
        private PipeEndPrepPreferences GetPreferences(Element element)
        {
            if (!(element is FamilyInstance fitting)) return new PipeEndPrepPreferences(ElementId.InvalidElementId.ToString(), "NULL");

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
        public readonly string pipeTypeId = "-1";
        private readonly string pipeTypeName = "default_pipe_type";
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