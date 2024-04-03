using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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
    /// Command that Filters the current selection to only allow Piping Elements ( i.e. Pipes, Pipe Flanges, Pipe Bends / Junctions, and Pipe Accessories ).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class FilterPipingElementsCommand : IExternalCommand, ISettingsComponent
    {
        public const bool IsWIP = false;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("FilterPipingElementsCommand", "Filters Piping Elements", assembly.Location, "CarsonsAddins.FilterPipingElementsCommand");
            pushButtonData.ToolTip = "Filters Selection.";
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
                TaskDialog.Show("Filter Piping Elements Command", "Command should not be used within a family document.");
                return Result.Failed;
            }
            Transaction transaction = new Transaction(doc);
            transaction.Start("FilterPipingElementsCommand");
            try
            {
                SelectionFilter_PipingElements filter = new SelectionFilter_PipingElements(true, true, true, true, true);
                List<ElementId> selectedIds = uidoc.Selection.GetElementIds() as List<ElementId>;
                List<ElementId> filteredIds = new List<ElementId>();
                foreach (ElementId id in selectedIds)
                {
                    if (filter.AllowElement((doc.GetElement(id)))) filteredIds.Add(id);
                }
                uidoc.Selection.SetElementIds(filteredIds);
                transaction.Commit();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Filter Piping Elements Error ", ex.Message);
                transaction.RollBack();
                return Result.Failed;
            }
        }
    }
}
