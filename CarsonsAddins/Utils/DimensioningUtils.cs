using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    public static class DimensioningUtils
    {
        public static Line GetDimensionSegmentLine(DimensionSegment segment, XYZ direction)
        {
            if (segment.Value == null) return null;
            XYZ offset = direction.Multiply((double)segment.Value / 2);
            return Line.CreateBound(segment.Origin - offset, segment.Origin + offset); ;
        }


        public static Dictionary<XYZ, (Dimension, DimensionSegment)> ExtractPseudoDimensions(Document doc, Dimension dimension)
        {
            Dictionary<XYZ, (Dimension, DimensionSegment)> dimensionSegmentsByOrigin = new Dictionary<XYZ, (Dimension, DimensionSegment)>();
            List<Reference> refList = ReferenceArrayToList(dimension.References);
            XYZ direction = (dimension.Curve as Line).Direction;
            for (int i = 0; i < dimension.Segments.Size; i++)
            {
                DimensionSegment segment = dimension.Segments.get_Item(i);
                dimensionSegmentsByOrigin.Add(segment.Origin, (null, segment));
                Line line = GetDimensionSegmentLine(segment, direction);

                ReferenceArray ary = new ReferenceArray();

                ary.Append(refList[i]);
                ary.Append(refList[i + 1]);
                Dimension dim = doc.Create.NewDimension(doc.ActiveView, line, ary);
                dim.ValueOverride = segment.ValueOverride;
                dim.Above = segment.Above;
                dim.Below = segment.Below;
                dim.Prefix = segment.Prefix;
                dim.Suffix = segment.Suffix;
                dim.TextPosition = segment.TextPosition;
                dim.LeaderEndPosition = segment.LeaderEndPosition;

            }
            return dimensionSegmentsByOrigin;
        }
        public static List<Reference> ReferenceArrayToList(ReferenceArray referenceArray)
        {
            List<Reference> references = new List<Reference>();
            foreach (Reference reference in referenceArray)
            {
                references.Add(reference);
            }
            return references;
        }




        public class DimensionAndSegment
        {
            private readonly Dimension dimension = null;
            private readonly DimensionSegment dimensionSegment = null;
            private enum DimensionOrSegmentState { None, Dimension, DimensionSegment }
            private readonly DimensionOrSegmentState state = DimensionOrSegmentState.None;

            public string Above
            {
                get
                {
                    switch (state)
                    {

                        case (DimensionOrSegmentState.Dimension): return dimension.Above;
                        case (DimensionOrSegmentState.DimensionSegment): return dimensionSegment.Above;
                        default: return null;
                    }
                }
                set
                {
                    switch (state)
                    {
                        case (DimensionOrSegmentState.Dimension): dimension.Above = value; break;
                        case (DimensionOrSegmentState.DimensionSegment): dimensionSegment.Above = value; break;
                        default: break;
                    }
                }
            }

            public string Below
            {
                get
                {
                    switch (state)
                    {

                        case (DimensionOrSegmentState.Dimension): return dimension.Below;
                        case (DimensionOrSegmentState.DimensionSegment): return dimensionSegment.Below;
                        default: return null;
                    }
                }
                set
                {
                    switch (state)
                    {
                        case (DimensionOrSegmentState.Dimension): dimension.Below = value; break;
                        case (DimensionOrSegmentState.DimensionSegment): dimensionSegment.Below = value; break;
                        default: break;
                    }
                }
            }
            public string ValueString
            {
                get
                {
                    switch (state)
                    {

                        case (DimensionOrSegmentState.Dimension): return dimension.ValueString;
                        case (DimensionOrSegmentState.DimensionSegment): return dimensionSegment.ValueString;
                        default: return null;
                    }
                }
            }

            public string ValueOverride
            {
                get
                {
                    switch (state)
                    {

                        case (DimensionOrSegmentState.Dimension): return dimension.ValueOverride;
                        case (DimensionOrSegmentState.DimensionSegment): return dimensionSegment.ValueOverride;
                        default: return null;
                    }
                }
                set
                {
                    switch (state)
                    {
                        case (DimensionOrSegmentState.Dimension): dimension.ValueOverride = value; break;
                        case (DimensionOrSegmentState.DimensionSegment): dimensionSegment.ValueOverride = value; break;
                        default: break;
                    }
                }
            }
            public string Prefix
            {
                get
                {
                    switch (state)
                    {

                        case (DimensionOrSegmentState.Dimension): return dimension.Prefix;
                        case (DimensionOrSegmentState.DimensionSegment): return dimensionSegment.Prefix;
                        default: return null;
                    }
                }
                set
                {
                    switch (state)
                    {
                        case (DimensionOrSegmentState.Dimension): dimension.Prefix = value; break;
                        case (DimensionOrSegmentState.DimensionSegment): dimensionSegment.Prefix = value; break;
                        default: break;
                    }
                }
            }
            public string Suffix
            {
                get
                {
                    switch (state)
                    {

                        case (DimensionOrSegmentState.Dimension): return dimension.Suffix;
                        case (DimensionOrSegmentState.DimensionSegment): return dimensionSegment.Suffix;
                        default: return null;
                    }
                }
                set
                {
                    switch (state)
                    {
                        case (DimensionOrSegmentState.Dimension): dimension.Suffix = value; break;
                        case (DimensionOrSegmentState.DimensionSegment): dimensionSegment.Suffix = value; break;
                        default: break;
                    }
                }
            }
            public string LabelText
            {
                get
                {
                    switch (state)
                    {

                        case (DimensionOrSegmentState.Dimension): return dimension.get_Parameter(BuiltInParameter.DIM_LABEL).AsValueString();
                        case (DimensionOrSegmentState.DimensionSegment): return "";
                        default: return null;
                    }
                }
            }
            public DimensionAndSegment(Dimension dimension)
            {
                this.dimension = dimension;
                state = (dimension == null) ? DimensionOrSegmentState.None : DimensionOrSegmentState.Dimension;
            }

            public DimensionAndSegment(DimensionSegment dimensionSegment)
            {
                this.dimensionSegment = dimensionSegment;
                state = (dimensionSegment == null) ? DimensionOrSegmentState.None : DimensionOrSegmentState.DimensionSegment;

            }
            public DimensionAndSegment((Dimension, DimensionSegment) pair)
            {
                dimension = pair.Item1;
                dimensionSegment = pair.Item2;
                state = GetState();
            }


            private DimensionOrSegmentState GetState()
            {
                bool dimensionIsNull = dimension == null;
                bool dimensionSegmentIsNull = dimensionSegment == null;
                if (dimensionIsNull && dimensionSegmentIsNull) return DimensionOrSegmentState.None;
                else if (dimensionIsNull) return DimensionOrSegmentState.DimensionSegment;
                else if (dimensionSegmentIsNull) return DimensionOrSegmentState.Dimension;
                else return DimensionOrSegmentState.None;
            }
        }

        /// <summary>
        /// Returns the position of the ConnectorManager based on the average of all the Connector Elements' Origins. Not the same as the Element position / location.
        /// </summary>
        /// <param name="cm">A ConnectorManager Instance.</param>
        /// <returns>Average position of all of the Connector Elements within the ConnectorManager.</returns>
        public static XYZ GetConnectorManagerCenter(ConnectorManager cm)
        {
            XYZ origin = XYZ.Zero;
            Connector[] connectors = ConnectionUtils.GetConnectors(cm);
            foreach (Connector con in connectors)
            {
                origin += con.Origin;
            }
            origin /= connectors.Length;
            return origin;
        }
    }
    
}
