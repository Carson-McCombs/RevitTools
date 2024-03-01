using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        //Below is an attempt at dimensioning Pipe Lines by retrieving the Geometry Objects of each piping element and compares their direction and endpoint positions to the connectors of their respective elements. Due to the current issues stated above, fixing the below function is put on pause.
        public void CreateDimensionLinesFromReferences(Document doc, double offset) //can only be called after GetPipeLine is called
        {
            if (elements == null) return;
            string names = "";
            foreach (Element elem in elements)
            {
                names = names + elem.Id.ToString() + '\n';  
            }
            //TaskDialog.Show("ELEM NAMES", names);
            string log = "";
            List<Reference[]> dimensionReferences = new List<Reference[]>();
            for (int i = 0; i < elements.Count; i++)
            {
                Element elem = elements[i];
                Line line = null;
                int flag = 0;
                //Reference[] references = null;  
                ReferenceArray referenceArray = new ReferenceArray();
                Reference tmp = null;
                try
                {
                    if (Util.IsPipe(elem))
                    {
                        flag = 20;
                        PlanarFace[] faces = Util.GetGeometryObjectsOfPipe(doc.ActiveView, elem);
                        List<XYZ> points = new List<XYZ>();
                        foreach (PlanarFace face in faces)
                        {
                            points.Add(face.Origin);
                            referenceArray.Append(face.Reference);
                        }
                        //line = Line.CreateBound(points[0], points[1]);
                        Line originalLine = Line.CreateBound(points[0], points[1]);//(elem.Location as LocationCurve).Curve as Line;
                        line = originalLine.CreateOffset(offset, points[0].CrossProduct(points[1])) as Line;

                        //line = Util.GetGeometryLineOfPipe(doc.ActiveView, elem);
                        //references = Util.GetEndPointReferences(line);
                        //foreach (Reference reference in references)
                        //{
                        //    referenceArray.Append(reference);
                        //}
                        //dimensionReferences.Add(references);
                        doc.Create.NewDimension(doc.ActiveView, line, referenceArray);

                    }

                    else if (Util.IsPipeBend(elements[i]))
                    {
                        flag = 10;
                        Element connected = (i == 0) ? elements[1] : elements[elements.Count - 2];
                        //XYZ direction = Line.CreateUnbound((elements[0].Location as LocationPoint).Point, (elements[elements.Count - 1].Location as LocationPoint).Point).Direction; //could cause issues depending on view orientation in relation to bend direction
                        XYZ direction = Util.GetDirectionOfConnection(elem as FamilyInstance, connected as FamilyInstance);
                        Line[] lines = Util.GetGeometryLinesOfBend(doc.ActiveView, elem);
                        //line = (Util.IsDirectionCloserThan(lines[0].Direction, lines[1].Direction, direction)) ? lines[0] : lines[1];
                        //line = (lines[0].Direction.IsAlmostEqualTo(direction) || lines[0].Direction.IsAlmostEqualTo(-direction)) ? lines[0] : lines[1];
                        //XYZ otherDirection = (Util.IsDirectionCloserThan(lines[0].Direction, lines[1].Direction, roughDirection.Direction)) ? lines[0].Direction: lines[1].Direction;
                        referenceArray.Append(line.GetEndPointReference(0));
                        referenceArray.Append(line.GetEndPointReference(1));





                        //line = (Util.IsDirectionCloserThan(lines[0].Direction, lines[1].Direction, roughDirection.Direction)) ? lines[0] : lines[1];
                        //Line lineB = (Util.IsDirectionCloserThan(lines[0].Direction, lines[1].Direction, roughDirection.Direction)) ? lines[1] : lines[0];
                        //PlanarFace planarFace = Util.GetPlanarFaceOfBend(doc.ActiveView, elem, line);
                        Line parallelLine = line.CreateOffset(offset, XYZ.Zero) as Line;
                        //Line parallelLine = Util.GetParallelLine(line.GetEndPoint(0), line.GetEndPoint(1), offset);
                        //Line parallelLine = line.CreateOffset(offset, doc.ActiveView.RightDirection) as Line;
                        //XYZ center = (elem.Location as LocationPoint).Point;
                        //Reference centerReference = line.GetEndPoint(0).DistanceTo(center) <= line.GetEndPoint(1).DistanceTo(center) ? line.GetEndPointReference(0) : line.GetEndPointReference(1);
                        //tmp = centerReference;
                        //referenceArray.Append(planarFace.Reference);
                        //referenceArray.Append(centerReference);
                        //dimensionReferences.Add(references);
                        Dimension dim = doc.Create.NewDimension(doc.ActiveView, parallelLine, referenceArray);
                        if (dim == null) continue;
                        //dim.Location.Move(new XYZ(1, 1, 1));
                       // dim.Location.Move(parallelLine.Origin - dim.Origin);
                    }
                    else continue;
                }
                catch (Exception ex)
                { 
                    TaskDialog.Show("ERR " + flag.ToString(), log + "\n\n" + ex.Message); 
                }

                
            }

        }
        
        #endregion



    }
}
