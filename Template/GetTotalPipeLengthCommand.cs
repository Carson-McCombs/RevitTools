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
    class GetTotalPipeLengthCommand : IExternalCommand, ISettingsComponent
    {
        public const bool IsWIP = false;
        private static double nominalLength = 17.5;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("GetTotalPipeLengthCommand", "Get Total Pipe Length", assembly.Location, "CarsonsAddins.GetTotalPipeLengthCommand");
            pushButtonData.ToolTip = "Gets the total length of all selected pipe.";
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
            
            try
            {
                transaction.Start("GetTotalPipeLengthCommand");
                double totalLength = 0;
                List<ElementId> currentSelection = uidoc.Selection.GetElementIds() as List<ElementId>;
                foreach (ElementId elementId in currentSelection)
                {
                    if (elementId.Equals(ElementId.InvalidElementId)) continue;
                    Pipe pipe = doc.GetElement(elementId) as Pipe;
                    if (pipe == null) continue;
                    double? length = Util.GetPipeLength(pipe);
                    if (length == null) continue;
                    totalLength += (double)length;
                }
                int totalFeet = (int)totalLength;
                double totalInches = (totalLength - totalFeet) * 12;
                double closureLength = totalLength % nominalLength;
                int closureFeet = (int)closureLength;
                double closureInches = (closureLength - closureFeet) * 12;
                TaskDialog.Show("Total Pipe Length", "Total Pipe Length:\n" + totalFeet + "\'- " + totalInches + "\"");

                //TaskDialog.Show("Total Pipe Length", "Total Pipe Length:\n" + totalFeet + "\'- " + totalInches + "\"\n\n Closure Piece:\n " + closureFeet + "\'- " + closureInches + "\"");
                transaction.Commit();
                return Result.Succeeded;

            }
            catch (Exception e)
            {
                transaction.RollBack();
                return Result.Failed;
            }
        }

        
    }
}
