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
using System.Windows.Forms;

namespace CarsonsAddins
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class SelectAllElementsOfSystemCommand : IExternalCommand, ISettingsComponent
    {
        public const string FolderName = "Misc";
        public const bool IsWIP = false;

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("SelectAllElementsOfSystemCommand", "Selects all Elements in Piping System", assembly.Location, "CarsonsAddins.SelectAllElementsOfSystemCommand")
            {
                AvailabilityClassName = typeof(Setup.CommandAvailability.Availability_ProjectDocumentsOnly).FullName,
                ToolTip = "Selects all Elements in Piping System."
            };
            return pushButtonData;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Transaction transaction = new Transaction(doc);
            transaction.Start("SelectAllElementsOfSystem");
            try
            {
                Reference elementReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilters.SelectionFilter_PipingElements(false), "Please select a Piping Element.");
                Element element = doc.GetElement(elementReference.ElementId);
                ElementId psTypeId = element.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsElementId();
                PipingSystemType psType = doc.GetElement(psTypeId) as PipingSystemType;
                List<ElementId> psIds = psType.GetDependentElements(new ElementClassFilter(typeof(PipingSystem))) as List<ElementId>;
                List<ElementId> psElementIds = new List<ElementId>();

                foreach (ElementId psId in psIds)
                {
                    if (!(doc.GetElement(psId) is PipingSystem ps)) continue;
                    foreach (Element elem in ps.PipingNetwork)
                    {
                        psElementIds.Add(elem.Id);
                        elements.Insert(elem);
                    }
                }
                uidoc.Selection.SetElementIds(psElementIds);
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
