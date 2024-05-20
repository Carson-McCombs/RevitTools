using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using CarsonsAddins.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CarsonsAddins.Utils.ConnectionUtils;

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for PipeEndPrepBCWindow.xaml
    /// </summary>
    public partial class PipeEndPrepBCWindow : Window, ISettingsUIComponent, ISettingsUpdaterComponent, INotifyPropertyChanged
    {

        public const string FolderName = "Automation";
        public const bool IsWIP = false;
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly RepeatedFunctionCallEventHandler handler = new RepeatedFunctionCallEventHandler();
        private IntPtr windowHandle;
        private readonly ExternalEvent updateSelectionEvent;
        private PipeEndPrepBCUpdater updater;
        private bool isRunning = false;

        private readonly Brush redBrush = new SolidColorBrush(Colors.Red);
        private readonly Brush greenBrush = new SolidColorBrush(Colors.Green);


        private bool isDomestic = false;
        private bool IsDomestic
        {
            get => isDomestic;
            set
            {
                if (isDomestic == value) return;
                isDomestic = value;
                OnNotifyPropertyChanged();
            }

        }
        private bool checkIfTapped = false;
        private bool CheckIfTapped
        {
            get => checkIfTapped;
            set
            {
                if (checkIfTapped == value) return;
                checkIfTapped = value;
                OnNotifyPropertyChanged();
            }
        }
        private bool checkPipeClass = false;
        private bool CheckPipeClass
        {
            get => checkPipeClass;
            set
            {
                if (checkPipeClass == value) return;
                checkPipeClass = value;
                OnNotifyPropertyChanged();
            }
        }
        private bool checkPipePenetrations = false;
        private bool CheckPipePenetrations
        {
            get => checkPipePenetrations;
            set
            {
                if (checkPipePenetrations == value) return;
                checkPipePenetrations = value;
                OnNotifyPropertyChanged();
            }
        }
        private bool forceUpdate;
        private bool ForceUpdate
        {
            get => forceUpdate;
            set
            {
                if (forceUpdate == value) return;
                forceUpdate = value;
                OnNotifyPropertyChanged();
            }
        }




        private static Definition pipeEndPrepDefinition;

        private UIDocument uidoc;

        public PipeEndPrepBCWindow()
        {
            InitializeComponent();
            updateSelectionEvent = ExternalEvent.Create(handler);
            handler.Functions += UpdateSelection;
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            return new PushButtonData("Pipe End Prep BC", "Pipe End Prep BC", assembly.Location, typeof(GenericCommands.ShowWindow<PipeEndPrepBCWindow>).FullName)
            {
                AvailabilityClassName = typeof(Setup.Availablity.Availability_ProjectDocumentsOnly).FullName,
                ToolTip = "Opens Pipe End Prep By Connectors DockablePane"
            };
        }

        public void RegisterUpdater(AddInId addinId)
        {
            updater = new PipeEndPrepBCUpdater(addinId);
            updater.UpdateEndPrep += UpdatePipeEndPreps;
        }

        public void UnregisterUpdater()
        {
            if (updater == null) return;
            updater.Unregister();
        }

        public void Init(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            RegisterUpdater(uidoc.Application.ActiveAddInId);

        }

        private void SetUpdaterState(bool state)
        {
            isRunning = state;
            updater.SetState(state);
            if (updater == null) return;
            StatusTextBlock.Text = isRunning ? "Status: Running" : "Status: Not Running";
            StatusTextBlock.Foreground = isRunning ? greenBrush : redBrush;
            ToggleStatusButton.Content = isRunning ? "Disable" : "Enable";
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = this;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Bottom,
            };
        }

        private void ToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            SetUpdaterState(!isRunning);
        }
        private void UpdateSelection_Click(object sender, RoutedEventArgs e)
        {
            
            updateSelectionEvent.Raise();
            windowHandle = MediaUtils.GetForegroundWindow();
            Hide();
            MediaUtils.SetForegroundWindow(uidoc.Application.MainWindowHandle);
        }

        private void UpdateSelection(Document doc)
        {
            Transaction transaction = new Transaction(doc);
            transaction.Start("Update PEP");
            List<Reference> references = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, new SelectionFilters.SelectionFilter_Pipe()).ToList();
            Element[] elements = references.Select(reference => doc.GetElement(reference)).ToArray();
            Pipe[] pipes = elements.Where(element => element != null && element is Pipe).Cast<Pipe>().ToArray();
            UpdatePipeEndPreps(doc, pipes);
            transaction.Commit();
            if (windowHandle != null) MediaUtils.SetForegroundWindow(windowHandle);
            ShowDialog();
        }

        private bool SetDefinitions(Document doc)
        {
            DefinitionBindingMapIterator iterator = doc.ParameterBindings.ForwardIterator();
            while (iterator.MoveNext())
            {
                string parameterName = iterator.Key.Name;
                if (!"Pipe End Prep".Equals(parameterName)) continue;
                pipeEndPrepDefinition = iterator.Key;
                return true;
            }
            return false;
        }
       
        private void UpdatePipeEndPreps(Document doc, Pipe[] pipes)
        {
            bool err = false;
            foreach (Pipe pipe in pipes)
            {
                if (!UpdatePipeEndPrep(doc, pipe)) err = true;
            }
            if (err)
            {
                TaskDialog.Show("Update PEP By Connectors Error", "Please ensure that connectors have their description set in the format \"A-B\" where A is either \"B\" / \"Bell\" or \"S\" / \"Spigot\" and B is the label for that pipe end prep.");
            }
        }

        private bool UpdatePipeEndPrep(Document doc, Pipe pipe)
        {
            SubTransaction subtransaction = new SubTransaction(doc);
            subtransaction.Start();
            try
            {
                const double largestTapDistance = 0.5; //6"
                if (pipeEndPrepDefinition == null && SetDefinitions(doc) == false) return false;
                if (pipe == null) 
                { 
                    subtransaction.RollBack();
                    return false;
                }
                
                if (pipeEndPrepDefinition == null)
                {
                    subtransaction.RollBack();
                    return false;
                }
                Parameter endPrepParameter = pipe.get_Parameter(pipeEndPrepDefinition);
                if (endPrepParameter == null)
                {
                    subtransaction.RollBack();
                    return false;
                }
                Connector[] connectors = GetConnectors(pipe);
                string currentEndPrep = endPrepParameter.AsString();

                EndPrepInfo endPrepA = EndPrepInfo.GetEndPrepByConnector(connectors[0]);
                EndPrepInfo endPrepB = EndPrepInfo.GetEndPrepByConnector(connectors[1]);

                if (endPrepA.endType == BellOrSpigot.NONE || endPrepB.endType == BellOrSpigot.NONE)
                {
                    return false;
                }
                endPrepA.isDomestic = IsDomestic;
                endPrepB.isDomestic = IsDomestic;

                Curve[] intersectionSegments = GetIntersectionsBySolids(doc, pipe);
                XYZ[] wallCenters = intersectionSegments.Select(curve => (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2).ToArray();

                double closestDistA = GetShortestDistanceToWalls(endPrepA.position, intersectionSegments);
                double closestDistB = GetShortestDistanceToWalls(endPrepB.position, intersectionSegments);

                endPrepA.isTapped = (closestDistA < largestTapDistance) && (endPrepA.endType.Equals(BellOrSpigot.BELL)) && CheckIfTapped;
                endPrepB.isTapped = (closestDistB < largestTapDistance) && (endPrepB.endType.Equals(BellOrSpigot.BELL)) && CheckIfTapped;

                bool reorder = CheckIfReorder(endPrepA, endPrepB, closestDistA, closestDistB);
                if (reorder)
                {
                    (endPrepA, endPrepB) = (endPrepB, endPrepA);
                    (closestDistA, closestDistB) = (closestDistB, closestDistA);
                }
                string wcString = IsDomestic ? "DWC" : "WC";

                double[] distancesFromPrepA = wallCenters.Select(wallCenter => endPrepA.position.DistanceTo(wallCenter)).OrderBy(dist => dist).ToArray();

                List<string> endPrepStrings = new List<string> { endPrepA.ToString() };
                if (CheckPipePenetrations) Array.ForEach(intersectionSegments, curves => endPrepStrings.Add(wcString));
                endPrepStrings.Add(endPrepB.ToString());
                string combinedEndPrep = string.Join(" x ", endPrepStrings);

                string[] commentStrings = distancesFromPrepA.Select(distance => "w/ " + wcString + " " + new Utils.UnitUtils.FeetAndInchesFraction(distance, 16).ToString() + " FROM " + endPrepA.ToString()).ToArray();
                string combinedComments = string.Join("; ", commentStrings);
                Parameter commentsParameter = pipe.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                if (combinedEndPrep.Equals(currentEndPrep) && (commentsParameter.AsString() == combinedComments) && ForceUpdate)
                {
                    subtransaction.RollBack();
                }

                commentsParameter.Set(combinedComments);
                endPrepParameter.Set(combinedEndPrep);
                subtransaction.Commit();

                return true;
            } catch (Exception ex)
            {
                subtransaction.RollBack();
                TaskDialog.Show("Error Updating PEP By Connectors", ex.Message);
                return false;
            }
            
        }
        private double GetShortestDistanceToWalls(XYZ endPoint, Curve[] curves)
        {
            double shortestDistance = double.MaxValue;
            foreach (Curve curve in curves)
            {
                for (int i = 0; i < 2; i++)
                {
                    shortestDistance = Math.Min(shortestDistance, endPoint.DistanceTo(curve.GetEndPoint(i)));

                }
            }
            return shortestDistance;
        }

        private Solid[] GetIntersectingSolids(Document doc, Pipe pipe)
        {
            Options options = new Options { DetailLevel = ViewDetailLevel.Undefined, IncludeNonVisibleObjects = false, ComputeReferences = true };
            BuiltInCategory[] validCategories = new BuiltInCategory[] { BuiltInCategory.OST_StructuralFoundation, BuiltInCategory.OST_Walls, BuiltInCategory.OST_Floors };
            ElementMulticategoryFilter categoryFilter = new ElementMulticategoryFilter(validCategories);
            ElementIntersectsElementFilter intersectionFilter = new ElementIntersectsElementFilter(pipe);
            List<Solid> solids = new List<Solid>();
            Element[] intersectingElements = new FilteredElementCollector(doc).WherePasses(categoryFilter).WherePasses(intersectionFilter).ToElements().ToArray();
            Array.ForEach(intersectingElements, element => solids.AddRange(element.get_Geometry(options).OfType<Solid>()));
            return solids.ToArray();
        }
        private Curve[] GetIntersectionsBySolids(Document doc, Pipe pipe)
        {
            List<Curve> intersectionSegments = new List<Curve>();
            Curve pipeCurve = (pipe.Location as LocationCurve).Curve;
            Solid[] intersectingSolids = GetIntersectingSolids(doc, pipe);

            if (intersectingSolids.Length == 0) return new Curve[0];
            SolidCurveIntersectionOptions solidCurveIntersectionOptions = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside };
            foreach (Solid solid in intersectingSolids)
            {
                SolidCurveIntersection solidIntersection = solid.IntersectWithCurve(pipeCurve, solidCurveIntersectionOptions);
                for (int i = 0; i < solidIntersection.SegmentCount; i++)
                {
                    Curve curve = solidIntersection.GetCurveSegment(i);
                    if (curve == null) continue;
                    intersectionSegments.Add(curve);
                }

            }

            return intersectionSegments.ToArray();
        }

        protected void OnNotifyPropertyChanged([CallerMemberName] string memberName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
        }
    }
}
