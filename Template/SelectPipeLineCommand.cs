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
using System.Windows.Controls;

namespace CarsonsAddins
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class SelectPipeLineCommand : IExternalCommand, ISettingsComponent
    {
        public const bool IsWIP = false;

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
            transaction.Start("SelectPipeLineCommand");
            try
            {
                Reference pipeReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilter_Pipe(), "Please select a Pipe.");
                Pipe pipe = doc.GetElement(pipeReference.ElementId) as Pipe;
                if (pipe == null)
                {
                    transaction.RollBack();
                    return Result.Cancelled;
                }
                PipeLine pipeLine = new PipeLine();
                List<Element> elements = pipeLine.GetPipeLine(uidoc, pipe, new SelectionFilter_PipingElements(true, true, false, true));
                List<ElementId> elementIds = new List<ElementId>();
                foreach (Element element in elements)
                {
                    
                    //if (element != null || !element.IsValidObject || element.Id == null || element.Id.Equals(ElementId.InvalidElementId)) continue;
                    elementIds.Add(element.Id);
                }
                uidoc.Selection.SetElementIds(elementIds);
                transaction.Commit();
                return Result.Succeeded;
                //Util.TryGetConnected()
            }
            catch (Exception ex)
            {
                transaction.RollBack();
                TaskDialog.Show("Pipe Line Section Command", ex.Message);
                return Result.Failed;
            }
            //return Result.Succeeded;
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushbuttonData = new PushButtonData("SelectPipeLineCommand", "Select Elements in Pipe Line", assembly.Location, "CarsonsAddins.SelectPipeLineCommand");
            pushbuttonData.ToolTip = "Selects all pipes connected to selected pipe.";
            return pushbuttonData;
        }
    }

 
    
}
