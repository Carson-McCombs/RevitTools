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
            

            Transaction questionMarkTransaction = new Transaction(doc);
            questionMarkTransaction.Start("QuestionMarkTransaction");
            //List<XYZ> leftoverKeys = dimensionSegmentsByOrigin.Keys.ToList();
            //List<XYZ> leftoverSelected = new List<XYZ>();
            SelectIndividualDimensionsCommand selectCommand = new SelectIndividualDimensionsCommand();
            selectCommand.Execute(commandData, ref message, elements);
            List<(Dimension, DimensionSegment)> dimensionsAndSegments = selectCommand.DimensionsAndSegments;
            foreach ((Dimension, DimensionSegment) dimensionOrSegment in dimensionsAndSegments)
            {
                SetDimensionOrSegmentQuestionMark(dimensionOrSegment);
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
