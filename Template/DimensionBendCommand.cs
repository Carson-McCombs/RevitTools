using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class DimensionBendCommand : IExternalCommand, ISettingsComponent
    {
        public const bool IsWIP = true;

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("DimensionBendCommand", "Dimension Bend (WIP)", assembly.Location, "CarsonsAddins.DimensionBendCommand");
            pushButtonData.ToolTip = "Dimensions selected bend.";
            return pushButtonData;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application);
        }
        public Result Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Question Mark Dimensions Command", "Command should not be used within a family document.");
                return Result.Failed;
            }
            Transaction transaction = new Transaction(doc);
            string s = "";
            try
            {
                double offset = 4;
                transaction.Start("DimensionBendCommand");
                Reference elementReference = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new SelectionFilter_PipingElements(false, false, false, true, false), "Please Pick a Bend, Tee, Wye, or Cross.");
                Element element = doc.GetElement(elementReference);
                //Dictionary<XYZ, (PlanarFace,Line)> dimensionDictionaryByDirection = new Dictionary<XYZ, (PlanarFace, Line)>();
                //PlanarFace[] faces = Util.GetPlanarFacesOfBend(doc.ActiveView, element);
                List<(PlanarFace,PlanarFace)> faces = new List<(PlanarFace, PlanarFace)>();
                //Line[] lines = Util.GetGeometryLineOfAdjacentFittingsOnBend(doc.ActiveView, element as FamilyInstance);
                Connector[] connectors = Util.GetConnectors(element as FamilyInstance).ToArray();

                Element[] connectedElements = Util.GetConnectedElements(connectors);
                List<Line> lines = new List<Line>();
                List<XYZ> directions = new List<XYZ>();
                List<XYZ> uniqueDirections = new List<XYZ>();
                for(int i = 0; i < connectedElements.Length; i++)
                {
                    Connector connected = Util.TryGetConnected(connectors[i]);
                    Connector adj = Util.GetAdjacentConnector(connected);
                    if (!adj.IsConnected) continue;
                    Connector other = Util.TryGetConnected(adj);
                    if (other == null) continue;
                    if (other.Owner == null) continue;
                    if (!other.Owner.IsValidObject) continue;
                    if (!Util.IsPipe(other.Owner)) continue;
                    lines.Add(Util.GetGeometryLineOfPipe(doc.ActiveView, other.Owner));
                    //lines.Add(Util.GetGeometryLinesOfBend(doc.ActiveView, connectedElements[i])[0]);
                    FamilyInstance fitting = connectedElements[i] as FamilyInstance;
                    //PlanarFace[] instanceFaces = Util.GetGeometryObjectFromInstanceGeometry<PlanarFace>(doc.ActiveView, connectedElements[i]);
                    //PlanarFace[] fittingFaces = Util.GetPlanarFacesOfBend(doc.ActiveView, connectedElements[i]);
                    
                    faces.Add(Util.GetPlanarFaceFromConnector(doc.ActiveView, Util.TryGetConnected(connectors[i])));
                    directions.Add(faces.Last().Item1.FaceNormal);
                }
                //foreach(PlanarFace face in faces) 
                //{
                //    XYZ faceNormal = face.FaceNormal;
                //    for(int i = 0; i < connectedElements.Length; i++)
                //    {
                //        FamilyInstance familyInstance = connectedElements[i] as FamilyInstance;
                        
                //        if (!faceNormal.IsAlmostEqualTo(familyInstance.HandOrientation)) 
                //        {
                //            dimensionDictionaryByDirection.Add(familyInstance.HandOrientation, (face, lines[i]));
                //            break;
                //        }
                //    }
                    
                //}
                for (int i = 0; i < lines.Count; i++)
                {
                    SubTransaction subtransaction = new SubTransaction(doc);
                    subtransaction.Start();
                    try
                    {
                        ReferenceArray rf = new ReferenceArray();
                        XYZ pointA = new XYZ();
                        XYZ pointB = (element.Location as LocationPoint).Point;
                        for (int j = 0; j < faces.Count; j++)
                        {
                            if (i == j) continue;
                            if (directions[i].IsAlmostEqualTo(directions[j]) || directions[i].IsAlmostEqualTo(-directions[j])) continue;
                            
                            if (rf.IsEmpty) 
                            {
                                rf.Append(faces[j].Item1.Reference);
                                rf.Append(lines[i].Reference);
                                pointA = faces[j].Item2.Origin;
                            }
                            else
                            {
                                rf.Append(faces[j].Item1.Reference);
                                pointB = faces[j].Item2.Origin;
                            }
                        }
                        
                        Line line = Line.CreateBound(pointA, pointB).CreateOffset(offset, XYZ.Zero) as Line;
                        Dimension dim = doc.Create.NewDimension(doc.ActiveView, line, rf);
                        if (dim == null) continue;
                        s = s + "( " + i.ToString() + " ): " + dim.Id.ToString() + "\n";
                        subtransaction.Commit();
                    }
                    catch (Exception e)
                    {
                        subtransaction.RollBack();
                        TaskDialog.Show("DIMENSION ERROR", e.Message);
                    }
                }
                TaskDialog.Show("DIMENSIONS CHECK", s);
                transaction.Commit();
                return Result.Succeeded;
            }
                //    foreach((PlanarFace, Line) value in dimensionDictionaryByDirection.Values)
                //    {
                //        SubTransaction subtransaction = new SubTransaction(doc);
                //        subtransaction.Start();
                //        try
                //        {
                //            ReferenceArray rf = new ReferenceArray();
                //            rf.Append(value.Item1.Reference);
                //            rf.Append(value.Item2.Reference);
                //            Line line = Line.CreateBound((element.Location as LocationPoint).Point, value.Item2.Origin).CreateOffset(offset, XYZ.Zero) as Line;
                //            Dimension dim = doc.Create.NewDimension(doc.ActiveView, line, rf);
                //            if (dim == null) continue;
                //            s = s + dim.Id.ToString() + "\n";
                //            subtransaction.Commit();
                //        }
                //        catch(Exception e)
                //        {
                //            subtransaction.RollBack();
                //            TaskDialog.Show("DIMENSION ERROR", e.Message);
                //        }

                //    }
                //    TaskDialog.Show("NEW DIMENSIONS " + lines.Count + " - " + dimensionDictionaryByDirection.Values.Count.ToString(), s);
                //    transaction.Commit();
                //    return Result.Succeeded;

                //}
            catch (Exception ex)
            {
                TaskDialog.Show("DIMENSION BEND ERROR", s + " \n\n " + ex.Message);
                transaction.RollBack();
                return Result.Failed;
            }


        }

    }
}
