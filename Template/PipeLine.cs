using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace CarsonsAddins
{

    /*
     * I am defining a Pipe Line as all of the connected piping elements ( i.e. Pipes, Pipe Fittings, and Pipe Accessories ) between either a Pipe Junction ( i.e. a Bend, Tee, Wye, Cross, etc. ) or end of pipe ( an empty connection ).
     * 
     * This is meant to be a base class that will be called for actions such as movement and dimensioning of piping elements.
     *
     * Note: Currently dimensioning a any junction that is not a bend in Revit does not work. Lateral Tees and Crosses seem to work for dimensioning but not for Routing Preferences. 
     *  Getting the programmatic dimensioning to work is put on delay until either the problem is solved within my current Revit setup or by Revit themselves.
     */
    public class PipeLine
    {
        private ISelectionFilter filter = null;
        private List<Element> elements;
        public List<Element> GetPipeLine(UIDocument uidoc, Pipe pipe)
        {
            elements = new List<Element>();
            filter = new SelectionFilter_PipingElements(true, true, false, true, true, true);
            Connector[] connectors = Util.GetConnectors(pipe);
            AddNextElement_Left(uidoc, connectors[0]);
            elements.Add(pipe);
            AddNextElement_Right(uidoc, connectors[1]);
            
            return elements;
        }
        public List<Element> GetPipeLine(UIDocument uidoc, Pipe pipe, ISelectionFilter filter)
        {
            elements = new List<Element>();
            this.filter = filter;
            Connector[] connectors = Util.GetConnectors(pipe);
            AddNextElement_Left(uidoc, connectors[0]);
            elements.Add(pipe);
            AddNextElement_Right(uidoc, connectors[1]);
            return elements;
        }

        //Recursively adds all the connected piping elements on one side (arbitrarly called the "left" side) of an element
        private void AddNextElement_Left(UIDocument uidoc, Connector current)
        {
            Connector adjacent = Util.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = Util.TryGetConnected(adjacent);
            if (CanContinue(next)) AddNextElement_Left(uidoc, next);
            if (adjacent.IsConnected) elements.Add(next.Owner);
        }

        //Recursively adds all the connected piping elements on one side (arbitrarly called the "right" side) of an element
        private void AddNextElement_Right(UIDocument uidoc, Connector current)
        {
            Connector adjacent = Util.GetAdjacentConnector(current);
            if (adjacent == null) return;
            Connector next = Util.TryGetConnected(adjacent);
            if (adjacent.IsConnected) elements.Add(next.Owner);

            if (CanContinue(next)) AddNextElement_Right(uidoc, next);
            
        }

        //Checks if the Pipe Line extends to the next element, or if the current element is the last one in the Pipe Line.
        private bool CanContinue(Connector next)
        {
            if (next == null) return false;
            if (next.Owner == null) return false;
            if (next.Id.Equals(ElementId.InvalidElementId)) return false;
            if (!next.IsValidObject) return false;
            if (!filter.AllowElement(next.Owner)) return false;
            return true;
        }

        #region WIP

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

        

        private static XYZ GetOriginOfElement(Element element)
        {
            if (element == null) return null;
            Location location = element.Location;
            if (location is LocationPoint locationPoint) return locationPoint.Point;
            if (location is LocationCurve locationCurve) return (locationCurve.Curve as Line).Origin;
            return null;
        }

        //private static Line ChooseGeometryLineByConnectorPosition(XYZ connectorPosition, Line[] lines)
        //{
        //    if (lines == null) return null;

        //    foreach (Line line in lines)
        //    {
        //        XYZ endpointA = line.GetEndPoint(0);
        //        XYZ endpointB = line.GetEndPoint(1);
        //        if (connectorPosition.IsAlmostEqualTo(endpointA) || connectorPosition.IsAlmostEqualTo(endpointB)) return line;
        //    }
        //    return null;
        //}

        //private static Line GetLineWithId(int id, Line[] lines)
        //{
        //    foreach (Line line in lines)
        //    {
        //        if (line.Id.Equals(id))
        //        {
        //            return line;
        //        }
        //    }
        //    return null;
        //}
        private static Line CreateDimensionLine(Plane plane, Line elementLine, XYZ dimensionPoint)
        {
            Line dimensionLine = Line.CreateUnbound(Util.ProjectPointOntoPlane(plane, dimensionPoint), elementLine.Direction);
            dimensionLine.MakeUnbound();
            return dimensionLine;
        }
        private static XYZ Lerp(XYZ start, XYZ end, double amount)
        {
            return start + (end - start) * amount;
        }
        private static Reference GetCenterReference(ElementId[] validStyleIds, Element element)
        {
            Line[] instanceLines = Util.GetInstanceGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), element, validStyleIds);
            Line[] symbolLines = Util.GetSymbolGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), element, validStyleIds);
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
        private static Reference GetPipeEndReference(View activeView, Pipe pipe)
        {
            if (pipe == null) return null;
            XYZ position = null;
            foreach (Connector connector in pipe.ConnectorManager.UnusedConnectors)
            {
                position = connector.Origin;
                break;
            }
            if (position == null) return null;
            Line line = Util.GetGeometryLineOfPipe(activeView, pipe);
            for (int i = 0; i < 2; i++)
            {
                if (line.GetEndPoint(i).IsAlmostEqualTo(position)) return line.GetEndPointReference(i);
            }
            return null;
        }
        private static Line CreateSecondaryDimensionLine(View view, DimensionType secondaryDimensionType, Line elementLine, Line primaryDimensionLine)
        {
            double textSize = secondaryDimensionType.get_Parameter(BuiltInParameter.TEXT_SIZE).AsDouble();
            double textOffset = secondaryDimensionType.get_Parameter(BuiltInParameter.TEXT_DIST_TO_LINE).AsDouble();

            double offset = (textSize + 3 * textOffset) * view.Scale;
            
            IntersectionResult result = primaryDimensionLine.Project(elementLine.Origin);
            double percent = offset / result.Distance;
            return Line.CreateUnbound(Lerp(result.XYZPoint, elementLine.Origin, percent), primaryDimensionLine.Direction);
        }

        private static Reference GetEndReference(View activeView, ElementId[] validStyleIds, Element element)
        {
            if (Util.IsPipe(element)) return GetPipeEndReference(activeView, element as Pipe);
            return GetCenterReference(validStyleIds, element);
        }

        private struct DimensionStyles
        {
            public DimensionType primaryDimensionType;
            public DimensionType secondaryPipeDimensionType;
            public DimensionType secondaryAccessoryDimensionType;
            public DimensionType secondaryFittingDimensionType;
        }
        
        private DimensionType GetElementDimensionType(DimensionStyles styles, Element element)
        {
            if (element == null) return null;
            if (Util.IsPipe(element)) return styles.secondaryPipeDimensionType;
            if (BuiltInCategory.OST_PipeFitting.Equals(element.Category.BuiltInCategory)) return styles.secondaryFittingDimensionType;
            if (Util.IsPipeAccessory(element)) return styles.secondaryAccessoryDimensionType;
            return null;
        }

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

        //Below is an attempt at dimensioning Pipe Lines by retrieving the Geometry Objects of each piping element and compares their direction and endpoint positions to the connectors of their respective elements. Due to the current issues stated above, fixing the below function is put on pause.
        public void CreateDimensionLinesFromReferences(Document doc, Plane plane, XYZ dimensionPoint, bool secondaryDimension) //can only be called after GetPipeLine is called
        {
            if (elements == null) return;
            View activeView = doc.ActiveView;
            ElementId[] validStyleIds = GetCenterlineIds(doc);
            
            XYZ pointA = Util.ProjectPointOntoPlane(plane, GetOriginOfElement(elements[0]));
            XYZ pointB = Util.ProjectPointOntoPlane(plane, GetOriginOfElement(elements[1]));
            Line elementLine = Line.CreateBound(pointA, pointB);
            DimensionStyles dimensionStyles = GetDimensionStyles(doc);

            Line primaryDimensionLine = CreateDimensionLine(plane, elementLine, dimensionPoint);

            
            Line secondaryDimensionLine = CreateSecondaryDimensionLine(doc.ActiveView, dimensionStyles.secondaryPipeDimensionType, elementLine, primaryDimensionLine);

            ReferenceArray primaryReferenceArray = new ReferenceArray();
            primaryReferenceArray.Append(GetEndReference(activeView, validStyleIds, elements[0]));
            primaryReferenceArray.Append(GetEndReference(activeView, validStyleIds, elements[elements.Count - 1]));

            //ElementId pipeAccessoryStyleId = new FilteredElementCollector(doc).WhereElementIsElementType().OfClass(typeof(GraphicsStyle)).OfCategory(BuiltInCategory.OST_PipeAccessory).FirstElementId();

            Dimension primaryDimension = doc.Create.NewDimension(activeView, primaryDimensionLine, primaryReferenceArray, dimensionStyles.primaryDimensionType );
            if (!secondaryDimension) return;
            for (int i = 0; i < elements.Count; i++)
            {
                Element element = elements[i];
                try
                {
                    if (Util.IsPipe(element))
                    {
                        DimensionPipe(doc, dimensionStyles.secondaryPipeDimensionType, secondaryDimensionLine, element as Pipe);
                        continue;
                    }
                    if (Util.IsPipeFlange(element)) continue;
                    bool isLinear = Util.IsLinearElement(element);
                    DimensionType dimensionType = GetElementDimensionType(dimensionStyles, element);
                    if (isLinear)
                    {
                        DimensionLinearElement(doc, dimensionType, validStyleIds, plane, secondaryDimensionLine, element as FamilyInstance);
                    }
                    else
                    {
                        FamilyInstance connected = (i == 0) ? elements[1] as FamilyInstance : elements[elements.Count - 2] as FamilyInstance;
                        DimensionNonLinearElement(doc, dimensionType, validStyleIds, plane, secondaryDimensionLine, element as FamilyInstance, connected);
                    }
                }
                catch (Exception ex)
                { 
                    TaskDialog.Show("ERROR DIMENSIONING PIPELINE ", ex.Message); 
                }

                
            }

        }

        private static Dimension DimensionPipe(Document doc, DimensionType dimensionType, Line dimensionLine, Pipe pipe)
        {
            ReferenceArray referenceArray = new ReferenceArray();
            Line line =  Util.GetGeometryLineOfPipe(doc.ActiveView, pipe);
            referenceArray.Append(line.GetEndPointReference(0));
            referenceArray.Append(line.GetEndPointReference(1));

            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        }

        //private static Dimension DimensionPipeBend(Document doc, DimensionType dimensionType, ElementId[] validStyleIds, Plane plane, Line dimensionLine, FamilyInstance familyInstance, FamilyInstance connected)
        //{
        //    Connector connector = Util.TryGetConnection(familyInstance, connected);
        //    Reference connectorReference = Util.GetPseudoReferenceOfConnector(Util.GetGeometryOptions(), plane, connector);
        //    if (connectorReference == null) connectorReference =  Util.GetPseudoReferenceOfConnector(Util.GetGeometryOptions(doc.ActiveView), plane, connector);
        //    if (connectorReference == null) return null;
        //    Reference centerReference = GetCenterReference(validStyleIds, familyInstance);
        //    if (centerReference == null) return null;
        //    ReferenceArray referenceArray = new ReferenceArray();
        //    referenceArray.Append(centerReference);
        //    referenceArray.Append(connectorReference);
        //    return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        //}
        //This methods uses solely references lines
        //private static Dimension DimensionPipeBendC(Document doc, DimensionType dimensionType, ElementId[] validStyleIds, Plane plane, Line dimensionLine, FamilyInstance familyInstance, FamilyInstance connected)
        //{
        //    Line[] symbolLines = Util.GetSymbolGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), familyInstance, validStyleIds);
        //    Line[] instanceLines = Util.GetInstanceGeometryObjectsWithStyleIds<Line>(Util.GetGeometryOptions(), familyInstance, validStyleIds);

        //    Connector connector = Util.TryGetConnection(familyInstance, connected);
        //    Line instanceLine = ChooseGeometryLineByConnectorPosition(connector.Origin, instanceLines);
        //    Line symbolLine = GetLineWithId(instanceLine.Id, symbolLines);

        //    ReferenceArray referenceArray = new ReferenceArray();
        //    referenceArray.Append(symbolLine.GetEndPointReference(0));
        //    referenceArray.Append(symbolLine.GetEndPointReference(1));
        //    return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        //}
        //private static Dimension DimensionPipeAccessory(Document doc, DimensionType dimensionType, ElementId[] validStyleIds, Line dimensionLine, FamilyInstance familyInstance)
        //{
        //    Connector[] connectors = Util.GetConnectors(familyInstance);
        //    if (connectors.)
        //    ReferenceArray referenceArray = new ReferenceArray();

        //    foreach (Connector connector in connectors)
        //    {
        //        if (connector)
        //        referenceArray.Append()
        //    }

        //    return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        //}
        private static Dimension DimensionLinearElement(Document doc, DimensionType dimensionType, ElementId[] validStyleIds, Plane plane, Line dimensionLine, FamilyInstance familyInstance)
        {
            Connector[] connectors = Util.GetConnectors(familyInstance);
            ReferenceArray referenceArray = new ReferenceArray();
            foreach (Connector connector in connectors)
            {
                Reference reference = Util.GetPseudoReferenceOfConnector(Util.GetGeometryOptions(), plane, connector);
                if (reference != null) referenceArray.Append(reference);
            }
            if (referenceArray.Size != 2) return null;
            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        }
        private static Dimension DimensionNonLinearElement(Document doc, DimensionType dimensionType, ElementId[] validStyleIds, Plane plane, Line dimensionLine, FamilyInstance familyInstance, FamilyInstance connected)
        {
            Connector connector = Util.TryGetConnection(familyInstance, connected);
            Reference connectorReference = Util.GetPseudoReferenceOfConnector(Util.GetGeometryOptions(), plane, connector);
            if (connectorReference == null) connectorReference = Util.GetPseudoReferenceOfConnector(Util.GetGeometryOptions(doc.ActiveView), plane, connector);
            if (connectorReference == null) return null;
            Reference centerReference = GetCenterReference(validStyleIds, familyInstance);
            if (centerReference == null) return null;
            ReferenceArray referenceArray = new ReferenceArray();
            referenceArray.Append(centerReference);
            referenceArray.Append(connectorReference);
            return doc.Create.NewDimension(doc.ActiveView, dimensionLine, referenceArray, dimensionType);
        }
        //private static Dimension DimensionElement()
        #endregion



    }
}
