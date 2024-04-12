using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using static CarsonsAddins.Utils.ConnectionUtils;

namespace CarsonsAddins
{
    class PipeEndPrepBCUpdater : IUpdater
    {
        private readonly UpdaterId updaterId;
        private static Definition pipeEndPrepDefinition;
        public PipeEndPrepBCUpdater(AddInId addinId)
        {
            updaterId = new UpdaterId(addinId, ApplicationIds.GetId(GetType()));

            Register();
            RegisterTriggers();
        }


        private static bool SetDefinitions(Document doc)
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
        
        private void Register()
        {
            if (UpdaterRegistry.IsUpdaterRegistered(updaterId)) Unregister();

            UpdaterRegistry.RegisterUpdater(this);
        }
        private void RegisterTriggers()
        {
            if (updaterId == null) return;
            if (!UpdaterRegistry.IsUpdaterRegistered(updaterId)) return;
            UpdaterRegistry.RemoveAllTriggers(updaterId);
            ElementFilter elementFilter = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);
            UpdaterRegistry.AddTrigger(updaterId, elementFilter, Element.GetChangeTypeGeometry());
            UpdaterRegistry.AddTrigger(updaterId, elementFilter, Element.GetChangeTypeElementAddition());
        }
        public void Unregister()
        {
            UpdaterRegistry.RemoveAllTriggers(updaterId);
            UpdaterRegistry.UnregisterUpdater(updaterId);
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


        private void UpdatePipeEndPrep(Document doc, Pipe pipe)
        {
            const double largestTapDistance = 0.5; //6"

            if (pipe == null) return;
            if (pipeEndPrepDefinition == null) return;
            Parameter endPrepParameter = pipe.get_Parameter(pipeEndPrepDefinition);
            if (endPrepParameter == null) return;
            Connector[] connectors = GetConnectors(pipe);
            string currentEndPrep = endPrepParameter.AsString();
            EndPrepInfo endPrepA = EndPrepInfo.GetEndPrepByConnector(connectors[0]);
            EndPrepInfo endPrepB = EndPrepInfo.GetEndPrepByConnector(connectors[1]);
            Curve[] intersectionSegments = GetIntersectionsBySolids(doc, pipe);
            XYZ[] wallCenters = intersectionSegments.Select(curve => (curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2).ToArray();
            string wallCollarString = "";
            Array.ForEach(intersectionSegments, curves => wallCollarString += " x WC ");
            double closestDistA = GetShortestDistanceToWalls(endPrepA.position, intersectionSegments);
            double closestDistB = GetShortestDistanceToWalls(endPrepB.position, intersectionSegments);
            endPrepA.isTapped = (closestDistA < largestTapDistance) && (endPrepA.endType.Equals(BellOrSpigot.BELL));
            endPrepA.isTapped = (closestDistB < largestTapDistance) && (endPrepB.endType.Equals(BellOrSpigot.BELL));
            bool reorder = CheckIfReorder(endPrepA, endPrepB, closestDistA, closestDistB);
            if (reorder)
            {
                EndPrepInfo tmp = endPrepB;
                endPrepB = endPrepA;
                endPrepA = tmp;
                double tmpDistance = closestDistB;
                closestDistB = closestDistA;
                closestDistA = tmpDistance;
            }
            double[] distancesFromPrepA = wallCenters.Select(wallCenter => endPrepA.position.DistanceTo(wallCenter)).ToArray();
            
            string tappedStringA =  endPrepA.isTapped ? "T" : "";
            string tappedStringB = endPrepB.isTapped ? "T" : "";
            string comments = "";


            Units units = new Units(UnitSystem.Imperial) ;
            FormatValueOptions formatValueOptions = new FormatValueOptions();
            //Array.ForEach(distancesFromPrepA,distance => comments += "w/ WC " + UnitFormatUtils.Format(units,SpecTypeId.Length,distance, false) + " FROM " + tappedStringA + endPrepA.endPrep + "; ");
            Array.ForEach(distancesFromPrepA, distance => comments += "w/ WC " + new Utils.UnitUtils.FeetAndInchesFraction(distance, 16).ToString() + " FROM " + tappedStringA + endPrepA.endPrep + "; ");

            string combinedEndPrep = tappedStringA + endPrepA.endPrep + wallCollarString + " x " + tappedStringB + endPrepB.endPrep;
            Parameter commentsParameter = pipe.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            
            if (combinedEndPrep.Equals(currentEndPrep) && (commentsParameter.AsString() == comments)) return;
            endPrepParameter.Set(combinedEndPrep);
            commentsParameter.Set(comments);
        }
        private double GetShortestDistanceToWalls(XYZ endPoint, Curve[] curves)
        {
            double shortestDistance = double.MaxValue;
            foreach (Curve curve in curves)
            {
                for (int i = 0; i < 2;  i++)
                {
                    shortestDistance = Math.Min(shortestDistance, endPoint.DistanceTo(curve.GetEndPoint(i)));
                    
                }
            }
            return shortestDistance;
        }



        

        public void Execute(UpdaterData data)
        {
            try
            {
                Document doc = data.GetDocument();
                if (pipeEndPrepDefinition == null)
                    if (!SetDefinitions(doc)) return;
                List<ElementId> elementIds = data.GetAddedElementIds().ToList();
                elementIds.AddRange(data.GetModifiedElementIds());
                Pipe[] pipes = elementIds.Select(id => doc.GetElement(id)).Cast<Pipe>().ToArray();
                Array.ForEach(pipes, pipe => UpdatePipeEndPrep(doc, pipe));
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Pipe End Prep By Connectors - ERROR", ex.Message);
            }
            
        }

        public string GetAdditionalInformation()
        {
            return "NA";
        }

        public ChangePriority GetChangePriority()
        {
            return ChangePriority.Connections;
        }

        public UpdaterId GetUpdaterId()
        {
            return updaterId;
        }

        public string GetUpdaterName()
        {
            return "Piping Connections Updater By Connector";
        }
    }
}
