using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins
{

    public class SetDimensionsTextEventHandler : IExternalEventHandler
    {
        private List<(Dimension, DimensionSegment)> dimensionsAndSegments = new List<(Dimension, DimensionSegment)>();
#nullable enable
        private string? above, below, valueOverride, prefix, suffix;
        public void Inject(List<(Dimension, DimensionSegment)> dimensionsAndSegments) => this.dimensionsAndSegments = dimensionsAndSegments;
        public void Inject(string above = "", string below = "", string valueOverride = "", string prefix = "", string suffix = "")
        {
            this.above = above;
            this.below = below;
            this.valueOverride = valueOverride;
            this.prefix = prefix;
            this.suffix = suffix;
        }
        public void Clear() => dimensionsAndSegments.Clear();
        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;
            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Set Dimensions Text EventHandler", "Command should not be used within a family document.");
                return;
            }
            if (dimensionsAndSegments == null || dimensionsAndSegments.Count == 0)
            {
                SelectIndividualDimensionsCommand command = new SelectIndividualDimensionsCommand();
                command.Execute(app);
                dimensionsAndSegments = command.DimensionsAndSegments;
            }

            Transaction transaction = new Transaction(doc);
            transaction.Start("Set Dimensions Text");
            try
            {
                for (int i = 0; i < dimensionsAndSegments.Count; i++)
                {
                    SetDimensionOrSegment(dimensionsAndSegments[i], above, below, valueOverride, prefix, suffix);
                }
                transaction.Commit();
            }

            catch
            {
                transaction.RollBack();
            }
        }
        private void SetDimensionOrSegment((Dimension, DimensionSegment) pair, string above, string below, string valueOverride, string prefix, string suffix)
        {
            new Utils.DimensioningUtils.DimensionAndSegment(pair)
            {
                Above = above,
                Below = below,
                ValueOverride = valueOverride,
                Prefix = prefix,
                Suffix = suffix
            };
        }
        public string GetName()
        {
            return "Set Dimensions Text";
        }
    }
}
