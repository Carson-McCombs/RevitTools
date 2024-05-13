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
        public const string FolderName = "Dimensioning";
        public const bool IsWIP = false;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("DimensionPipeLineCommand", "Dimensions Pipe Line", assembly.Location, "CarsonsAddins.DimensionPipeLineCommand")
            {
                ToolTip = "Gets the dimensions of all elements in pipe line."
            };
            return pushButtonData;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            if (doc.IsFamilyDocument)
            {
                message = "Command should not be used within a family document.";
                return Result.Failed;
            }
            Transaction transaction = new Transaction(doc);
            transaction.Start("DimensionPipeLineCommand");
            try
            {
                Reference pipeReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilters.SelectionFilter_Pipe(), "Please select a Pipe.");
                Element pipeElement = doc.GetElement(pipeReference.ElementId);
                if (!(pipeElement is Pipe pipe))
                {
                    transaction.RollBack();
                    message = "Pipe is null";
                    elements.Insert(pipeElement);
                    return Result.Cancelled;
                }
                PipeLine pipeLine = new PipeLine(doc.ActiveView, pipe);
                pipeLine.GetElements();
                Plane plane = null;
                if (doc.ActiveView.SketchPlane == null)
                {
                    plane = Plane.CreateByNormalAndOrigin(doc.ActiveView.ViewDirection, doc.ActiveView.Origin);

                    SubTransaction sketchplaneTransaction = new SubTransaction(doc);
                    sketchplaneTransaction.Start();
                    SketchPlane sketchplane = SketchPlane.Create(doc, plane);
                    doc.ActiveView.SketchPlane = sketchplane;
                    doc.ActiveView.HideElements(new List<ElementId>() { sketchplane.Id });
                    sketchplaneTransaction.Commit();
                }
                else
                {
                    plane = doc.ActiveView.SketchPlane.GetPlane();
                }

                ObjectSnapTypes objectSnapTypes = ObjectSnapTypes.Endpoints | ObjectSnapTypes.Nearest | ObjectSnapTypes.Intersections | ObjectSnapTypes.Perpendicular | ObjectSnapTypes.Points;
                XYZ dimensionPoint = uidoc.Selection.PickPoint(objectSnapTypes, "Please select where you would like the dimensions to be placed.");
                if (dimensionPoint == null) return Result.Cancelled;
                pipeLine.CreateDimensions(doc, plane, dimensionPoint, true);

                transaction.Commit();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                transaction.RollBack();
                return Result.Failed;
            }
        }
        

        
    }
}
