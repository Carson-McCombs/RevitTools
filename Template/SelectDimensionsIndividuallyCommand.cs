using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CarsonsAddins
{
    public class SelectIndividualDimensionsCommand : IExternalCommand
    {
        private List<(Dimension, DimensionSegment)> dimensionsAndSegments = new List<(Dimension, DimensionSegment)>();

        public List<(Dimension, DimensionSegment)> DimensionsAndSegments { get => dimensionsAndSegments; }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Execute(commandData.Application);
        }
        

        public Result Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;
            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Question Mark Dimensions Command", "Command should not be used within a family document.");
                return Result.Cancelled;
            }
            List<ElementId> allDimensionReferences = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Dimensions).ToElementIds() as List<ElementId>;
            Dictionary<XYZ, (Dimension, DimensionSegment)> dimensionSegmentsByOrigin = new Dictionary<XYZ, (Dimension, DimensionSegment)>();
            Transaction dummyTransaction = new Transaction(doc);
            dummyTransaction.Start("DummySeperateDimensionSegments");
            string errorLog = "";
            List<ElementId> multiSegmentDimensions = new List<ElementId>();
            foreach (ElementId id in allDimensionReferences)
            {
                try
                {
                    Dimension dimension = doc.GetElement(id) as Dimension;
                    if (dimension == null) continue;
                    if (!DimensionShape.Linear.Equals(dimension.DimensionShape)) continue;
                    if (dimension.HasOneSegment())
                    {
                        dimensionSegmentsByOrigin.Add(dimension.Origin, (dimension, null));
                        continue;
                    }
                    multiSegmentDimensions.Add(id);
                    Dictionary<XYZ, (Dimension, DimensionSegment)> tmp = Util.ExtractPseudoDimensions(doc, dimension);
                    foreach (XYZ origin in tmp.Keys)
                    {
                        dimensionSegmentsByOrigin.Add(origin, tmp[origin]);
                    }

                }
                catch (Exception ex)
                {
                    errorLog = errorLog + ex.Message + "\n";
                }

            }
            doc.Delete(multiSegmentDimensions);
            if (errorLog != "") TaskDialog.Show("Dummy Seperate Dimensions Error", errorLog);
            List<Reference> selectedDimensionReferences = new List<Reference>();
            try
            {
                selectedDimensionReferences = uidoc.Selection.PickObjects(ObjectType.Element, new SelectionFilter_LinearDimension(), "Please select dimensions to question mark.") as List<Reference>;
            }
            catch
            {
                dummyTransaction.RollBack();
                return Result.Failed;
            }
            List<XYZ> selectedDimensionOrigins = GetOriginsOfSelectedReferences(doc, selectedDimensionReferences);

            dimensionsAndSegments = GetSelectedDimensionsAndSegments(dimensionSegmentsByOrigin, selectedDimensionOrigins); ;


            dummyTransaction.RollBack();
            return Result.Succeeded;
        }


        private static List<XYZ> GetOriginsOfSelectedReferences(Document doc, List<Reference> selectedDimensionReferences)
        {
            List<XYZ> origins = new List<XYZ>();    
            foreach (Reference reference in selectedDimensionReferences)
            {
                Dimension dimension = doc.GetElement(reference.ElementId) as Dimension;
                if (dimension == null) continue;
                origins.Add(dimension.Origin);


            }
            return origins;
        }

        private List<(Dimension, DimensionSegment)> GetSelectedDimensionsAndSegments(Dictionary<XYZ, (Dimension, DimensionSegment)> dimensionSegmentsByOrigin, List<XYZ> selectedDimensionOrigins)
        {
            List<(Dimension, DimensionSegment)> selected = new List<(Dimension, DimensionSegment)>();
            foreach (XYZ origin in selectedDimensionOrigins)
            {
                if (origin == null) continue;
                if (dimensionSegmentsByOrigin.ContainsKey(origin))
                {
                    selected.Add(dimensionSegmentsByOrigin[origin]);
                }
                else
                {
                    foreach (XYZ key in dimensionSegmentsByOrigin.Keys)
                    {
                        if (!origin.IsAlmostEqualTo(key)) continue;
                        selected.Add(dimensionSegmentsByOrigin[key]);
                        break;
                    }
                }
            }
            return selected;
        }
    }


    public class SelectIndividualDimensionsEventHandler : IExternalEventHandler
    {
        private List<(Dimension, DimensionSegment)> dimensionsAndSegments = new List<(Dimension, DimensionSegment)>();

        public List<(Dimension, DimensionSegment)> DimensionsAndSegments { get => dimensionsAndSegments; }
        public void Execute(UIApplication app)
        {
            SelectIndividualDimensionsCommand command = new SelectIndividualDimensionsCommand();
            command.Execute(app);
            dimensionsAndSegments = command.DimensionsAndSegments;
        }

        public string GetName()
        {
            return "Select Individual Dimensions Event Handler";
        }
    }

}
