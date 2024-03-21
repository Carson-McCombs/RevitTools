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
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class TestGetDimensionPointsCommand : IExternalCommand, ISettingsComponent
    {
        public const bool IsWIP = false;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            return new PushButtonData("TestGetDimensionPointsCommand", "Get Dimension Points", assembly.Location, "CarsonsAddins.TestGetDimensionPointsCommand");
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Reference elementReference = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilter_PipingElements(false, true, true, true, true));
            FamilyInstance familyInstance = doc.GetElement(elementReference) as FamilyInstance;
            if (familyInstance == null) return Result.Cancelled;
            Plane plane = Plane.CreateByNormalAndOrigin(doc.ActiveView.ViewDirection, doc.ActiveView.Origin);
            string exposedLog = "";
            string familyLog = "";
            foreach (Connector connector in Util.GetConnectors(familyInstance))
            {
                Reference reference = Util.GetPseudoReferenceOfConnector(Util.GetGeometryOptions(), plane, connector);
                if (reference != null) exposedLog += reference.ConvertToStableRepresentation(doc) + '\n';
                reference = Util.GetPseudoReferenceOfConnector(Util.GetGeometryOptions(doc.ActiveView), plane, connector);
                if (reference != null) familyLog += reference.ConvertToStableRepresentation(doc) + '\n';


            }
            TaskDialog.Show("GetConnectorReferences", "Exposed:\n---------------------" + exposedLog + "\n \n Family:\n" + familyLog);

            return Result.Succeeded;
        }

        
    }
}
