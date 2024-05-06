using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace CarsonsAddins
{

    /// <summary>
    /// Command that Filters the current selection to only allow Piping Elements ( i.e. Pipes, Pipe Flanges, Pipe Bends / Junctions, and Pipe Accessories ).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class FilterPipingElementsCommand : IExternalCommand, ISettingsComponent
    {
        public const string FolderName = "Misc";
        public const bool IsWIP = false;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("FilterPipingElementsCommand", "Filters Piping Elements", assembly.Location, "CarsonsAddins.FilterPipingElementsCommand")
            {
                ToolTip = "Filters Selection."
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
                TaskDialog.Show("Filter Piping Elements Command", "Command should not be used within a family document.");
                return Result.Failed;
            }
            Transaction transaction = new Transaction(doc);
            transaction.Start("FilterPipingElementsCommand");
            try
            {
                SelectionFilters.SelectionFilter_PipingElements filter = new SelectionFilters.SelectionFilter_PipingElements(false);
                List<ElementId> selectedIds = uidoc.Selection.GetElementIds() as List<ElementId>;
                List<Element> selectedElements = selectedIds.Select<ElementId, Element>(id => doc.GetElement(id)).ToList();
                List<ElementId> filteredIds = new List<ElementId>();
                foreach (Element element in selectedElements)
                {
                    
                    if (!filter.AllowElement((element))) continue;
                    filteredIds.Add(element.Id);
                    elements.Insert(element);
                }
                uidoc.Selection.SetElementIds(filteredIds);
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
