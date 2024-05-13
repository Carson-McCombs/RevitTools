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


    /// <summary>
    /// Gets all the Pipe Elements from the User's current selection and shows the Sum of all of their lengths.  
    /// Note: Nominal Lengths were used at one point for the additional purpose of calculating "closure" pieces. Will revert at some point after adding a popup Window or Dockable Pane.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class GetTotalPipeLengthCommand : IExternalCommand, ISettingsComponent
    {
        public const string FolderName = "";
        public const bool IsWIP = false;
        //private static double nominalLength = 17.5;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("GetTotalPipeLengthCommand", "Get Total Pipe Length", assembly.Location, "CarsonsAddins.GetTotalPipeLengthCommand")
            {
                AvailabilityClassName = typeof(Setup.CommandAvailability.Availability_ProjectDocumentsOnly).FullName,
                Image = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.total_pipe_length_icon_32.png"),
                LargeImage = Utils.MediaUtils.GetImage(assembly, "CarsonsAddins.Resources.total_pipe_length_icon_32.png"),
                ToolTip = "Gets the total length of all selected pipe."
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

            try
            {
                transaction.Start("GetTotalPipeLengthCommand");
                uint count = 0;
                double totalLength = 0;
                List<ElementId> currentSelection = uidoc.Selection.GetElementIds() as List<ElementId>;
                foreach (ElementId elementId in currentSelection)
                {
                    if (elementId.Equals(ElementId.InvalidElementId)) continue;
                    if (!(doc.GetElement(elementId) is Pipe pipe)) continue;
                    double? length = (pipe.Location as LocationCurve).Curve.Length;
                    if (length == null) continue;
                    count++;
                    totalLength += (double)length;
                }
                int totalFeet = (int)totalLength;
                double totalInches = (totalLength - totalFeet) * 12;
                //double closureLength = totalLength % nominalLength;
                //int closureFeet = (int)closureLength;
                //double closureInches = (closureLength - closureFeet) * 12;
                TaskDialog.Show("Total Pipe Length ( " + count + " )", "Total Pipe Length:\n" + totalFeet + "\'- " + totalInches + "\"");

                transaction.Commit();
                return Result.Succeeded;

            }
            catch (Exception ex) 
            {
                transaction.RollBack();
                message = ex.Message;
                return Result.Failed;
            }
        }


        
    }
}
