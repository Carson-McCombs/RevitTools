using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace CarsonsAddins
{

    /*
     * I am defining a Pipe Line as all of the connected piping elements ( i.e. Pipes, Pipe Fittings, and Pipe Accessories ) between either a Pipe Junction ( i.e. a Bend, Tee, Wye, Cross, etc. ) or end of pipe ( an empty connection ).
     * 
     * This is meant to be a base class that will be called for actions such as movement and dimensioning of piping elements.
     *
     * Note: Currently dimensioning a any junction that is not a bend in Revit does not work. Lateral Tees and Crosses seem to work for dimensioning but not for Routing Preferences. 
     *  Getting the programmatic dimensioning to work is put on delay until either the problem is solved within my current Revit setup or by Revit themselves.
     */
    public class PipeLine
    {
        private ISelectionFilter filter = null;
        private List<Element> elements;
        public List<Element> GetPipeLine(UIDocument uidoc, Pipe pipe)
        {
            elements = new List<Element>();
            filter = new SelectionFilter_PipingElements(true, true, false, true, true);
            List<Connector> connectors = Util.GetConnectors(pipe);
            AddNextElement_Left(uidoc, connectors[0]);
            elements.Add(pipe);
            AddNextElement_Right(uidoc, connectors[1]);
            
            return elements;
        }
        public List<Element> GetPipeLine(UIDocument uidoc, Pipe pipe, ISelectionFilter filter)
        {
            elements = new List<Element>();
            this.filter = filter;
            List<Connector> connectors = Util.GetConnectors(pipe);
            AddNextElement_Left(uidoc, connectors[0]);
            elements.Add(pipe);
            AddNextElement_Right(uidoc, connectors[1]);
            return elements;
        }

        //Recursively adds all the connected piping elements on one side (arbitrarly called the "left" side) of an element
        private void AddNextElement_Left(UIDocument uidoc, Connector current)
        {
            Connector adjacent = Util.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = Util.TryGetConnected(adjacent);
            if (CanContinue(next)) AddNextElement_Left(uidoc, next);
            if (adjacent.IsConnected) elements.Add(next.Owner);
        }

        //Recursively adds all the connected piping elements on one side (arbitrarly called the "right" side) of an element
        private void AddNextElement_Right(UIDocument uidoc, Connector current)
        {
            Connector adjacent = Util.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = Util.TryGetConnected(adjacent);
            if (adjacent.IsConnected) elements.Add(next.Owner);

            if (CanContinue(next)) AddNextElement_Right(uidoc, next);
            
        }

        //Checks if the Pipe Line extends to the next element, or if the current element is the last one in the Pipe Line.
        private bool CanContinue(Connector next)
        {
            if (next == null) return false;
            if (next.Owner == null) return false;
            if (next.Id.Equals(ElementId.InvalidElementId)) return false;
            if (!next.IsValidObject) return false;
            if (!filter.AllowElement(next.Owner)) return false;
            return true;
        }

        #region WIP

        private ElementId[] GetCenterlineIds(Document doc)
        {
            
            List<Element> centerlineStyleElements = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)).ToList();
            List<ElementId> centerlineStyleIds = new List<ElementId>();
            for (int i = 0; i < centerlineStyleElements.Count; i++)
            {
                if (centerlineStyleElements[i].Name.Equals("zLines") || (centerlineStyleElements[i].Name.Equals("Center line"))) centerlineStyleIds.Add(centerlineStyleElements[i].Id);
            }
            return centerlineStyleIds.ToArray();
        }

        /// <summary>
        /// Projects a 3D point onto a plane. Meant to be used for projecting dimensioning reference points onto the active view's plane Made by @jeremytammik on https://thebuildingcoder.typepad.com/blog/2014/09/planes-projections-and-picking-points.html
        /// </summary>
        /// <param name="plane">Plane to be projected onto.</param>
        /// <param name="point">Point not on plane.</param>
        /// <returns>Projected point on plane.</returns>
        private static XYZ ProjectPointOntoPlane(Plane plane, XYZ point)
        {
            double distance = plane.Normal.DotProduct(point);
            return point - distance * plane.Normal;
        }

        private static XYZ GetOriginOfElement(Element element)
        {
            if (element == null) return null;
            Location location = element.Location;
            if (location is LocationPoint locationPoint) return locationPoint.Point;
            if (location is LocationCurve locationCurve) return (locationCurve.Curve as Line).Origin;
            return null;
        }

        private static Line ChooseGeometryLineByConnectorPosition(XYZ connectorPosition, Line[] lines)
        {
            if (lines == null) return null;

            foreach (Line line in lines)
            {
                XYZ endpointA = line.GetEndPoint(0);
                XYZ endpointB = line.GetEndPoint(1);
                if (connectorPosition.IsAlmostEqualTo(endpointA) || connectorPosition.IsAlmostEqualTo(endpointB)) return line;
            }
            return null;
        }

        private static Line GetLineWithId(int id, Line[] lines)
        {
            foreach (Line line in lines)
            {
                if (line.Id.Equals(id))
                {
                    return line;
                }
            }
            return null;
        }
        private static Line CreateDimensionLine(Plane plane, Line elementLine, XYZ dimensionPoint)
        {
            Line dimensionLine = Line.CreateUnbound(ProjectPointOntoPlane(plane, dimensionPoint), elementLine.Direction);
            dimensionLine.MakeUnbound();
            return dimensionLine;
        }
        private static XYZ Lerp(XYZ start, XYZ end, double amount)
        {
            return start + (end - start) * amount;
        }
        private static Reference GetCenterReference(ElementId[] validStyleIds, Element element)
        {
            Line[] instanceLines = Util.GetInstanceGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), element, validStyleIds);
            Line[] symbolLines = Util.GetSymbolGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), element, validStyleIds);
            XYZ origin = GetOriginOfElement(element);
            int id = -1;
            int endIndex = -1;
            foreach(Line line in instanceLines)
            {
                for(int i = 0; i < 2; i++)
                {
                    if (line.GetEndPoint(i).IsAlmostEqualTo(origin))
                    {
                        id = line.Id;
                        endIndex = i;
                        break;
                    }
                }
                if (id != -1) break;
            }
            
            return symbolLines.Where(line => line.Id.Equals(id)).FirstOrDefault().GetEndPointReference(endIndex);
        }
        private static Reference GetPipeEndReference(Element element)
        {
            Pipe pipe = element as Pipe;
            if (pipe == null) return null;
            XYZ position = null;
            foreach (Connector connector in pipe.ConnectorManager.UnusedConnectors)
            {
                position = connector.Origin;
                break;
            }
            if (position == null) return null;
            PlanarFace face = element.get_Geometry(Util.GetGeometryOptions()).OfType<PlanarFace>().Where(f => f.Origin.IsAlmostEqualTo(position)).FirstOrDefault();
            return face.Reference;
            
        }
        private static Line CreateSecondaryDimensionLine(View view, DimensionType secondaryDimensionType, Line elementLine, Line primaryDimensionLine)
        {
            double textSize = secondaryDimensionType.get_Parameter(BuiltInParameter.TEXT_SIZE).AsDouble();
            double textOffset = secondaryDimensionType.get_Parameter(BuiltInParameter.TEXT_DIST_TO_LINE).AsDouble();

            double offset = (textSize + 3 * textOffset) * view.Scale;
            
            IntersectionResult result = primaryDimensionLine.Project(elementLine.Origin);
            double percent = offset / result.Distance;
            return Line.CreateUnbound(Lerp(result.XYZPoint, elementLine.Origin, percent), primaryDimensionLine.Direction);
        }

        private static Reference GetEndReference(ElementId[] validStyleIds, Element element)
        {
            if (Util.IsPipe(element)) return GetPipeEndReference(element);
            return GetCenterReference(validStyleIds, element);
        }

        //Below is an attempt at dimensioning Pipe Lines by retrieving the Geometry Objects of each piping element and compares their direction and endpoint positions to the connectors of their respective elements. Due to the current issues stated above, fixing the below function is put on pause.
        public void CreateDimensionLinesFromReferences(Document doc, XYZ dimensionPoint, bool secondaryDimension) //can only be called after GetPipeLine is called
        {
            if (elements == null) return;
            
            ElementId[] validStyleIds = GetCenterlineIds(doc);
            Plane plane = Plane.CreateByNormalAndOrigin(doc.ActiveView.ViewDirection, doc.ActiveView.Origin);
            XYZ pointA = ProjectPointOntoPlane(plane, GetOriginOfElement(elements[0]));
            XYZ pointB = ProjectPointOntoPlane(plane, GetOriginOfElement(elements[1]));
            Line elementLine = Line.CreateBound(pointA, pointB);

            Line primaryDimensionLine = CreateDimensionLine(plane, elementLine, dimensionPoint);
            DimensionType primaryDimensionType = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Where(dt => dt.Name.Equals("C & B PIPE")).FirstOrDefault() as DimensionType;
            DimensionType secondaryPipeDimensionType = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Where(dt => dt.Name.Equals("C & B PIPE w/dot")).FirstOrDefault() as DimensionType;
            DimensionType secondaryFittingDimensionType = new FilteredElementCollector(doc).OfClass(typeof(DimensionType)).Where(dt => dt.Name.Equals("C & B FITTING w/dot")).FirstOrDefault() as DimensionType;
            Line secondaryDimensionLine = CreateSecondaryDimensionLine(doc.ActiveView, secondaryPipeDimensionType, elementLine, primaryDimensionLine);

            ReferenceArray primaryReferenceArray = new ReferenceArray();
            primaryReferenceArray.Append(GetEndReference(validStyleIds, elements[0]));
            primaryReferenceArray.Append(GetEndReference(validStyleIds, elements[elements.Count - 1]));


            Dimension primaryDimension = doc.Create.NewDimension(doc.ActiveView, primaryDimensionLine, primaryReferenceArray, primaryDimensionType );
            if (!secondaryDimension) return;
            for (int i = 0; i < elements.Count; i++)
            {
                Element element = elements[i];
                try
                {
                    if (Util.IsPipe(element))
                    {
                        DimensionPipeByPlanarFaces(doc, secondaryPipeDimensionType, secondaryDimensionLine, element);
                    }

                    else if (Util.IsPipeBend(element))
                    {
                        FamilyInstance connected = (i == 0) ? elements[1] as FamilyInstance : elements[elements.Count - 2] as FamilyInstance;
                        DimensionPipeBendByGeometryLines(doc, secondaryFittingDimensionType, validStyleIds,  secondaryDimensionLine, element as FamilyInstance, connected);
                    }
                    else continue;
                }
                catch (Exception ex)
                { 
                    TaskDialog.Show("ERROR DIMENSIONING PIPELINE ", ex.Message); 
                }

                
            }

        }

        private static Dimension DimensionPipeByPlanarFaces(Document doc, DimensionType dimensionType, Line dimensionLine, Element elem)
        {
            ReferenceArray referenceArray = new ReferenceArray();
            PlanarFace[] faces = Util.GetGeometryObjectsOfPipe(doc.ActiveView, elem);
            List<XYZ> points = new List<XYZ>();
            foreach (PlanarFace face in faces)
            {
                points.Add(face.Origin);
                referenceArray.Append(face.Reference);
            }
            
            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray);
        }

        private static Dimension DimensionPipeBendByGeometryLines(Document doc, DimensionType dimensionType, ElementId[] validStyleIds, Line dimensionLine, FamilyInstance familyInstance, FamilyInstance connected)
        {
            Line[] symbolLines = Util.GetSymbolGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), familyInstance, validStyleIds);
            Line[] instanceLines = Util.GetInstanceGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), familyInstance, validStyleIds);

            Connector connector = Util.TryGetConnection(familyInstance, connected);
            Line instanceLine = ChooseGeometryLineByConnectorPosition(connector.Origin, instanceLines);
            Line symbolLine = GetLineWithId(instanceLine.Id, symbolLines);

            ReferenceArray referenceArray = new ReferenceArray();
            referenceArray.Append(symbolLine.GetEndPointReference(0));
            referenceArray.Append(symbolLine.GetEndPointReference(1));

            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray);
        }

        #endregion



    }
}
