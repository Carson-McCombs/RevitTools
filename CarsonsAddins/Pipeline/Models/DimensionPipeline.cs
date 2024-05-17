using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using CarsonsAddins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CarsonsAddins.Utils.DimensioningUtils;
using Autodesk.Revit.UI;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;

namespace CarsonsAddins.Pipeline.Models
{
    public static class DimensionPipeline
    {



        /// <summary>
        /// Lerp Function. Calculates a point that is an "amount" from the "start" point to the "end" point.
        /// </summary>
        /// <param name="start">The starting point XYZ position</param>
        /// <param name="end">The endpoint XYZ position</param>
        /// <param name="amount">The amount to offset from the starting point.</param>
        /// <returns>an XYZ position that has been offset an "amount" from the "start" point to the "end" point.</returns>
        private static XYZ Lerp(XYZ start, XYZ end, double amount)
        {
            return start + (end - start) * amount;
        }


        /// <summary>
        /// Creates a Line based on by taking the primary dimension line and offsetting that line an amount equal to the Dimension Type (text size + 3 * text offset) *  active View scale, towards the original projected element line.
        /// </summary>
        /// <param name="activeView">The active View.</param>
        /// <param name="secondaryDimensionType">The Dimension Type of the secondary Dimension. This should the Dimension Type with the largest text size and text offset ( although generally each Dimension Type within the secondary dimension is equal).</param>
        /// <param name="projectedElementLine">The element line projected onto the active activeView's plane.</param>
        /// <param name="primaryDimensionLine">The dimension line for the primary dimension.</param>
        /// <returns>a secondary dimension line.</returns>
        private static Line CreateSecondaryDimensionLine(View activeView, DimensionType secondaryDimensionType, Line projectedElementLine, Line primaryDimensionLine)
        {
            const double nullTextSize = (1.0 / 96.0); // 1/8"
            const double nullTextOffset = (1.0 / 128.0); // 3/32"

            double textSize = secondaryDimensionType?.get_Parameter(BuiltInParameter.TEXT_SIZE).AsDouble() ?? nullTextSize;
            double textOffset = secondaryDimensionType?.get_Parameter(BuiltInParameter.TEXT_DIST_TO_LINE).AsDouble() ?? nullTextOffset;

            double offset = (textSize + 3 * textOffset) * activeView.Scale;

            IntersectionResult result = primaryDimensionLine.Project(projectedElementLine.Origin);
            double percent = offset / result.Distance;

            return Line.CreateUnbound(Lerp(result.XYZPoint, projectedElementLine.Origin, percent), primaryDimensionLine.Direction);
        }
        public static void CreateDimensions(Document doc, Element[] elements, Element selectedElement, XYZ dimensionPoint) //can only be called after GetPipeLine is called
        {
            if (elements == null) return;
            View activeView = doc.ActiveView;
            Plane plane = GeometryUtils.GetOrCreatePlane(doc);

            DimensionStyles dimensionStyles = DimensionSettingsWindow.DimensionStylesSettings ?? new DimensionStyles();
            ElementId[] validStyleIds = dimensionStyles.centerlineStyles.Select(style => style.Id).ToArray();
            ElementId defaultDimensionTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType);
            DimensionType defaultDimensionType = (defaultDimensionTypeId != ElementId.InvalidElementId) ? doc.GetElement(defaultDimensionTypeId) as DimensionType : default;

            Connector[] connectors = ConnectionUtils.GetConnectors(selectedElement);
            XYZ pointA = connectors[0].Origin;
            XYZ pointB = connectors[1].Origin;


            Line elementLine = Line.CreateBound(pointA, pointB);

            //Projects the endpoints and the element line onto the plane of the active activeView.
            XYZ projectedPointA = GeometryUtils.ProjectPointOntoPlane(plane, pointA);
            XYZ projectedPointB = GeometryUtils.ProjectPointOntoPlane(plane, pointB);
            Line projectedElementLine = Line.CreateBound(projectedPointA, projectedPointB);

            double differenceFromOriginal = elementLine.Length - projectedElementLine.Length;
            //if the element line is not parallel with the active activeView, don't create the secondaryDimension.
            //Originally the comparison was made with the Line.Direction, but there were often inconsistencies due to the precision being spread across 3 variables and the normalization factor.
            //Note: the tolerance allowed is not double.Epsilon because the precision is limited by Revit's coordinate system.
            bool secondaryDimension = (differenceFromOriginal < 0.001 && differenceFromOriginal > -0.001);

            //Retrieves and stores the desired dimension types per element category. This could also be an Dict<BuiltInCategory, DimensionType) + one dimension type for the primary dimension line.

            //Calculates the primary dimension line by creating a line that is parallel with the projected element line and intersects the dimension point.
            Line primaryDimensionLine = CreateDimensionLine(plane, projectedElementLine, dimensionPoint);

            //Calculates the secondary dimension line by creating a line parallel to the primary dimension line but offset a distance based on the dimension type settings
            //( right now it only checks the pipe dimension type settings, but it should choose the type with the largest text and text offset )
            Line secondaryDimensionLine = CreateSecondaryDimensionLine(doc.ActiveView, dimensionStyles.secondaryPipeDimensionType ?? defaultDimensionType, projectedElementLine, primaryDimensionLine);

            //Creates a reference array for the primary dimension based on its end point references.
            ReferenceArray primaryReferenceArray = new ReferenceArray();
            //ReferenceArray secondaryReferenceArray = new ReferenceArray();
            PipingElementReferenceOrderedList referenceSets = new PipingElementReferenceOrderedList(validStyleIds, doc.ActiveView, elements);
            referenceSets.SubtractFlanges();
            ReferenceArray secondaryReferenceArray = new ReferenceArray();
            bool matchesPrimary = true;
            for (int i = 0; i < referenceSets.nodes.Length; i++)
            {
                if (referenceSets.nodes[i].isNonConnector) continue;
                PipingElementReferenceOrderedList.ReferenceNode node = referenceSets.nodes[i];
                                
                
                AddReferences(ref primaryReferenceArray, ref secondaryReferenceArray, node, secondaryDimension);
                if (node.firstReference != null || node.lastReference != null) matchesPrimary = false;
                bool splitDimension = (i == referenceSets.nodes.Length - 1) || (node.mode == PipingElementReferenceOrderedList.FlangeDimensionMode.Ignore || (node.mode == PipingElementReferenceOrderedList.FlangeDimensionMode.Partial && (node.isEdge || node.adjacentNonLinear)));
                if (splitDimension && secondaryReferenceArray.Size > 1) 
                {
                    BuiltInCategory builtInCategory = (node.referenceCount == secondaryReferenceArray.Size) ? node.builtInCategory : BuiltInCategory.OST_PipeCurves;
                    if (!matchesPrimary) doc.Create.NewDimension(activeView, secondaryDimensionLine, secondaryReferenceArray, dimensionStyles.GetSecondaryDimensionType(builtInCategory) ?? defaultDimensionType);
                    secondaryReferenceArray.Clear();
                    matchesPrimary = true;
                }

            }
            doc.Create.NewDimension(activeView, primaryDimensionLine, primaryReferenceArray, dimensionStyles.primaryDimensionType ?? defaultDimensionType);
        }
        private static void AddReferences(ref ReferenceArray primaryReferenceArray, ref ReferenceArray secondaryReferenceArray, PipingElementReferenceOrderedList.ReferenceNode node, bool secondaryDimension)
        {
            if (node.isStart && node.centerReference == null && node.lastReference != null) primaryReferenceArray.Append(node.lastReference);
            if (node.centerReference != null) primaryReferenceArray.Append(node.centerReference);
            if (node.isEnd && node.centerReference == null && node.firstReference != null) primaryReferenceArray.Append(node.firstReference);

            if (secondaryDimension)
            {
                if (node.firstReference != null) secondaryReferenceArray.Append(node.firstReference);
                if (node.centerReference != null) secondaryReferenceArray.Append(node.centerReference);
                if (node.lastReference != null) secondaryReferenceArray.Append(node.lastReference);
            }

        }
    }
    
}

/* OLD CODE
 * /// <summary>
        /// Creates a primary and ( optionally ) a secondary dimension, with the primary dimension ranging from the center points of each of the elements on the end of the Pipeline. And the secondary dimension containing each of the individual dimensions for each element in the Pipeline. Will not allow the secondary dimension to be created if the Pipeline is not parallel with the plane of the active View.
        /// </summary>
        /// <param name="doc">The active Document.</param>
        /// <param name="plane">The plane of the active View</param>
        /// <param name="dimensionPoint">The point where the primary dimension line will intersect ( i.e. determines where the created dimensions will be placed ).</param>
        /// <param name="secondaryDimension">Whether or not the secondary dimensions should be created.</param>
        public static void CreateDimensions(Document doc, Plane plane, Element[] elements, Element selectedElement, XYZ dimensionPoint, bool secondaryDimension) //can only be called after GetPipeLine is called
        {
            if (elements == null) return;
            View activeView = doc.ActiveView;
            DimensionStyles dimensionStyles = DimensionSettingsWindow.DimensionStylesSettings ?? new DimensionStyles();
            ElementId[] validStyleIds = dimensionStyles.centerlineStyles.Select(style => style.Id).ToArray();
            ElementId defaultDimensionTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType);
            DimensionType defaultDimensionType = (defaultDimensionTypeId != ElementId.InvalidElementId) ? doc.GetElement(defaultDimensionTypeId) as DimensionType : default(DimensionType);
            //Creates the element line which goes from the center of the element on each end of Pipeline.

            //XYZ pointA = ConnectionUtils.TryGetConnectionPosition(elements[0], elements[1]) ?? GetOriginOfElement(elements[0]);

            //XYZ pointB = ConnectionUtils.TryGetConnectionPosition(elements[elements.Length - 2], elements[elements.Length - 1]) ?? GetOriginOfElement(elements[elements.Length - 1]);

            Connector[] connectors = ConnectionUtils.GetConnectors(selectedElement);
            XYZ pointA = connectors[0].Origin;
            XYZ pointB = connectors[1].Origin;


            Line elementLine = Line.CreateBound(pointA, pointB);

            //Projects the endpoints and the element line onto the plane of the active activeView.
            XYZ projectedPointA = GeometryUtils.ProjectPointOntoPlane(plane, pointA);
            XYZ projectedPointB = GeometryUtils.ProjectPointOntoPlane(plane, pointB);
            Line projectedElementLine = Line.CreateBound(projectedPointA, projectedPointB);

            double differenceFromOriginal = elementLine.Length - projectedElementLine.Length;
            //if the element line is not parallel with the active activeView, don't create the secondaryDimension.
            //Originally the comparison was made with the Line.Direction, but there were often inconsistencies due to the precision being spread across 3 variables and the normalization factor.
            //Note: the tolerance allowed is not double.Epsilon because the precision is limited by Revit's coordinate system.
            if (differenceFromOriginal > 0.001 || differenceFromOriginal < -0.001) secondaryDimension = false;

            //Retrieves and stores the desired dimension types per element category. This could also be an Dict<BuiltInCategory, DimensionType) + one dimension type for the primary dimension line.

            //Calculates the primary dimension line by creating a line that is parallel with the projected element line and intersects the dimension point.
            Line primaryDimensionLine = CreateDimensionLine(plane, projectedElementLine, dimensionPoint);

            //Calculates the secondary dimension line by creating a line parallel to the primary dimension line but offset a distance based on the dimension type settings
            //( right now it only checks the pipe dimension type settings, but it should choose the type with the largest text and text offset )
            Line secondaryDimensionLine = CreateSecondaryDimensionLine(doc.ActiveView, dimensionStyles.secondaryPipeDimensionType ?? defaultDimensionType, projectedElementLine, primaryDimensionLine);

            //Creates a reference array for the primary dimension based on its end point references.
            ReferenceArray primaryReferenceArray = new ReferenceArray();
            //primaryReferenceArray.Append(GetEndReference(activeView, validStyleIds, elements[0]));
            //primaryReferenceArray.Append(GetEndReference(activeView, validStyleIds, elements[elements.Length - 1]));


            for (int i = 0; i < elements.Length; i++)
            {
                Element element = elements[i];
                try
                {
                    bool isMechanicalEquipment = BuiltInCategory.OST_MechanicalEquipment.Equals((BuiltInCategory)element.Category.Id.IntegerValue);
                    bool isEdge = i == 0 || i == elements.Length - 1;
                    Element connected = i < elements.Length - 1 ? elements[i + 1] : elements[i - 1];
                    DimensionType dimensionType = dimensionStyles.GetSecondaryDimensionType((BuiltInCategory)element.Category.Id.IntegerValue) ?? defaultDimensionType;
                    //As Pipe elements have static geometry, a dedicated function is used to retrieve their references and create the dimension.
                    //Even though the DimensionLinearElement function could be used instead, it would be slower and less efficient.
                    if (ElementCheckUtils.IsPipe(element))
                    {
                        if (isEdge) primaryReferenceArray.Append(GetEndReference(activeView, validStyleIds, element));
                        if (secondaryDimension) DimensionPipe(doc, dimensionType, secondaryDimensionLine, element as Pipe);
                        continue;
                    }
                    else if (ElementCheckUtils.IsPipeFlange(element))
                    {
                        if (!isEdge) continue;
                        primaryReferenceArray.Append(GetFlangeEndReference(plane, element, connected));
                    }
                    else
                    {
                        bool isLinear = ConnectionUtils.IsLinearElement(element);

                        if (isLinear) //Dimensions element based on whether or not it is linear.
                        {
                            if (isEdge) primaryReferenceArray.Append(GetEndReference(activeView, validStyleIds, element));
                            if (secondaryDimension) DimensionLinearElement(doc, dimensionType, plane, secondaryDimensionLine, element as FamilyInstance);
                        }
                        else
                        {

                            if (secondaryDimension && !isMechanicalEquipment) DimensionNonLinearElement(doc, dimensionType, validStyleIds, plane, secondaryDimensionLine, element as FamilyInstance, connected as FamilyInstance, isEdge);
                            if (isMechanicalEquipment) primaryReferenceArray.Append(GetMechanicalEquipmentEndReference(plane, element as FamilyInstance, connected as FamilyInstance));
                            else primaryReferenceArray.Append(GetEndReference(activeView, validStyleIds, element));
                        }
                    }

                }
                catch (Exception ex)
                {
                    TaskDialog.Show("ERROR DIMENSIONING PIPELINE ", ex.Message);
                }


            }
            //Creates the primary dimension
            doc.Create.NewDimension(activeView, primaryDimensionLine, primaryReferenceArray, dimensionStyles.primaryDimensionType ?? defaultDimensionType);
        }

        /// <summary>
        /// Creates a Dimension from each end of a linear element by finding the pseudo-Reference of the connector on each end.
        /// </summary>
        /// <param name="doc">The active Document.</param>
        /// <param name="dimensionType">The Dimension Type of the created dimension</param>
        /// <param name="plane">The plane that the current activeView is located on.</param>
        /// <param name="dimensionLine">The line that the created dimension will be located on.</param>
        /// <param name="familyInstance">The Linear element in the Pipeline that is being dimensioned.</param>
        /// <returns>a newly created Dimension going from each end of the linear element</returns>
        public static Dimension DimensionLinearElement(Document doc, DimensionType dimensionType, Plane plane, Line dimensionLine, Element element)
        {
            Connector[] connectors = ConnectionUtils.GetConnectors(element);
            ReferenceArray referenceArray = new ReferenceArray();
            foreach (Connector connector in connectors)
            {
                //Retrieve a reference to the connector. As connectors themselves don't have a reference,
                //find either a PlanarFace or a Line that has an endpoint that shares that connector's position,
                //and retrieve that endpoint's reference.
                Reference reference = GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(), plane, connector);
                if (reference != null) referenceArray.Append(reference);
            }
            if (referenceArray.Size != 2) return null;
            //Create and return the Dimension based on the pseudo-connector references.
            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        }


        



        /// <summary>
        /// Creates a Dimension from the connector apart of the Pipeline to the centerpoint in the element.
        /// </summary>
        /// <param name="doc">The active Document.</param>
        /// <param name="dimensionType">The Dimension Type of the created dimension</param>
        /// <param name="validStyleIds">Which centerline styles are valid for the purposes of getting a reference to the center of the element.</param>
        /// <param name="plane">The plane that the current activeView is located on.</param>
        /// <param name="dimensionLine">The line that the created dimension will be located on.</param>
        /// <param name="familyInstance">The non-linear element in the Pipeline that is being dimensioned.</param>
        /// <param name="connected">The adjacent element that is within the Pipeline. This is used to retrieve the connector of the main element that is located within the Pipeline.</param>
        /// <returns>a newly created Dimension going from the connector of the non-linear element within the Pipeline to that element's centerpoint.</returns>
        private static Dimension DimensionNonLinearElement(Document doc, DimensionType dimensionType, ElementId[] validStyleIds, Plane plane, Line dimensionLine, Element element, Element connected, bool isEdge)
        {
            ReferenceArray referenceArray = new ReferenceArray();
            //Get the element's connector that is still within the Pipeline
            Connector connector = ConnectionUtils.TryGetOneSidedConnection(element, connected);

            //Retrieve a reference to the connector. As connectors themselves don't have a reference,
            //find either a PlanarFace or a Line that has an endpoint that shares that connector's position,
            //and retrieve that endpoint's reference.
            Reference connectorReference = GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(), plane, connector)
                ?? GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(doc.ActiveView), plane, connector);
            if (connectorReference == null) return null;
            referenceArray.Append(connectorReference);
            //Retrieve a reference to the element's centerpoint. As there is no dedicated reference, all of the geometry lines must be checked to see
            //if there is one with an endpoint that shares a position with the center of the element, and the reference of that endpoint is used instead.
            Reference centerReference = GetCenterReference(validStyleIds, element);
            if (centerReference == null) return null;
            referenceArray.Append(centerReference);
            if (!isEdge)
            {
                Connector adjacent = ConnectionUtils.GetAdjacentConnector(connector);
                if (adjacent == null) return null;
                Reference adjacentReference = GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(), plane, adjacent)
                    ?? GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(doc.ActiveView), plane, adjacent);
                if (adjacentReference == null) return null;
                referenceArray.Append(adjacentReference);
            }





            //Assembly the References into a ReferenceArray and then create and return the Dimension based on those references.
            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        }
 */