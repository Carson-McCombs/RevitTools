using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CarsonsAddins.Properties;
using CarsonsAddins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CarsonsAddins.Settings.Dimensioning.Models;

namespace CarsonsAddins
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class DebugGeometryCommand : IExternalCommand, ISettingsComponent
    {
        public const string FolderName = "";
        public const bool IsWIP = true;
        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("Debug Geometry", "Debug Geometry", assembly.Location, "CarsonsAddins.DebugGeometryCommand")
            {
                AvailabilityClassName = typeof(Setup.Availablity.Availability_ProjectDocumentAndActiveView).FullName,
                ToolTip = "Debugs Element Geometry References."
            };
            return pushButtonData;
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Reference selectionReference = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);
            if (selectionReference == null) return Result.Cancelled;
            Transaction transaction = new Transaction(uidoc.Document);
            transaction.Start("Debug Element Geometry");
            try
            {
                Element element = uidoc.Document.GetElement(selectionReference);
                elements.Insert(element);
                ElementId[] validStyleIds = GetStyleIds(uidoc.Document);
                GeometryUtils.XYZWithReference[] exposedWithView = GeometryUtils.XYZWithReference.StripGeometryObjectsWithReferences(validStyleIds, GeometryUtils.GetGeometryOptions(uidoc.Document.ActiveView), element);
                string exposedWithViewString = string.Join("\n", exposedWithView.Select(xyzWR =>  xyzWR.xyz.ToString() + " -> " + xyzWR.reference.ConvertToStableRepresentation(uidoc.Document)).ToArray());
                TaskDialog.Show("Geometry with View", exposedWithViewString);
                GeometryUtils.XYZWithReference[] exposed = GeometryUtils.XYZWithReference.StripGeometryObjectsWithReferences(validStyleIds, GeometryUtils.GetGeometryOptions(), element);
                string exposedString = string.Join("\n", exposed.Select(xyzWR => xyzWR.xyz.ToString() + " -> " + xyzWR.reference.ConvertToStableRepresentation(uidoc.Document)).ToArray());
                TaskDialog.Show("Geometry", exposedString);
                Connector[] connectors = ConnectionUtils.GetConnectors(element);
                Plane plane = uidoc.Document.ActiveView.SketchPlane.GetPlane();
                string[] referenceStrings = connectors.Select(connector => FindReference(exposedWithView, plane, connector.Origin) ?? FindReference(exposed, plane, connector.Origin)).Select(reference => reference?.ConvertToStableRepresentation(uidoc.Document) ?? "NULL").ToArray();
                string output = string.Join("\n", referenceStrings);
                output += "\n Center: " + (FindReference(exposedWithView, plane, GeometryUtils.GetOrigin(element.Location)) ?? FindReference(exposed, plane, GeometryUtils.GetOrigin(element.Location)))?.ConvertToStableRepresentation(uidoc.Document) ?? "NULL";
                TaskDialog.Show("Guess Connections", output);
                transaction.Commit();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        private Reference FindReference(GeometryUtils.XYZWithReference[] xyzReferences, Plane plane, XYZ xyz)
        {
            Reference reference = xyzReferences.Where(xyzReference => xyz.IsAlmostEqualTo(xyzReference.xyz)).FirstOrDefault().reference;
            if (reference != null) return reference;
            XYZ projectedXYZ = GeometryUtils.ProjectPointOntoPlane(plane, xyz);
            Reference projectedReference = xyzReferences.Where(xyzReference => xyz.IsAlmostEqualTo(GeometryUtils.ProjectPointOntoPlane(plane, xyzReference.xyz))).FirstOrDefault().reference;
            return projectedReference;
        }
        private ElementId[] GetStyleIds(Document doc) //requires that Load Dimension Types has been called first
        {
            List<ElementId> validStyleIds = new List<ElementId>();

            if (string.IsNullOrWhiteSpace(MySettings.Default.DimensionStyles_Preferences)) return new ElementId[0];
            BuiltInCategory[] pipingCategories = new BuiltInCategory[] { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory, BuiltInCategory.OST_MechanicalEquipment, BuiltInCategory.OST_PipeCurvesCenterLine, BuiltInCategory.OST_PipeFittingCenterLine, BuiltInCategory.OST_CenterLines, BuiltInCategory.OST_ReferenceLines };
            GraphicsStyle[] graphicStyles = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().Where(gs => pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Id.IntegerValue) || ((gs.GraphicsStyleCategory.Parent != null) && pipingCategories.Contains((BuiltInCategory)gs.GraphicsStyleCategory.Parent.Id.IntegerValue))).ToArray();
            try
            {
                DimensionPreferencesSave dimensionStyleNames = JsonConvert.DeserializeObject<DimensionPreferencesSave>(MySettings.Default.DimensionStyles_Preferences);
                if (dimensionStyleNames.graphicsStyleNames == null) return new ElementId[0];
                foreach (GraphicsStyle graphicsStyle in graphicStyles)
                {
                    if (dimensionStyleNames.graphicsStyleNames.Contains(graphicsStyle.Name)) validStyleIds.Add(graphicsStyle.Id);
                }

            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error Loading Dimension Styles from DB", ex.Message);
            }
            return validStyleIds.ToArray();

        }

    }
}
