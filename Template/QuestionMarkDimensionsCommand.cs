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

        public PushButtonData RegisterButton(Assembly assembly)
        {
            PushButtonData pushButtonData = new PushButtonData("QuestionMarkDimensionsCommand", "Question Mark Dimensions", assembly.Location, "CarsonsAddins.QuestionMarkDimensionsCommand");
            pushButtonData.ToolTip = "Overrides selected dimensions' value with a question mark.";
            return pushButtonData;
        }

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

        


        private void SetDimensionOrSegmentQuestionMark((Dimension, DimensionSegment) dimensionOrSegment)
        {
            if (dimensionOrSegment.Item1 != null) dimensionOrSegment.Item1.ValueOverride = "?";
            if (dimensionOrSegment.Item2 != null) dimensionOrSegment.Item2.ValueOverride = "?";

        }
        

        

        
    }
    
    
}
