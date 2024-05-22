using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarsonsAddins.Utils
{
    public static class DimensioningUtils
    {

        public enum FlangeDimensionMode { None, Default, Exact, Partial, Negate }
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
            Line line = dimension.Curve as Line;
            XYZ direction = (dimension.Curve as Line).Direction;
            for (int i = 0; i < dimension.Segments.Size; i++)
            {
                DimensionSegment segment = dimension.Segments.get_Item(i);
                dimensionSegmentsByOrigin.Add(segment.Origin, (null, segment));

                ReferenceArray ary = new ReferenceArray();

                ary.Append(refList[i]);
                ary.Append(refList[i + 1]);
                try
                {
                    Dimension dim = doc.Create.NewDimension(doc.ActiveView, line, ary);
                    CopySegmentDataToDimension(dim, segment);
                } 
                catch
                {
                    Line segmentLine = GetDimensionSegmentLine(segment, direction);
                    
                    DetailCurve detailCurve = doc.Create.NewDetailCurve(doc.ActiveView, segmentLine);
                    
                    ary.Clear();
                    ary.Append(detailCurve.GeometryCurve.GetEndPointReference(0));
                    ary.Append(detailCurve.GeometryCurve.GetEndPointReference(1));
                    try
                    {
                        Dimension dim = doc.Create.NewDimension(doc.ActiveView, line, ary);
                        CopySegmentDataToDimension(dim, segment);
                    }
                    catch 
                    {

                    }
                    continue;
                }




            }
            return dimensionSegmentsByOrigin;
        }
        public static void CopySegmentDataToDimension(Dimension dimension, DimensionSegment segment)
        {
            dimension.ValueOverride = segment.ValueOverride;
            dimension.Above = segment.Above;
            dimension.Below = segment.Below;
            dimension.Prefix = segment.Prefix;
            dimension.Suffix = segment.Suffix;
            dimension.TextPosition = segment.TextPosition;
            //dim.LeaderEndPosition = segment.LeaderEndPosition;
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



       
        public static Reference GetFlangeEndReference(Plane plane, Element flange, Element connected)
        {
            if (flange == null || connected == null) return null;
            Connector connector = ConnectionUtils.TryGetOneSidedConnection(flange, connected);
            if (connector == null) return null;
            Connector adjacent = ConnectionUtils.GetAdjacentConnector(connector);
            if (adjacent == null) return null;
            return GeometryUtils.GetPseudoReferenceOfConnector(null, GeometryUtils.GetGeometryOptions(), plane, adjacent);
        }

        /// <summary>
        /// Creates a Dimension from each end of a pipe. *Note: DimensionLinearElement could be called instead, but as a pipe has static geometry, a dedicated function ( that should be faster than the generic function ) was used instead.
        /// </summary>
        /// <param name="doc">The active Document.</param>
        /// <param name="dimensionType">The Dimension Type of the created dimension</param>
        /// <param name="dimensionLine">The line that the created dimension will be located on.</param>
        /// <param name="pipe">The pipe element within the Pipeline that is being dimensioned.</param>
        /// <returns>a newly created Dimension going from each end of the pipe element</returns>
        public static Dimension DimensionPipe(Document doc, DimensionType dimensionType, Line dimensionLine, Pipe pipe)
        {
            ReferenceArray referenceArray = new ReferenceArray();
            Line line = GeometryUtils.GetGeometryLineOfPipe(doc.ActiveView, pipe);
            referenceArray.Append(line.GetEndPointReference(0));
            referenceArray.Append(line.GetEndPointReference(1));

            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        }


        

        /// <summary>
        /// Retrieves the center point of the element based on whether or not it has a location point or a location curve.
        /// </summary>
        /// <param name="element">a piping element.</param>
        /// <returns>The center point of the element.</returns>
        public static XYZ GetOriginOfElement(Element element)
        {
            if (element == null) return null;
            Location location = element.Location;
            if (location is LocationPoint locationPoint) return locationPoint.Point;
            if (location is LocationCurve locationCurve) return (locationCurve.Curve as Line).Origin;
            return null;
        }

        

        /// <summary>
        /// Creates a line that is parallel with the projected element line and intersects the dimension point. This is where the primary dimension will be placed.
        /// </summary>
        /// <param name="plane">The plane of the active View.</param>
        /// <param name="projectedElementLine">The line ranging from each end of the Pipeline and projected onto the active View plane.</param>
        /// <param name="dimensionPoint">The point that the dimension line intersects and therefore determines the position of the dimension line.</param>
        /// <returns>A line offset from the Pipeline where the primary dimension will be placed.</returns>
        public static Line CreateDimensionLine(Plane plane, Line projectedElementLine, XYZ dimensionPoint)
        {
            Line dimensionLine = Line.CreateUnbound(Utils.GeometryUtils.ProjectPointOntoPlane(plane, dimensionPoint), projectedElementLine.Direction);
            dimensionLine.MakeUnbound();
            return dimensionLine;
        }

       
        /// <summary>
        /// Gets a pseudo-Reference to the center of the piping element. 
        /// This is done by retrieving all of the element's geometry lines that have a valid line style and have an endpoint sharing a position with the center of the piping element.
        /// </summary>
        /// <param name="validStyleIds">An array of valid Line Style Ids for centerlines. </param>
        /// <param name="element">a piping element.</param>
        /// <returns>a Reference to the center of the piping element.</returns>
        public static Reference GetCenterReference(ElementId[] validStyleIds, Element element)
        {
            Line[] instanceLines = GeometryUtils.GetInstanceGeometryObjectsWithStyleIds<Line>(GeometryUtils.GetGeometryOptions(), element, validStyleIds);
            Line[] symbolLines = GeometryUtils.GetSymbolGeometryObjectsWithStyleIds<Line>(GeometryUtils.GetGeometryOptions(), element, validStyleIds);
            if (instanceLines == null || instanceLines.Length == 0 || symbolLines == null || symbolLines.Length == 0) return null;
            XYZ origin = GetOriginOfElement(element);
            int id = -1;
            int endIndex = -1;
            foreach (Line line in instanceLines)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (line.GetEndPoint(i).IsAlmostEqualTo(origin))
                    {
                        id = line.Id;
                        endIndex = i;
                        break;
                    }
                }
                if (id != -1) break;
            }
            if (id == -1 || endIndex == -1) return null;
            return symbolLines.Where(line => line.Id.Equals(id)).FirstOrDefault()?.GetEndPointReference(endIndex);
        }

        public static Reference GetProjectedCenterReference(Options geometryOptions, ElementId[] validStyleIds, Plane plane, Element element)
        {
            Line[] instanceLines = GeometryUtils.GetInstanceGeometryObjectsWithStyleIds<Line>(geometryOptions, element, validStyleIds);
            Line[] symbolLines = GeometryUtils.GetSymbolGeometryObjectsWithStyleIds<Line>(geometryOptions, element, validStyleIds);
            if (instanceLines == null || instanceLines.Length == 0 || symbolLines == null || symbolLines.Length == 0) return null;
            XYZ projectedOrigin = GeometryUtils.ProjectPointOntoPlane(plane, GetOriginOfElement(element));
            int id = -1;
            int endIndex = -1;
            foreach (Line line in instanceLines)
            {
                for (int i = 0; i < 2; i++)
                {
                    XYZ projectedEndPoint = GeometryUtils.ProjectPointOntoPlane(plane, line.GetEndPoint(i));
                    if (projectedEndPoint.IsAlmostEqualTo(projectedOrigin))
                    {
                        id = line.Id;
                        endIndex = i;
                        break;
                    }
                }
                if (id != -1) break;
            }
            if (id == -1 || endIndex == -1) return null;
            return symbolLines.Where(line => line.Id.Equals(id)).FirstOrDefault()?.GetEndPointReference(endIndex);
        }

        /// <summary>
        /// Retrieves the Reference for unused connector of the pipe.
        /// </summary>
        /// <param name="activeView">The active View.</param>
        /// <param name="pipe">a Pipe element with one open connector.</param>
        /// <returns>a Reference to the endpoint of the pipe.</returns>
        public static Reference GetPipeEndReference(View activeView, Pipe pipe)
        {
            if (pipe == null) return null;
            //Gets the position of the open connector of the pipe.
            XYZ position = null;
            foreach (Connector connector in pipe.ConnectorManager.UnusedConnectors)
            {
                position = connector.Origin;
                break;
            }
            if (position == null) return null;
            //Attempts to find an endpoint of the pipes geometry that shares the same position as the open connector.
            Line line = Utils.GeometryUtils.GetGeometryLineOfPipe(activeView, pipe);
            for (int i = 0; i < 2; i++)
            {
                if (line.GetEndPoint(i).IsAlmostEqualTo(position)) return line.GetEndPointReference(i);
            }
            return null;
        }

        public static Reference GetMechanicalEquipmentEndReference(Plane plane, FamilyInstance mechanicalEquipment, FamilyInstance connected)
        {
            return null;
            //Connector connector = ConnectionUtils.TryGetOneSidedConnection(mechanicalEquipment, connected);
            //if (connector == null) return null;
            //return GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(), plane, connector);
        }
    }
    
}
