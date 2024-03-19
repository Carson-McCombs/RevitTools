using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace CarsonsAddins
{
    /// <summary>
    /// WIP ( until issue with dimensioning Junctions gets fixed ) - 
    /// Command that dimensions a group of Piping Elements within a "Pipe Line".
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class DimensionPipeLineCommand : IExternalCommand, ISettingsComponent
    {
        public const bool IsWIP = false;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("DimensionPipeLineCommand", "Dimensions Pipe Line", assembly.Location, "CarsonsAddins.DimensionPipeLineCommand");
            pushButtonData.ToolTip = "Gets the dimensions of all elements in pipe line.";
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
                TaskDialog.Show("Dimension Pipe Command", "Command should not be used within a family document.");
                return Result.Failed;
            }
            Transaction transaction = new Transaction(doc);
            transaction.Start("DimensionPipeLineCommand");
            try
            {
                //Reference elementReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilter_PipingElements(true, true, true, true), "Please select a Pipe, Flange, Bend, or Accessory");
                //Element elem = doc.GetElement(elementReference);
                //if (elem == null)
                //{
                //    transaction.RollBack();
                //    return Result.Cancelled;
                //}
                //string s = "";
                //XYZ[] points = Util.GetDimensionPoints(elem);
                //if (points == null)
                //{
                //    TaskDialog.Show("Get Dimension Points from Element", "Element Dimension Points are null");
                //    transaction.RollBack();
                //    return Result.Failed;
                //}
                //foreach (XYZ point in points)
                //{
                //    s = s + point.ToString() + "\n";
                //}
                //TaskDialog.Show("Get Dimension Points from Element",s);
                
                Reference pipeReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilter_Pipe(), "Please select a Pipe.");
                Pipe pipe = doc.GetElement(pipeReference.ElementId) as Pipe;
                if (pipe == null)
                {
                    transaction.RollBack();
                    TaskDialog.Show("DPL Error", "Pipe is null");
                    return Result.Cancelled;
                }
                PipeLine pipeLine = new PipeLine();
                pipeLine.GetPipeLine(uidoc, pipe);
                Plane plane = null;
                if (doc.ActiveView.SketchPlane == null)
                {
                    plane = Plane.CreateByNormalAndOrigin(doc.ActiveView.ViewDirection, doc.ActiveView.Origin);

                    SubTransaction sketchplaneTransaction = new SubTransaction(doc);
                    sketchplaneTransaction.Start();
                    SketchPlane sketchplane = SketchPlane.Create(doc, plane);
                    doc.ActiveView.SketchPlane = sketchplane;
                    doc.ActiveView.HideElements(new List<ElementId>(){sketchplane.Id});
                    sketchplaneTransaction.Commit();
                }
                else
                {
                    plane = doc.ActiveView.SketchPlane.GetPlane();
                }
                

                XYZ dimensionPoint = uidoc.Selection.PickPoint(ObjectSnapTypes.Perpendicular, "Please select where you would like the dimensions to be placed.");
                if (dimensionPoint == null) return Result.Cancelled;
                pipeLine.CreateDimensionLinesFromReferences(doc, plane, dimensionPoint, true);
                
                transaction.Commit();
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Dimension Pipe Line Command Error ", ex.Message);
               
                transaction.RollBack();
                return Result.Failed;
            }
        }

        
    }
}
