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
        /// Retrieves an array of Line style Ids where each style's name must be either "zLines" or "Center line". This is quite static now, but will be able to be adjusted in future revisions. 
        /// </summary>
        /// <param name="doc">The active Document.</param>
        /// <returns>an array of ElementIds corresponding to valid centerline line styles.</returns>
        private ElementId[] GetCenterlineIds(Document doc)
        {
            List<Element> centerlineStyleElements = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)).ToList();
            List<ElementId> centerlineStyleIds = new List<ElementId>();
            for (int i = 0; i < centerlineStyleElements.Count; i++)
            {
                if (centerlineStyleElements[i].Name.Equals("zLines") || centerlineStyleElements[i].Name.Equals("Center line")) centerlineStyleIds.Add(centerlineStyleElements[i].Id);
            }
            return centerlineStyleIds.ToArray();
        }

        
        /// <summary>
        /// Retrieves the center point of the element based on whether or not it has a location point or a location curve.
        /// </summary>
        /// <param name="element">a piping element.</param>
        /// <returns>The center point of the element.</returns>
        private static XYZ GetOriginOfElement(Element element)
        {
            if (element == null) return null;
            Location location = element.Location;
            if (location is LocationPoint locationPoint) return locationPoint.Point;
            if (location is LocationCurve locationCurve) return (locationCurve.Curve as Line).Origin;
            return null;
        }

        private static XYZ TryGetConnectionPosition(Element element, Element other)
        {
            Connector connector = ConnectionUtils.TryGetConnection(element, other);
            return connector?.Origin;
        }

        /// <summary>
        /// Creates a line that is parallel with the projected element line and intersects the dimension point. This is where the primary dimension will be placed.
        /// </summary>
        /// <param name="plane">The plane of the active View.</param>
        /// <param name="projectedElementLine">The line ranging from each end of the Pipeline and projected onto the active View plane.</param>
        /// <param name="dimensionPoint">The point that the dimension line intersects and therefore determines the position of the dimension line.</param>
        /// <returns>A line offset from the Pipeline where the primary dimension will be placed.</returns>
        private static Line CreateDimensionLine(Plane plane, Line projectedElementLine, XYZ dimensionPoint)
        {
            Line dimensionLine = Line.CreateUnbound(Utils.GeometryUtils.ProjectPointOntoPlane(plane, dimensionPoint), projectedElementLine.Direction);
            dimensionLine.MakeUnbound();
            return dimensionLine;
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
        /// Gets a pseudo-Reference to the center of the piping element. 
        /// This is done by retrieving all of the element's geometry lines that have a valid line style and have an endpoint sharing a position with the center of the piping element.
        /// </summary>
        /// <param name="validStyleIds">An array of valid Line Style Ids for centerlines. </param>
        /// <param name="element">a piping element.</param>
        /// <returns>a Reference to the center of the piping element.</returns>
        private static Reference GetCenterReference(ElementId[] validStyleIds, Element element)
        {
            Line[] instanceLines = GeometryUtils.GetInstanceGeometryObjectsWithStyleIds<Line>(Utils.GeometryUtils.GetGeometryOptions(), element, validStyleIds);
            Line[] symbolLines = GeometryUtils.GetSymbolGeometryObjectsWithStyleIds<Line>(Utils.GeometryUtils.GetGeometryOptions(), element, validStyleIds);
            if (instanceLines == null || symbolLines == null) return null;
            if (instanceLines.Length == 0 || symbolLines.Length == 0) return null;
            XYZ origin = GetOriginOfElement(element);
            int id = -1;
            int endIndex = -1;
            foreach(Line line in instanceLines)
            {
                for(int i = 0; i < 2; i++)
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
            
            return symbolLines.Where(line => line.Id.Equals(id)).FirstOrDefault().GetEndPointReference(endIndex);
        }


        /// <summary>
        /// Retrieves the Reference for unused connector of the pipe.
        /// </summary>
        /// <param name="activeView">The active View.</param>
        /// <param name="pipe">a Pipe element with one open connector.</param>
        /// <returns>a Reference to the endpoint of the pipe.</returns>
        private static Reference GetPipeEndReference(View activeView, Pipe pipe)
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

        private static Reference GetMechanicalEquipmentEndReference(Plane plane, FamilyInstance mechanicalEquipment, FamilyInstance connected)
        {
            Connector connector = ConnectionUtils.TryGetConnection(mechanicalEquipment, connected);
            if (connector == null) return null;
            return GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(), plane, connector);
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
        /// Retrieves a reference to the end point of a Piping Element. Meant to be used when dimensioning to a end of the Pipeline
        /// </summary>
        /// <param name="activeView">The active View.</param>
        /// <param name="validStyleIds">The ElementIds corresponding to the valid centerline line styles.</param>
        /// <param name="element">a Piping Element</param>
        /// <returns>a Reference corresponding to the endpoint of the Piping Element.</returns>
        private static Reference GetEndReference(View activeView, ElementId[] validStyleIds, Element element)
        {
            if (ElementCheckUtils.IsPipe(element)) return GetPipeEndReference(activeView, element as Pipe);
            return GetCenterReference(validStyleIds, element);
        }
        /// <summary>
        /// A list of static Dimension Types corresponding to each element category within the Pipeline and one for the Primary Dimension.
        /// </summary>
        private struct DimensionStyles
        {
            public DimensionType primaryDimensionType;
            public DimensionType secondaryPipeDimensionType;
            public DimensionType secondaryAccessoryDimensionType;
            public DimensionType secondaryFittingDimensionType;
        }
        
        /// <summary>
        /// Retrives the Dimension Type based on the element category.
        /// </summary>
        /// <param name="styles">The Pipeline DimensionStyles.</param>
        /// <param name="element">A Piping Element.</param>
        /// <returns>The Dimension Type corresponding to the selected piping element</returns>
        private DimensionType GetElementDimensionType(DimensionStyles styles, Element element)
        {
            if (element == null) return null;
            if (ElementCheckUtils.IsPipe(element)) return styles.secondaryPipeDimensionType;
            if (BuiltInCategory.OST_PipeFitting.Equals(element.Category.BuiltInCategory)) return styles.secondaryFittingDimensionType;
            if (ElementCheckUtils.IsPipeAccessory(element)) return styles.secondaryAccessoryDimensionType;
            return null;
        }
        /// <summary>
        /// Retrieves the Dimension Types for the Pipeline
        /// </summary>
        /// <param name="doc">The active Document</param>
        /// <returns>A Struct containing the Dimension Types for each element category within the Pipeline.</returns>
        private static DimensionStyles GetDimensionStyles(Document doc)
        {
            DimensionStyles dimensionStyles = new DimensionStyles();
            DimensionType[] dimensionTypes = new FilteredElementCollector(doc).WhereElementIsElementType().OfClass(typeof(DimensionType)).ToElements().Cast<DimensionType>().ToArray();

            foreach (DimensionType dimensionType in dimensionTypes)
            {
                switch (dimensionType.Name)
                {
                    case ("C & B"): dimensionStyles.primaryDimensionType = dimensionType; break;
                    case ("C & B PIPE w/dot"): dimensionStyles.secondaryPipeDimensionType = dimensionType; break;
                    case ("C & B FITTING w/dot"): 
                        dimensionStyles.secondaryFittingDimensionType = dimensionType;
                        dimensionStyles.secondaryAccessoryDimensionType = dimensionType;
                        break;
                    default: break;
                }
            }

            return dimensionStyles;

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
            ElementId[] validStyleIds = GetCenterlineIds(doc);

            //Creates the element line which goes from the center of the element on each end of Pipeline.

            XYZ pointA = TryGetConnectionPosition(elements[0], elements[1]) ?? GetOriginOfElement(elements[0]);

            XYZ pointB = TryGetConnectionPosition(elements[elements.Length - 2], elements[elements.Length - 1]) ?? GetOriginOfElement(elements[elements.Length - 1]);
            Line elementLine = Line.CreateBound(pointA, pointB);

            //Projects the endpoints and the element line onto the plane of the active activeView.
            XYZ projectedPointA = GeometryUtils.ProjectPointOntoPlane(plane, pointA);
            XYZ projectedPointB = GeometryUtils.ProjectPointOntoPlane(plane, pointB);
            Line projectedElementLine = Line.CreateBound(projectedPointA, projectedPointB);
            
            //if the element line is not parallel with the active activeView, don't create the secondaryDimension.
            if (!elementLine.Direction.IsAlmostEqualTo(projectedElementLine.Direction, 0.001)) secondaryDimension = false;

            //Retrieves and stores the desired dimension types per element category. This could also be an Dict<BuiltInCategory, DimensionType) + one dimension type for the primary dimension line.
            DimensionStyles dimensionStyles = GetDimensionStyles(doc);

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
                    bool isMechanicalEquipment = BuiltInCategory.OST_MechanicalEquipment.Equals(element.Category.BuiltInCategory);
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
                        DimensionType dimensionType = GetElementDimensionType(dimensionStyles, element);
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

        private static Reference GetFlangeEndReference(Plane plane, Element flange, Element connected)
        {
            if (flange == null || connected == null) return null;
            Connector connector = ConnectionUtils.TryGetConnection(flange, connected);
            if (connector == null) return null;
            Connector adjacent = ConnectionUtils.GetAdjacentConnector(connector);
            if (adjacent == null) return null;
            return GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(), plane, adjacent);
        }


        /// <summary>
        /// Creates a Dimension from each end of a pipe. *Note: DimensionLinearElement could be called instead, but as a pipe has static geometry, a dedicated function ( that should be faster than the generic function ) was used instead.
        /// </summary>
        /// <param name="doc">The active Document.</param>
        /// <param name="dimensionType">The Dimension Type of the created dimension</param>
        /// <param name="dimensionLine">The line that the created dimension will be located on.</param>
        /// <param name="pipe">The pipe element within the Pipeline that is being dimensioned.</param>
        /// <returns>a newly created Dimension going from each end of the pipe element</returns>
        private static Dimension DimensionPipe(Document doc, DimensionType dimensionType, Line dimensionLine, Pipe pipe)
        {
            ReferenceArray referenceArray = new ReferenceArray();
            Line line =  GeometryUtils.GetGeometryLineOfPipe(doc.ActiveView, pipe);
            referenceArray.Append(line.GetEndPointReference(0));
            referenceArray.Append(line.GetEndPointReference(1));

            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
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
        private static Dimension DimensionLinearElement(Document doc, DimensionType dimensionType, Plane plane, Line dimensionLine, Element element)
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
