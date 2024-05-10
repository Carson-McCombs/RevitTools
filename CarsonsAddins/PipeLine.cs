using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarsonsAddins.Utils;
using CarsonsAddins.Properties;
using static CarsonsAddins.Utils.DimensioningUtils;

namespace CarsonsAddins
{








    /// <summary>
    /// I am defining a Pipe Line as all of the connected piping elements ( i.e. Pipes, Pipe Fittings, and Pipe Accessories ) between either a Pipe Junction ( i.e. a Bend, Tee, Wye, Cross, etc. ) or end of pipe ( an empty connection ).
    /// This is meant to be a utility class that will be called for actions such as movement and dimensioning of piping elements.
    /// Future revisions of this will include UI to customize settings.
    /// Note: Currently dimensioning a any junction that is not a bend in Revit does not work. Lateral Tees and Crosses seem to work for dimensioning but not for Routing Preferences. 
    /// Getting the programmatic dimensioning to work is put on delay until either the problem is solved within my current Revit setup or by Revit themselves.
    /// </summary>
    public class PipeLine
    {
        readonly private SelectionFilters.SelectionFilter_PipingElements filter = null;
        readonly private Element[] elements;
        public PipeLine(View view, Pipe pipe)
        {
            List<Element> elementList = new List<Element>();
            filter = new SelectionFilters.SelectionFilter_PipingElements(false, true, true, false, true, true, true);
            Connector[] connectors = ConnectionUtils.GetConnectors(pipe);
            AddNextElement_Left(view, connectors[0], ref elementList);
            elementList.Add(pipe);
            AddNextElement_Right(view, connectors[1], ref elementList);
            elements = elementList.ToArray();
        }

        public Element[] GetElements() => elements;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uidoc">The active UIDocument</param>
        /// <param name="pipe">The selected Pipe</param>
        /// <param name="filter">The SelectionFilter</param>
        /// <returns>An array of elements containing all of the elements in the Pipeline.</returns>
        public PipeLine(View view, Pipe pipe, SelectionFilters.SelectionFilter_PipingElements filter)
        {
            this.filter = filter;
            List<Element> elementList = new List<Element>();
            Connector[] connectors = ConnectionUtils.GetConnectors(pipe);
            AddNextElement_Left(view, connectors[0], ref elementList);
            elementList.Add(pipe);
            AddNextElement_Right(view, connectors[1], ref elementList);

            elements = elementList.ToArray();
        }

        /// <summary>
        /// Recursively adds all the connected piping elements on one side (arbitrarly called the "left" side) of an element
        /// </summary>
        /// <param name="uidoc">The active UIDocument</param>
        /// <param name="current">The current connector/ </param>
        private void AddNextElement_Left(View view, Connector current, ref List<Element> elementList)
        {
            Connector adjacent = ConnectionUtils.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = ConnectionUtils.TryGetConnected(adjacent);
            if (CanContinue(view, next)) AddNextElement_Left(view, next, ref elementList);
            if (adjacent.IsConnected && !next.Owner.IsHidden(view)) elementList.Add(next.Owner);
        }

        /// <summary>
        /// Recursively adds all the connected piping elements on one side (arbitrarly called the "right" side) of an element
        /// </summary>
        /// <param name="uidoc">The active UIDocument</param>
        /// <param name="current">The current connector/ </param>
        private void AddNextElement_Right(View view, Connector current, ref List<Element> elementList)
        {
            Connector adjacent = ConnectionUtils.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = ConnectionUtils.TryGetConnected(adjacent);
            if (adjacent.IsConnected && !next.Owner.IsHidden(view)) elementList.Add(next.Owner);

            if (CanContinue(view, next)) AddNextElement_Right(view, next, ref elementList);
            
        }

        /// <summary>
        /// Checks if the Pipe Line extends to the next element, or if the current element is the last one on this end of the Pipeline.
        /// </summary>
        /// <param name="next">The next connector to be checked.</param>
        /// <returns>a boolean value determining if the next connector is still apart of the Pipeline.</returns>
        private bool CanContinue(View view, Connector next)
        {
            if (view == null) return false;
            if (next == null) return false;
            if (next.Owner == null) return false;
            if (next.Id.Equals(ElementId.InvalidElementId)) return false;
            if (!next.IsValidObject) return false;
            return (filter.AllowElement(next.Owner, next));
            
        }


        

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
            double textSize = secondaryDimensionType.get_Parameter(BuiltInParameter.TEXT_SIZE).AsDouble();
            double textOffset = secondaryDimensionType.get_Parameter(BuiltInParameter.TEXT_DIST_TO_LINE).AsDouble();

            double offset = (textSize + 3 * textOffset) * activeView.Scale;
            
            IntersectionResult result = primaryDimensionLine.Project(projectedElementLine.Origin);
            double percent = offset / result.Distance;
           
            return Line.CreateUnbound(Lerp(result.XYZPoint, projectedElementLine.Origin, percent), primaryDimensionLine.Direction);
        }

        /// <summary>
        /// Creates a primary and ( optionally ) a secondary dimension, with the primary dimension ranging from the center points of each of the elements on the end of the Pipeline. And the secondary dimension containing each of the individual dimensions for each element in the Pipeline. Will not allow the secondary dimension to be created if the Pipeline is not parallel with the plane of the active View.
        /// </summary>
        /// <param name="doc">The active Document.</param>
        /// <param name="plane">The plane of the active View</param>
        /// <param name="dimensionPoint">The point where the primary dimension line will intersect ( i.e. determines where the created dimensions will be placed ).</param>
        /// <param name="secondaryDimension">Whether or not the secondary dimensions should be created.</param>
        public void CreateDimensions(Document doc, Plane plane, XYZ dimensionPoint, bool secondaryDimension) //can only be called after GetPipeLine is called
        {
            if (elements == null) return;
            View activeView = doc.ActiveView;
            DimensionStyles dimensionStyles = DimensionSettingsWindow.DimensionStylesSettings ?? new DimensionStyles();
            ElementId[] validStyleIds = dimensionStyles.centerlineStyles.Select(style => style.Id).ToArray();

            //Creates the element line which goes from the center of the element on each end of Pipeline.

            XYZ pointA = ConnectionUtils.TryGetConnectionPosition(elements[0], elements[1]) ?? GetOriginOfElement(elements[0]);

            XYZ pointB = ConnectionUtils.TryGetConnectionPosition(elements[elements.Length - 2], elements[elements.Length - 1]) ?? GetOriginOfElement(elements[elements.Length - 1]);
            Line elementLine = Line.CreateBound(pointA, pointB);

            //Projects the endpoints and the element line onto the plane of the active activeView.
            XYZ projectedPointA = GeometryUtils.ProjectPointOntoPlane(plane, pointA);
            XYZ projectedPointB = GeometryUtils.ProjectPointOntoPlane(plane, pointB);
            Line projectedElementLine = Line.CreateBound(projectedPointA, projectedPointB);
            
            //if the element line is not parallel with the active activeView, don't create the secondaryDimension.
            if (!elementLine.Direction.IsAlmostEqualTo(projectedElementLine.Direction, 0.001)) secondaryDimension = false;

            //Retrieves and stores the desired dimension types per element category. This could also be an Dict<BuiltInCategory, DimensionType) + one dimension type for the primary dimension line.

            //Calculates the primary dimension line by creating a line that is parallel with the projected element line and intersects the dimension point.
            Line primaryDimensionLine = CreateDimensionLine(plane, projectedElementLine, dimensionPoint);

            //Calculates the secondary dimension line by creating a line parallel to the primary dimension line but offset a distance based on the dimension type settings
            //( right now it only checks the pipe dimension type settings, but it should choose the type with the largest text and text offset )
            Line secondaryDimensionLine = CreateSecondaryDimensionLine(doc.ActiveView, dimensionStyles.secondaryPipeDimensionType, projectedElementLine, primaryDimensionLine);

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
                    //As Pipe elements have static geometry, a dedicated function is used to retrieve their references and create the dimension.
                    //Even though the DimensionLinearElement function could be used instead, it would be slower and less efficient.
                    if (ElementCheckUtils.IsPipe(element))                     {
                        if (isEdge) primaryReferenceArray.Append(GetEndReference(activeView, validStyleIds, element));
                        if (secondaryDimension) DimensionPipe(doc, dimensionStyles.secondaryPipeDimensionType, secondaryDimensionLine, element as Pipe);
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
                        DimensionType dimensionType = dimensionStyles.GetSecondaryDimensionType((BuiltInCategory)element.Category.Id.IntegerValue);
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
            doc.Create.NewDimension(activeView, primaryDimensionLine, primaryReferenceArray, dimensionStyles.primaryDimensionType);
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
            Connector connector = ConnectionUtils.TryGetConnection(element, connected);
            
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

    }
}
