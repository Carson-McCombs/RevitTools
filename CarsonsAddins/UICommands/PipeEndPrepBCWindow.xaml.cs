using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static CarsonsAddins.Utils.ConnectionUtils;

namespace CarsonsAddins
{
    /// <summary>
    /// Interaction logic for PipeEndPrepBCWindow.xaml
    /// </summary>
    public partial class PipeEndPrepBCWindow : Window, ISettingsUIComponent, ISettingsUpdaterComponent
    {
        public const bool IsWIP = false;

        
        private PipeEndPrepBCUpdater updater;
        private bool isRunning = false;

        private Brush redBrush = new SolidColorBrush(Colors.Red);
        private Brush greenBrush = new SolidColorBrush(Colors.Green);

        private bool isDomestic = false;
        private bool IsDomestic { get => isDomestic; set  => isDomestic = value; }

        private bool checkIfTapped = false;
        private bool CheckIfTapped { get => checkIfTapped; set => checkIfTapped = value; }


        private bool checkClass = false;
        private bool CheckClass { get => checkClass; set => checkClass = value; }


        private bool checkPenetrations = false;
        private bool CheckPenetrations { get => checkPenetrations; set => checkPenetrations = value; }


        private bool forceUpdate = false;
        private bool ForceUpdate { get => forceUpdate; set => forceUpdate = value; }

        private Definition pipeEndPrepDefinition;

        private UIDocument uidoc;

        public PipeEndPrepBCWindow()
        {
            InitializeComponent();
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            return new PushButtonData("Pipe End Prep BC", "Pipe End Prep BC", assembly.Location, typeof(GenericCommands.ShowWindow<PipeEndPrepBCWindow>).FullName)
            {
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
            Hide();
            Document doc = uidoc.Document;
            List<Reference> references = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, new SelectionFilters.SelectionFilter_Pipe()).ToList();
            Element[] elements = references.Select(reference => doc.GetElement(reference)).ToArray();
            Pipe[] pipes = elements.Where(element => element != null && element is Pipe).Cast<Pipe>().ToArray();
            UpdatePipeEndPreps(doc, pipes);
            Show();
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
            Transaction transaction = new Transaction(doc);
            transaction.Start("Update PEP");
            foreach (Pipe pipe in pipes)
            {
                UpdatePipeEndPrep(doc, pipe);
            }
            transaction.Commit();
        }

        private void UpdatePipeEndPrep(Document doc, Pipe pipe)
        {
            try
            {
                const double largestTapDistance = 0.5; //6"
                if (pipeEndPrepDefinition == null && SetDefinitions(doc) == false) return;
                if (pipe == null) return;
                if (pipeEndPrepDefinition == null) return;
                Parameter endPrepParameter = pipe.get_Parameter(pipeEndPrepDefinition);
                if (endPrepParameter == null) return;
                Connector[] connectors = GetConnectors(pipe);
                string currentEndPrep = endPrepParameter.AsString();

                EndPrepInfo endPrepA = EndPrepInfo.GetEndPrepByConnector(connectors[0]);
                EndPrepInfo endPrepB = EndPrepInfo.GetEndPrepByConnector(connectors[1]);

                endPrepA.isDomestic = isDomestic;
                endPrepB.isDomestic = isDomestic;

                Curve[] intersectionSegments = GetIntersectionsBySolids(doc, pipe);
                XYZ[] wallCenters = intersectionSegments.Select(curve => (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2).ToArray();

                double closestDistA = GetShortestDistanceToWalls(endPrepA.position, intersectionSegments);
                double closestDistB = GetShortestDistanceToWalls(endPrepB.position, intersectionSegments);

                endPrepA.isTapped = (closestDistA < largestTapDistance) && (endPrepA.endType.Equals(BellOrSpigot.BELL)) && checkIfTapped;
                endPrepB.isTapped = (closestDistB < largestTapDistance) && (endPrepB.endType.Equals(BellOrSpigot.BELL)) && checkIfTapped;

                bool reorder = CheckIfReorder(endPrepA, endPrepB, closestDistA, closestDistB);
                if (reorder)
                {
                    (endPrepA, endPrepB) = (endPrepB, endPrepA);
                    (closestDistA, closestDistB) = (closestDistB, closestDistA);
                }
                string wcString = isDomestic ? "DWC" : "WC";

                double[] distancesFromPrepA = wallCenters.Select(wallCenter => endPrepA.position.DistanceTo(wallCenter)).OrderBy(dist => dist).ToArray();

                List<string> endPrepStrings = new List<string> { endPrepA.ToString() };
                if (checkPenetrations) Array.ForEach(intersectionSegments, curves => endPrepStrings.Add(wcString));
                endPrepStrings.Add(endPrepB.ToString());
                string combinedEndPrep = string.Join(" x ", endPrepStrings);

                string[] commentStrings = distancesFromPrepA.Select(distance => "w/ " + wcString + " " + new Utils.UnitUtils.FeetAndInchesFraction(distance, 16).ToString() + " FROM " + endPrepA.ToString()).ToArray();
                string combinedComments = string.Join("; ", commentStrings);
                Parameter commentsParameter = pipe.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);

                if (combinedEndPrep.Equals(currentEndPrep) && (commentsParameter.AsString() == combinedComments) && forceUpdate) return;

                commentsParameter.Set(combinedComments);
                endPrepParameter.Set(combinedEndPrep);



            } catch (Exception ex)
            {
                TaskDialog.Show("Error Updating PEP By Connectors", ex.Message);
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


    }
}
