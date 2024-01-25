using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace CarsonsAddins
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class QuestionMarkDimensionsCommand : IExternalCommand, ISettingsComponent
    {
        public const bool IsWIP = false;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Question Mark Dimensions Command", "Command should not be used within a family document.");
                return Result.Failed;
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
                    Dictionary<XYZ, (Dimension, DimensionSegment)> tmp = ExtractPseudoDimensions(doc, dimension);
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
            //doc.acti(multiSegmentDimensions);
            if (errorLog != "") TaskDialog.Show("Dummy Seperate Dimensions Error", errorLog);
            List<Reference > selectedDimensionReferences;
            try
            {
                selectedDimensionReferences = uidoc.Selection.PickObjects(ObjectType.Element, new SelectionFilter_LinearDimension(), "Please select dimensions to question mark.") as List<Reference>;

            }
            catch
            {
                dummyTransaction.RollBack();
                return Result.Cancelled;
            }
            List<XYZ> selectedDimensionOrigins = new List<XYZ>();
            
            foreach (Reference reference in selectedDimensionReferences)
            {
                Dimension dimension = doc.GetElement(reference.ElementId) as Dimension;
                if (dimension == null) continue;
                //selectedDimensionOrigins.Add(SetByBasis(dimension.Origin, viewOrigin, viewDirection));
                selectedDimensionOrigins.Add(dimension.Origin);
                
                
            }
            dummyTransaction.RollBack();
            if (selectedDimensionOrigins == null || selectedDimensionOrigins.Count == 0) return Result.Cancelled;

            Transaction questionMarkTransaction = new Transaction(doc);
            questionMarkTransaction.Start("QuestionMarkTransaction");
            //List<XYZ> leftoverKeys = dimensionSegmentsByOrigin.Keys.ToList();
            //List<XYZ> leftoverSelected = new List<XYZ>();
            foreach(XYZ origin in selectedDimensionOrigins)
            {
                if (origin == null) continue;
                if (dimensionSegmentsByOrigin.ContainsKey(origin)) 
                {
                    //dimensionSegmentsByOrigin[origin].ValueOverride = "?";
                    SetDimensionOrSegmentQuestionMark(dimensionSegmentsByOrigin[origin]);
                    //leftoverKeys.Remove(origin);
                }
                else
                {
                   // bool found = false;
                    foreach (XYZ key in dimensionSegmentsByOrigin.Keys)
                    {
                        if (!origin.IsAlmostEqualTo(key)) continue;
                        //found = true;
                        SetDimensionOrSegmentQuestionMark(dimensionSegmentsByOrigin[key]);
                        //dimensionSegmentsByOrigin[key].ValueOverride = "?";
                        //leftoverKeys.Remove(key);
                        break;
                    }
                    //if (!found) leftoverSelected.Add(origin);
                }
            }
            
            questionMarkTransaction.Commit();
            return Result.Succeeded;
        }

        private Dictionary<XYZ, (Dimension, DimensionSegment)> ExtractPseudoDimensions(Document doc, Dimension dimension)
        {
            Dictionary<XYZ, (Dimension, DimensionSegment)> dimensionSegmentsByOrigin = new Dictionary<XYZ, (Dimension, DimensionSegment)>();
            XYZ direction = (dimension.Curve as Line).Direction;
            for (int i = 0; i < dimension.Segments.Size; i++)
            {
                DimensionSegment segment = dimension.Segments.get_Item(i);
                dimensionSegmentsByOrigin.Add(segment.Origin, (null, segment));
                Line line = GetDimensionSegmentLine(segment, direction);

                ReferenceArray ary = GetDetailReference(doc, line);
                Dimension dim = doc.Create.NewDimension(doc.ActiveView, line, ary);
                dim.ValueOverride = segment.ValueOverride;
            }
            return dimensionSegmentsByOrigin;
        }


        private void SetDimensionOrSegmentQuestionMark((Dimension, DimensionSegment) dimensionOrSegment)
        {
            if (dimensionOrSegment.Item1 != null) dimensionOrSegment.Item1.ValueOverride = "?";
            if (dimensionOrSegment.Item2 != null) dimensionOrSegment.Item2.ValueOverride = "?";

        }
        private Line GetDimensionSegmentLine(DimensionSegment segment, XYZ direction)
        {
            if (segment.Value == null) return null;
            XYZ offset = direction.Multiply((double)segment.Value / 2);
            return Line.CreateBound(segment.Origin - offset, segment.Origin + offset); ;
        }

        private ReferenceArray GetDetailReference(Document doc, Line dimensionLine)
        {
            ReferenceArray rf = new ReferenceArray();
            
            //SketchPlane sketchPlane = SketchPlane.Create()
            //Plane plane = sketchPlane.GetPlane();
            View activeView = doc.ActiveView;
            //Plane plane = Plane.CreateByNormalAndOrigin(activeView.ViewDirection, activeView.Origin);

            XYZ perp = Line.CreateBound(dimensionLine.GetEndPoint(0), dimensionLine.GetEndPoint(1)).Direction.CrossProduct(activeView.UpDirection);
            
            DetailCurve line = doc.Create.NewDetailCurve(activeView, dimensionLine.CreateOffset(0.00000001d, perp));
            
            rf.Append(line.GeometryCurve.GetEndPointReference(0));
            rf.Append(line.GeometryCurve.GetEndPointReference(1));
            return rf;
        }

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("QuestionMarkDimensionsCommand", "Question Mark Dimensions", assembly.Location, "CarsonsAddins.QuestionMarkDimensionsCommand");
            pushButtonData.ToolTip = "Overrides selected dimensions' value with a question mark.";
            return pushButtonData;
        }
    }
    
    
}
