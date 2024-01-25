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
    public class PipeLine
    {
        private ISelectionFilter filter = null;
        private List<Element> elements;

        public List<Element> GetPipeLine(UIDocument uidoc, Pipe pipe)
        {
            elements = new List<Element>();
            filter = null;
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

        private void AddNextElement_Left(UIDocument uidoc, Connector current)
        {
            Connector adjacent = Util.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = Util.TryGetConnected(adjacent);
            if (CanContinue(next)) AddNextElement_Left(uidoc, next);
            if (adjacent.IsConnected) elements.Add(next.Owner);



        }
        private void AddNextElement_Right(UIDocument uidoc, Connector current)
        {
            Connector adjacent = Util.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = Util.TryGetConnected(adjacent);
            if (adjacent.IsConnected) elements.Add(next.Owner);

            if (CanContinue(next)) AddNextElement_Right(uidoc, next);
            
        }
        private bool CanContinue(Connector next)
        {
            if (next == null) return false;
            if (next.Owner == null) return false;
            if (next.Id.Equals(ElementId.InvalidElementId)) return false;
            if (!next.IsValidObject) return false;
            if (!filter.AllowElement(next.Owner)) return false;
            return true;
        }
        
        //public void CreateDimensionLines(Document doc, double offset)
        //{
        //    List<XYZ> dimensionPoints = new List<XYZ>();
        //    foreach (Element elem in elements)
        //    {
        //        //XYZ[] points = Util.GetDimensionPoints(elem);
        //        if (points == null) continue;
        //        foreach (XYZ point in points)
        //        {
        //            dimensionPoints.Add(point);

        //        }
        //    }
        //    if (dimensionPoints.Count == 0) return;

        //    int start = 0;
        //    int end = dimensionPoints.Count;
        //    if (!Util.IsPipeBend(elements[0]))
        //    {
        //        start++;
        //    }
        //    if (!Util.IsPipeBend(elements[elements.Count - 1]))
        //    {
        //        end--;
        //    }
        //    //List<Line> dimensionLines = new List<Line>();
        //    //string s = "";
        //    //List<DetailCurve> curves = new List<DetailCurve>();
        //    string s = "";
        //    string err = "";
        //    for (int i = start; i < end - 1; i += 2)
        //    {
        //        SubTransaction trans = new SubTransaction(doc);

        //        trans.Start();
        //        try
        //        {
        //            Connector con;

        //            CreateDimension(doc, dimensionPoints[i], dimensionPoints[i + 1], offset);
        //            trans.Commit();
        //        }

        //        catch (Exception ex)
        //        {
        //            s = s + dimensionPoints[i] + " connecting to " + dimensionPoints[i + 1] + " \n";
        //            err = ex.Message;
        //            trans.RollBack();

        //        }

        //    }
        //    TaskDialog.Show("ERROR CREATING DIMENSIONS", s + "\n\n" + err);


        //}
        public void CreateDimensionLinesFromReferences(Document doc, double offset) //can only be called after GetPipeLine is called
        {
            if (elements == null) return;
            string names = "";
            foreach (Element elem in elements)
            {
                names = names + elem.Id.ToString() + '\n';  
            }
            TaskDialog.Show("ELEM NAMES", names);
            string log = "";
            List<Reference[]> dimensionReferences = new List<Reference[]>();
            for (int i = 0; i < elements.Count; i++)
            {
                Element elem = elements[i];
                Line line = null;
                int flag = 0;
                Reference[] references = null;  
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
                        line = (Util.IsDirectionCloserThan(lines[0].Direction, lines[1].Direction, direction)) ? lines[0] : lines[1];
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

        
        
            //private void CreateDimension(Document doc, XYZ pointA, XYZ pointB, double offset)
            //{
            //    //Line line = Line.CreateBound(dimensionPoints[i], dimensionPoints[i + 1]);
            //    //s = s + dimensionPoints[i].ToString() + '\n';
            //    View activeView = doc.ActiveView;
            //    //Plane.CreateByOriginAndBasis(activeView.Origin, activeView.ViewDirection);
            //    //activeView.SketchPlane.GetPlane().projec
            //    //doc.ActiveView.SketchPlane = SketchPlane.Create(doc, SketchPLan
            //    //doc.ActiveView.ViewDirection,
            //   // doc.ActiveView.Origin);
            //    DetailCurve detailCurve = doc.Create.NewDetailCurve(doc.ActiveView, Util.GetParallelLine(pointA, pointB, offset));
            //    Line line = detailCurve.GeometryCurve as Line;
            //    DetailCurve dcurveA = doc.Create.NewDetailCurve(doc.ActiveView, Line.CreateBound(pointA, line.GetEndPoint(0)));
            //    DetailCurve dcurveB = doc.Create.NewDetailCurve(doc.ActiveView, Line.CreateBound(pointB, line.GetEndPoint(1)));
            //    ReferenceArray refArray = new ReferenceArray();
            //    refArray.Append(dcurveA.GeometryCurve.Reference);
            //    refArray.Append(dcurveB.GeometryCurve.Reference);
            //    doc.Create.NewDimension()
            //    doc.Create.NewDimension(doc.ActiveView, line, refArray);
            //}

        }
}
