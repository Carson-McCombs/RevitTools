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
        private Pipe selectedPipe;

        public List<Element> GetPipeLine(UIDocument uidoc, Pipe pipe)
        {
            elements = new List<Element>();
            filter = new SelectionFilter_PipingElements(true, true, false, true, true);
            List<Connector> connectors = Util.GetConnectors(pipe);
            selectedPipe = pipe;
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

        public void CreateDimensionLines(Document doc, double offset)
        {
            Line locationLine = (selectedPipe.Location as LocationCurve).Curve as Line;
            Line line = locationLine.CreateOffset(offset, locationLine.GetEndPoint(0).CrossProduct(locationLine.GetEndPoint(1))) as Line;
            line.MakeUnbound();
            ReferenceArray referenceArray = new ReferenceArray();
            
            ConnectorElement el;
            
        }
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

        private bool IsCloserAngleThan(XYZ directionA, XYZ directionB, XYZ sourceDirection)
        {
            if (sourceDirection.IsAlmostEqualTo(directionA) || sourceDirection.IsAlmostEqualTo(-directionA)) return true;
            if (sourceDirection.IsAlmostEqualTo(directionB) || sourceDirection.IsAlmostEqualTo(-directionB)) return false;
            double angleA = directionA.AngleTo(sourceDirection);
            double angleB = directionB.AngleTo(sourceDirection);
            angleA = (angleA > Math.PI / 2)? angleA - Math.PI: angleA;
            angleB = (angleB > Math.PI / 2) ? angleB - Math.PI : angleB;
            return (angleA < angleB);

        }

        private XYZ GetTransformedDirection(Transform transform, XYZ connectionDirection)
        {
            connectionDirection = connectionDirection.Normalize();
            XYZ transformedDirection = connectionDirection.X * transform.BasisX + connectionDirection.Y * transform.BasisY + connectionDirection.Z * transform.BasisZ;
            transformedDirection = transformedDirection.Normalize();
            return transformedDirection;
        }
        private Line ChooseGeometryLineByDirection(XYZ direction, Line[] lines)
        {
            if (direction == null) return null;
            if (lines == null) return null;
            Line negative = null;
            
            foreach (Line line in lines)
            {
                if (direction.IsAlmostEqualTo(line.Direction)) return line;
                else if (direction.IsAlmostEqualTo(-line.Direction)) negative = line;

            }
            return negative;
        }
        private Line ChooseGeometryLineByConnectorPosition(XYZ connectorPosition, Line[] lines)
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

            private Line GetLineWithId(int id, Line[] lines)
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

        //Below is an attempt at dimensioning Pipe Lines by retrieving the Geometry Objects of each piping element and compares their direction and endpoint positions to the connectors of their respective elements. Due to the current issues stated above, fixing the below function is put on pause.
        public void CreateDimensionLinesFromReferencesOld(Document doc, double offset) //can only be called after GetPipeLine is called
        {
            if (elements == null) return;
            string names = "";
            ElementId[] validStyleIds = GetCenterlineIds(doc);

            foreach (Element elem in elements)
            {
                names = names + elem.Id.ToString() + '\n';  
            }
            string log = "";
            Plane plane = Plane.CreateByNormalAndOrigin(doc.ActiveView.ViewDirection, doc.ActiveView.Origin);
            for (int i = 0; i < elements.Count; i++)
            {
                Element elem = elements[i];
                try
                {
                    if (Util.IsPipe(elem))
                    {
                        DimensionPipeByPlanarFaces(doc, plane, offset, elem);
                    }

                    else if (Util.IsPipeBend(elem))
                    {
                        DimensionPipeBendByGeometryLines(doc, validStyleIds, plane, offset, i, elem);
                    }
                    else continue;
                }
                catch (Exception ex)
                { 
                    TaskDialog.Show("ERROR DIMENSIONING PIPELINE ", log + "\n\n" + ex.Message); 
                }

                
            }

        }

        private static Line DimensionPipeByPlanarFaces(Document doc, Plane plane, double offset, Element elem)
        {
            ReferenceArray referenceArray = new ReferenceArray();
            Line line;
            PlanarFace[] faces = Util.GetGeometryObjectsOfPipe(doc.ActiveView, elem);
            List<XYZ> points = new List<XYZ>();
            foreach (PlanarFace face in faces)
            {
                points.Add(face.Origin);
                referenceArray.Append(face.Reference);
            }

            XYZ projectedPointA = ProjectPointOntoPlane(plane, points[0]);
            XYZ projectedPointB = ProjectPointOntoPlane(plane, points[1]);
            Line originalLine = Line.CreateBound(projectedPointA, projectedPointB);
            line = originalLine.CreateOffset(offset, projectedPointA.CrossProduct(projectedPointB)) as Line;
            doc.Create.NewDimension(doc.ActiveView, line, referenceArray);
            return line;
        }
        
        private Dimension DimensionPipeBendByPlanarFaces(Document doc, ElementId[] validStyleIds, Plane plane, int i, Element elem)
        {
            FamilyInstance familyInstance = elem as FamilyInstance;
            FamilyInstance connected = (i == 0) ? elements[1] as FamilyInstance : elements[elements.Count - 2] as FamilyInstance;
            PlanarFace[] faces = elem.get_Geometry(Util.GetGeometryOptions()).OfType<PlanarFace>().ToArray();
            Connector connector = Util.TryGetConnection(familyInstance, connected);
            PlanarFace connectorFace = faces.Where(face => face.Origin.Equals(connector.Origin)).FirstOrDefault();
            return null;
        }
        private Dimension DimensionPipeBendByGeometryLines(Document doc, ElementId[] validStyleIds, Plane plane, double offset, int i, Element elem)
        {
            FamilyInstance familyInstance = elem as FamilyInstance;
            FamilyInstance connected = (i == 0) ? elements[1] as FamilyInstance : elements[elements.Count - 2] as FamilyInstance;
            Line[] symbolLines = Util.GetSymbolGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), elem, validStyleIds);
            Line[] instanceLines = Util.GetInstanceGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), elem, validStyleIds);

            Connector connector = Util.TryGetConnection(familyInstance, connected);
            Line instanceLine = ChooseGeometryLineByConnectorPosition(connector.Origin, instanceLines);
            Line symbolLine = GetLineWithId(instanceLine.Id, symbolLines);
            ReferenceArray referenceArray = new ReferenceArray();
            referenceArray.Append(symbolLine.GetEndPointReference(0));
            referenceArray.Append(symbolLine.GetEndPointReference(1));



            XYZ projectedPointA = ProjectPointOntoPlane(plane, instanceLine.GetEndPoint(0));
            XYZ projectedPointB = ProjectPointOntoPlane(plane, instanceLine.GetEndPoint(1));

            Line projectedLine = Line.CreateBound(projectedPointA, projectedPointB);
            XYZ perpVector = projectedPointA.CrossProduct(projectedPointB); //projectedLine.Direction.CrossProduct(plane.Normal);
            Line offsetLine = projectedLine.CreateOffset(offset, perpVector) as Line;
            return doc.Create.NewDimension(doc.ActiveView, offsetLine, referenceArray);
        }

        private Dimension DimensionPipeBendByLineDirection(Document doc, ElementId[] validStyleIds, Plane plane, int i, Element elem)
        {
            FamilyInstance familyInstance = elem as FamilyInstance;
            FamilyInstance connected = (i == 0) ? elements[1] as FamilyInstance : elements[elements.Count - 2] as FamilyInstance;
            XYZ direction = Util.GetDirectionOfConnection(familyInstance, connected);

            Line[] symbolLines = Util.GetSymbolGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(doc.ActiveView), elem, validStyleIds);
            Line[] instanceLines = Util.GetInstanceGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(doc.ActiveView), elem, validStyleIds);


            Line instanceLine = ChooseGeometryLineByDirection(direction, instanceLines);
            Line symbolLine = GetLineWithId(instanceLine.Id, symbolLines);
            ReferenceArray referenceArray = new ReferenceArray();
            referenceArray.Append(symbolLine.GetEndPointReference(0));
            referenceArray.Append(symbolLine.GetEndPointReference(1));



            XYZ projectedPointA = ProjectPointOntoPlane(plane, instanceLine.GetEndPoint(0));
            XYZ projectedPointB = ProjectPointOntoPlane(plane, instanceLine.GetEndPoint(1));

            Line projectedLine = Line.CreateBound(projectedPointA, projectedPointB);
            XYZ perpVector = projectedLine.Direction.CrossProduct(plane.Normal);
            Line offsetLine = projectedLine.CreateOffset(0, perpVector) as Line;

           return doc.Create.NewDimension(doc.ActiveView, offsetLine, referenceArray);
        }

        #endregion



    }
}
