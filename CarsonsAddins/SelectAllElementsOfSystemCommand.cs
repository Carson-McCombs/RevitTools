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
    class SelectAllElementsOfSystemCommand : IExternalCommand, ISettingsComponent
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
            Transaction transaction = new Transaction(doc);
            transaction.Start("SelectAllElementsOfSystem");
            try
            {
                Reference elementReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilter_PipingElements(true,true,true,true,true), "Please select a Piping Element.");
                Element element = doc.GetElement(elementReference.ElementId);
                ElementId psTypeId = element.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
                PipingSystemType psType = doc.GetElement(psTypeId) as PipingSystemType;
                List<ElementId> psIds = psType.GetDependentElements(new ElementClassFilter(typeof(PipingSystem))) as List<ElementId>;
                List<ElementId> psElementIds = new List<ElementId>();

                foreach (ElementId psId in psIds)
                {
                    PipingSystem ps = doc.GetElement(psId) as PipingSystem;
                    if (ps == null) continue;
                    foreach (Element elem in ps.PipingNetwork)
                    {
                        psElementIds.Add(elem.Id);
                    }
                }
                uidoc.Selection.SetElementIds(psElementIds);
                transaction.Commit();
                //List<ElementId> elements = FilteredElementCollector()
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Select All Elements Of System Command", ex.Message);
                transaction.RollBack();
                return Result.Failed;
            }
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("SelectAllElementsOfSystemCommand", "Selects all Elements in Piping System", assembly.Location, "CarsonsAddins.SelectAllElementsOfSystemCommand");
            pushButtonData.ToolTip = "Selects all Elements in Piping System.";
            return pushButtonData;
        }
    }
}
