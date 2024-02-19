﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static CarsonsAddins.PipeEndPrepWindow;

namespace CarsonsAddins
{
    public enum BellOrSpigot { NONE, BELL, SPIGOT };

    /*
     * The Util class is an assortment of utility functions that are meant to be used and referenced in multiple commands and other parts of the addin.
     * 
     *  Note: In the future, this class should probably be split into seperate classes ( probably one class per region )
     */
    public class Util
    {
        private static int minimumDiameterGauged = 14;

        /// <summary>
        /// Retrieves the image at the file location as a Resource Stream and converts it to a Bitmap. This is used to save images and icons to the compiled addin.
        /// </summary>
        /// <param name="assembly">The Addin Assembly</param>
        /// <param name="imagePath">The filepath the image is located</param>
        /// <returns>The image as a Bitmap</returns>
        public static BitmapSource GetImage(Assembly assembly, string imagePath)
        {
            try
            {
                Stream s = assembly.GetManifestResourceStream(imagePath);
                return BitmapFrame.Create(s);
            }
            catch
            {
                return null;
            }
        }

        #region PipingDBTools

        /// <summary>
        /// Retrieves all of the Elements with the ElementType of "PipeType" within the Revit DB
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the PipeType Families (i.e. Generic, Flanged, Victaulic, etc. )</returns>
        public static List<Element> GetAllPipeTypeFamilies(Document doc)
        {
           return new FilteredElementCollector(doc).OfClass(typeof(PipeType)).ToElements() as List<Element>;
        }

        /// <summary>
        /// Calls the GetAllPipeTypeFamilies function and then returns a list of all the PipeType names
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the PipeType Family Names</returns>
        public static List<string> GetAllPipeTypeNames(Document doc) 
        { 
            return (List<string>)GetAllPipeTypeFamilies(doc).Select(ps => ps.Name); 
        }

        /// <summary>
        /// Retrieves all of the Elements within the PipingSystem category. This returns all of the instances of each Piping System 
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the Pipe System instances ( i.e. for the BYPASS Piping System, it would return BYP1, BYP2, BYP3, etc. )</returns>
        public static List<Element> GetAllPipeSystemInstances(Document doc)
        {
            return new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipingSystem).ToElements() as List<Element>;
        }

        /// <summary>
        /// Retrieves all of the ElementTypes within the PipingSystem category. This returns all of the Piping System ( i.e. AIR INTAKE, BYPASS, etc. )
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the Pipe System Families ( i.e. AIR INTAKE, BYPASS, etc. )</returns>
        public static List<Element> GetAllPipeSystemFamilies(Document doc)
        {
            return new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipingSystem).WhereElementIsElementType().ToElements() as List<Element>;
        }

        /// <summary>
        /// Calls the GetAllPipeSystemFamilies function and then returns a list of all the Pipe System names
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the Pipe System Family Names ( i.e. "AIR INTAKE", "BYPASS", etc. )</returns>
        public static List<string> GetAllPipeSystemNames(Document doc) { return (List<string>)GetAllPipeSystemFamilies(doc).Select(ps => ps.Name);}

        /// <summary>
        /// Retrieves all list of all of the Pipe Fitting Families within the Revit DB
        /// </summary>
        /// <param name="doc">The active Revit Document</param>
        /// <returns>a List containing all of the Pipe Fitting Families ( i.e. Flange, TR-Flex Bell, MJ Bell, etc. )</returns>
        public static List<Element> GetAllPipeFittingFamilies(Document doc)
        {
            return new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipeFitting).WhereElementIsElementType().ToElements() as List<Element>;
        }

        ///<summary>
        ///Calls the GetAllPipeFittingFamilies function and then returns a list of all the Pipe Fitting Family Names
        ///</summary>
        public static List<string> GetAllPipeFittingFamilyNames(Document doc) 
        { 
            return (List<string>)GetAllPipeFittingFamilies(doc).Select(ps => ps.Name); 
        }

        #endregion


        #region PipingConnectionTools

        /// <summary>
        /// Gets the connectors from elementA and elementB, then checks each combination of connectors to see if they are connected. 
        /// </summary>
        /// <param name="elementA">The primary element whose connectors is being checked to see if they connect to elementB.</param>
        /// <param name="elementB">The secondary element which is being connected to.</param>
        /// <returns>Returns the connector from elementA that connects to elementB. Returns null if there is no connection.</returns>
        public static Connector TryGetConnection(FamilyInstance elementA, FamilyInstance elementB)
        {
            List<Connector> connectorsA = GetConnectors(elementA.MEPModel.ConnectorManager);
            List<Connector> connectorsB = GetConnectors(elementB.MEPModel.ConnectorManager);
            foreach (Connector conA in connectorsA)
            {
                foreach (Connector conB in connectorsB)
                {
                    if (conA.IsConnectedTo(conB)) return conA;

                }

            }
            return null;

        }
        /// <summary>
        /// Calls the TryGetConnection function to retrieve the connector from elementA that connects to elementB. Then getting the direction from the center of elementA to the connector.
        /// </summary>
        /// <param name="elementA">The primary element which the direction is centered on.</param>
        /// <param name="elementB">The secondary element which is connected to the first.</param>
        /// <returns>The direction from elementA's center to the connection. Returns null if there is not a connection between the two elements.</returns>
        public static XYZ GetDirectionOfConnection(FamilyInstance elementA, FamilyInstance elementB)
        {
            Connector connector = TryGetConnection(elementA, elementB);
            if (connector == null) return null;
            XYZ origin = (elementA.Location as LocationPoint).Point;
            return (Line.CreateBound(origin, connector.Origin).Direction);
        }

        public static List<Connector> GetConnectors(Pipe pipe) {return GetConnectors(pipe.ConnectorManager); }

        public static List<Connector> GetConnectors(FamilyInstance fitting) { return GetConnectors(fitting.MEPModel.ConnectorManager); }
        public static List<Connector> GetConnectors(ConnectorManager cm)
        {
            List<Connector> connectors = new List<Connector>();
            foreach (Connector con in cm.Connectors)
            {
                connectors.Add(con);
            }
            return connectors;
        }
        
        public static Connector TryGetConnected(Connector connector)
        {
            if (!connector.IsConnected) return null;
            foreach (Connector con in connector.AllRefs)
            {
                if (con.ConnectorType != ConnectorType.End) continue;
                if (connector.Owner.Id.Equals(con.Owner.Id)) continue;

                return con;
            }

            return null;
        }
        public static FamilyInstance GetConnectedFamilyInstance(Connector connector)
        {
            if (connector == null) return null; 
            Connector connected = TryGetConnected(connector);
            if (connected == null) return null;
            if (connected.Owner == null) return null;
            FamilyInstance familyInstance = connected.Owner as FamilyInstance;
            if (familyInstance == null) return null;
            if (familyInstance.Symbol.Family.Id.IntegerValue == 4584135)
            {
                return GetConnectedFamilyInstance(GetAdjacentConnector(connected));
            };
            return connected.Owner as FamilyInstance;
        }
        
        public static Connector GetAdjacentConnector(Connector connector)
        {
            try
            {
                if (connector.ConnectorManager == null) return null;
                foreach (Connector other in connector.ConnectorManager.Connectors)
                {

                    if (connector.Id.Equals(other.Id)) continue;
                    return other;
                }
            }
            catch
            {
                TaskDialog.Show("PEP", "GET ADJACENT CONNECTOR ERROR");
            }

            return null;
        }
        public static bool IsConnectedToBend(Connector connector)
        {
            Connector adjacent = GetAdjacentConnector(connector);
            if (adjacent == null) return false;
            if (!adjacent.IsConnected) return false;
            Connector next = TryGetConnected(adjacent);
            if (next == null) return false;
            if (next.Owner == null) return false;
            if (!(next.Owner is FamilyInstance)) return false;
            if (!BendPartTypes.Contains(GetPartType(next.Owner as FamilyInstance))) return false;
            return true;
        }
        public static BellOrSpigot GetPipeEnd(Pipe pipe, FamilyInstance fitting)
        {
            XYZ offset = GetConnectorManagerCenter(pipe.ConnectorManager) - GetConnectorManagerCenter(fitting.MEPModel.ConnectorManager);
            XYZ direction = offset.Normalize();
            return (fitting.HandOrientation.IsAlmostEqualTo(direction)) ? BellOrSpigot.SPIGOT : BellOrSpigot.BELL;

        }
        public static bool IsGaugedPE(Pipe pipe, Connector connector, string endPrep)
        {
            if (endPrep != "PE") return false;
            if (pipe.Diameter * 12 < minimumDiameterGauged) return false;
            if (!connector.IsConnected) return false;
            Connector next = TryGetConnected(connector);
            if (next == null) return false;
            if (next.Owner == null) return false;

            return IsConnectedToBend(next);
        }
        #endregion
        
        
        
        #region PipingElementDimensioning
        public static Line GetParallelLine(XYZ pointA, XYZ pointB, double offset)
        {
            XYZ perpendicularVector = pointA.CrossProduct(pointB);
            perpendicularVector = perpendicularVector.Normalize();
            perpendicularVector.Multiply(offset);
            return Line.CreateBound(pointA + perpendicularVector, pointB + perpendicularVector);
        }
        //public static Reference[] GetDimensionReferences(View activeView, Element element, XYZ direction)
        //{
        //    if (IsPipe(element)) return GetDimensionReferencesOfPipe(activeView, element);
        //    if (IsPipeAccessory(element)) return null;
        //    if (IsPipeFlange(element)) return null;
        //    if (IsPipeBend(element)) return GetDimensionReferencesOfBend(activeView, element, direction);
        //    return null;

        //}
        public static Options GetGeometryOptions()
        {
            Options options = new Options();
            options.ComputeReferences = true;
            options.IncludeNonVisibleObjects = true;
            return options;
        }
        public static Options GetGeometryOptions(View activeView)
        {
            Options options = new Options();
            options.ComputeReferences = true;
            options.IncludeNonVisibleObjects = true;
            options.View = activeView;
            return options;
        }
        public static T[] GetGeometryObjectFromSymbolGeometry<T>(View activeView, Element element) where T : GeometryObject
        {
            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            List<T> gos = new List<T>();
            foreach (GeometryObject goA in geometry)
            {
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                gos.AddRange(GetGeometryObjectFromGeometry<T>(instance.GetSymbolGeometry()));

            }
            if (gos.Count == 0) return null;
            return gos.ToArray();
        }
        public static T[] GetGeometryObjectFromInstanceGeometry<T>(View activeView, Element element) where T : GeometryObject
        {
            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            List<T> gos = new List<T>();
            foreach (GeometryObject goA in geometry)
            {
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
               gos.AddRange(GetGeometryObjectFromGeometry<T>(instance.GetInstanceGeometry()));

            }
            if (gos.Count == 0) return null;
            return gos.ToArray();
        }
        public static T[] GetGeometryObjectFromGeometry<T>(GeometryElement geometry) where T : GeometryObject
        {
            List<T> geometryObjects = new List<T>();
            foreach (GeometryObject go in geometry)
            {
                if (!(go is Solid)) continue;
                Solid solid = go as Solid;
                if (typeof(Edge).Equals(typeof(T)))
                {
                    foreach (Edge edge in solid.Edges)
                    {
                        geometryObjects.Add(edge as T);
                    }

                }
                else if (typeof(CylindricalFace).Equals(typeof(T)) || typeof(PlanarFace).Equals(typeof(T)) || typeof(RevolvedFace).Equals(typeof(T)))
                {
                    foreach (Face face in solid.Faces)
                    {
                        if (!(face is T)) continue;
                        geometryObjects.Add(face as T);
                    }
                }
            }
            if (geometryObjects.Count == 0) return null;
            return geometryObjects.ToArray();
        }
        public static (PlanarFace,PlanarFace) GetPlanarFaceFromConnector(View activeView, Connector connector)
        {
            if (connector.Owner == null) return (null,null);
            if (!connector.Owner.IsValidObject) return (null,null);
            PlanarFace[] instanceFaces = GetGeometryObjectFromInstanceGeometry<PlanarFace>(activeView, connector.Owner);
            PlanarFace[] symbolFaces = GetGeometryObjectFromSymbolGeometry<PlanarFace>(activeView, connector.Owner);

            for (int i = 0; i < symbolFaces.Length; i++)
            {
                if (instanceFaces[i].Origin.IsAlmostEqualTo(connector.Origin)) return (symbolFaces[i], instanceFaces[i]);
            }
            return (null,null);
        }
        public static Line[] GetGeometryLinesOfBend(View activeView, Element element)
        {

            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            List<Line> references = new List<Line>();
            foreach (GeometryObject goA in geometry)
            {
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                foreach (GeometryObject goB in instance.SymbolGeometry)
                {
                    if (goB is Line /* && goB.GraphicsStyleId.IntegerValue.Equals(636627)*/) references.Add((goB as Line));

                }

            }
            if (references.Count == 0) return null;
            return references.ToArray();
        }
        public static Line[] GetGeometryLinesOfBendB(View activeView, Element element)
        {

            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            List<Line> references = new List<Line>();

            foreach (GeometryObject go in geometry)
            {
                if (go is Line) references.Add(go as Line);

            }
            if (references.Count == 0) return null;
            return references.ToArray();
        }
        public static Line[] GetGeometryLineOfAdjacentFittingsOnBend(View activeView, FamilyInstance bend)
        {
            List<Line> lines = new List<Line>();
            List<Connector> connectors = GetConnectors(bend);
            foreach (Connector connector in connectors)
            {
                if (!connector.IsConnected) continue;
                Connector connected = TryGetConnected(connector);
                if (connected == null) continue;
                if (connected.Owner == null) continue;
                if (!connected.Owner.IsValidObject) continue;
                lines.AddRange(GetGeometryLinesOfBendB(activeView, connected.Owner));
            }
            return lines.ToArray();
        }
        public static Element[] GetConnectedElements(FamilyInstance elem)
        {
            List<Connector> connectors = GetConnectors(elem);
            List<Element> connected = new List<Element>();
            foreach(Connector connector in connectors)
            {
                Connector other = TryGetConnected(connector);
                if (other == null) continue;
                if (other.Owner == null) continue;
                if (!other.Owner.IsValidObject) continue;
                
                connected.Add(other.Owner);
            }
            return connected.ToArray();
        }
        public static Element[] GetConnectedElements(Connector[] connectors)
        {
            List<Element> connected = new List<Element>();
            foreach (Connector connector in connectors)
            {
                Connector other = TryGetConnected(connector);
                if (other == null) continue;
                if (other.Owner == null) continue;
                if (!other.Owner.IsValidObject) continue;

                connected.Add(other.Owner);
            }
            return connected.ToArray();
        }
        public static PlanarFace[] GetPlanarFacesOfBend(View activeView, Element element)
        {
            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            List<PlanarFace> faces = new List<PlanarFace>();
            foreach (GeometryObject goA in geometry)
            {
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance gi = (GeometryInstance)goA;
                foreach (GeometryObject goB in gi.SymbolGeometry)
                {
                    if (!(goB is Solid)) continue;
                    Solid solid = goB as Solid;
                    foreach (Face face in solid.Faces)
                    {
                        if (!(face is PlanarFace)) continue;
                        faces.Add(face as PlanarFace);

                    }
                }
                


            }
            return faces.ToArray();
        }
        public static PlanarFace GetPlanarFaceOfBend(View activeView, Element element, Line line)
        {
            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));            
            foreach (GeometryObject goA in geometry)
            {
                if(!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                foreach (GeometryObject goB in instance.SymbolGeometry)
                {
                    if (!(goB is Solid)) continue;
                    Solid solid = goB as Solid;
                    foreach (Face face in solid.Faces)
                    {
                        if (!(face is PlanarFace)) continue;
                        PlanarFace planarFace = face as PlanarFace;
                        if (line.GetEndPoint(0).IsAlmostEqualTo(planarFace.Origin) || line.GetEndPoint(1).IsAlmostEqualTo(planarFace.Origin)) return planarFace;
                    }
                    
                }
                    

            }
            return null;
        }
        
        public static PlanarFace[] GetGeometryObjectsOfPipe(View activeView, Element element)
        {
            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            List<PlanarFace> faces = new List<PlanarFace>();
            foreach (GeometryObject go in geometry)
            {
                if (!(go is Solid)) continue;
                Solid solid = go as Solid;
                foreach (Face face in solid.Faces)
                {
                    if (!(face is PlanarFace)) continue;
                    if (face is PlanarFace && !face.GraphicsStyleId.Equals(-1)) faces.Add(face as PlanarFace);

                }
            }

            return faces.ToArray();
        }
        public static Line GetGeometryLineOfPipe(View activeView, Element element)
        {
            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            foreach (GeometryObject go in geometry)
            {
                if (go is Line) return (go as Line);
            }

            return null;
        }
        public static Reference[] GetEndPointReferences(Line line)
        {
            Reference refA = line.GetEndPointReference(0);
            Reference refB = line.GetEndPointReference(1);
            return new Reference[2] {refA, refB};
        }
        public static Reference[] GetDimensionReferencesOfFlange(Element element)
        {
            return null;
            //List<Connector> connectors = GetConnectors(element as FamilyInstance);
            //return new Reference[2] { connectors[0]., connectors[1].Origin };
        }


        //public static XYZ[] GetPipeBendDimensionPoints(Element element)
        //{

        //    LocationPoint point = (LocationPoint)element.Location;
        //    return new XYZ[1] { point.Point };
        //}
        //public static XYZ[] GetDimensionPoints(Element element)
        //{

        //    if (IsPipe(element)) return null;
        //    if (IsPipeAccessory(element)) return null;
        //    if (IsPipeFlange(element)) return GetPipeFlangeDimensionPoints(element);
        //    if (IsPipeBend(element)) return GetPipeBendDimensionPoints(element);
        //    return null;
        //}
        //public static XYZ[] GetPipeFlangeDimensionPoints(Element element)
        //{
        //    List<Connector> connectors = GetConnectors(element as FamilyInstance);
        //    return new XYZ[2] { connectors[0].Origin, connectors[1].Origin };
        //}
        //public static XYZ[] GetPipeBendDimensionPoints(Element element)
        //{

        //    LocationPoint point = (LocationPoint)element.Location;
        //    return new XYZ[1] { point.Point };
        //}
        #endregion
        #region PipingElementCheck
        public static PartType GetPartType(FamilyInstance fitting)
        {
            if (fitting == null) return PartType.Undefined;
            if (fitting.Symbol == null) return PartType.Undefined;
            if (fitting.Symbol.Family == null) return PartType.Undefined;
            Parameter param = fitting.Symbol.Family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
            if (param == null) return PartType.Undefined;
            return (PartType)param.AsInteger();
        }
        public static bool IsPipeFlange(Element element)
        {
            if (element == null) return false;
            if (!(element is FamilyInstance)) return false;
            return FlangePartTypes.Contains(GetPartType(element as FamilyInstance));
        }
        public static bool IsPipeBend(Element element)
        {
            if (element == null) return false;
            if (!(element is FamilyInstance)) return false;
            return BendPartTypes.Contains(GetPartType(element as FamilyInstance));
        }
        public static bool IsPipeAccessory(Element element)
        {
            return (BuiltInCategory.OST_PipeAccessory.Equals(element.Category.BuiltInCategory));
        }
        public static bool IsPipe(Element element)
        {
            return (BuiltInCategory.OST_PipeCurves.Equals(element.Category.BuiltInCategory));
        }
        public static readonly List<PartType> FlangePartTypes = new List<PartType>() { PartType.PipeFlange, PartType.MultiPort };
        public static readonly List<PartType> BendPartTypes = new List<PartType>() { PartType.Elbow, PartType.Cross, PartType.Tee, PartType.Wye, PartType.LateralTee, PartType.LateralCross };
#endregion
        public static double GetPipeLength(Pipe pipe)
        {
            return pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
            
        }
        public static XYZ GetConnectorManagerCenter(ConnectorManager cm)
        {
            XYZ origin = XYZ.Zero;
            foreach (Connector con in cm.Connectors)
            {
                origin += con.Origin;
            }
            return origin;
        }
    }

    #region Filters
    public class SelectionFilter_PipeFittingPartType : ISelectionFilter
    {
        private PartType partType = PartType.Undefined;
        public SelectionFilter_PipeFittingPartType(PartType partType)
        {
            this.partType = partType;
        }
        public bool AllowElement(Element elem)
        {
            if (elem == null) return false;
            if (Util.IsPipe(elem)) return false;
            FamilyInstance familyInstance = elem as FamilyInstance;
            PartType pt = Util.GetPartType(familyInstance);
            //TaskDialog.Show("TEST SELECTION", param.AsValueString());
            return (partType.Equals(pt));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
    public class SelectionFilter_Pipe : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return Util.IsPipe(elem);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
    public class SelectionFilter_PipingElements : ISelectionFilter 
    {
        private bool allowPipes = true;
        private bool allowFlanges = true;
        private bool allowBends = true;
        private bool allowAccessories = true;
        public SelectionFilter_PipingElements(bool allowPipes, bool allowFlanges, bool allowBends, bool allowAccessories)
        {
            this.allowPipes = allowPipes;
            this.allowFlanges = allowFlanges;
            this.allowBends = allowBends;
            this.allowAccessories = allowAccessories;
        }

        public bool AllowElement(Element elem)
        {
            if (allowPipes && Util.IsPipe(elem)) { return true; }
            if (allowAccessories && Util.IsPipeAccessory(elem)) return true;
            if (elem.Category.BuiltInCategory.Equals(BuiltInCategory.OST_PipeFitting)) 
            {
                if (!(elem is FamilyInstance)) return false;
                FamilyInstance familyInstance = elem as FamilyInstance;
                PartType partType = Util.GetPartType(familyInstance);
                if (allowFlanges && Util.FlangePartTypes.Contains(partType)) return true;
                if (allowBends && Util.BendPartTypes.Contains(partType)) return true;

            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

    public class SelectionFilter_LinearDimension : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            Dimension dimension = elem as Dimension;
            if (dimension == null) return false;
            return (DimensionShape.Linear.Equals(dimension.DimensionShape));
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }

}
#endregion



#region OLDCODE
//public static List<Connector> GetAllConnectorRefs(Pipe pipe)
//{
//    List<Connector> allConnectorsRefs = new List<Connector>();
//    List<Connector> localConnectors = GetConnectors(pipe);
//    foreach (Connector localConnector in localConnectors)
//    {
//        foreach (Connector con in localConnector.AllRefs)
//        {
//            if (con.ConnectorType != ConnectorType.End) continue;
//            if (pipe.Id.Equals(con.Owner.Id)) continue;

//            allConnectorsRefs.Add(con);
//        }
//    }

//    return allConnectorsRefs;
//}




//public static List<Connector> GetAllConnectorRefs(FamilyInstance fitting)
//{
//    List<Connector> allConnectorsRefs = new List<Connector>();
//    List<Connector> localConnectors = GetConnectors(fitting);
//    foreach (Connector localConnector in localConnectors)
//    {
//        foreach (Connector con in localConnector.AllRefs)
//        {
//            if (con.ConnectorType != ConnectorType.End) continue;
//            if (fitting.Id.Equals(con.Owner.Id)) continue;

//            allConnectorsRefs.Add(con);
//        }
//    }

//    return allConnectorsRefs;
//}
//public static Connector GetNextConnector(Connector connector)
//{
//    try
//    {
//        if (connector.ConnectorManager == null) return null;
//        foreach (Connector nc in connector.ConnectorManager.Connectors)
//        {

//            foreach (Connector con in nc.AllRefs)
//            {
//                if (!con.IsConnected) continue;
//                if (con.Owner == null) continue;
//                if (!con.Owner.IsValidObject) continue;
//                if (!connector.Owner.Id.Equals(con.Owner.Id))
//                {
//                    //TaskDialog.Show("NEXT CONNECTOR", connector.Owner.Name + " -> " + con.Owner.Name);
//                    return con;
//                }
//            }
//        }
//    }
//    catch
//    {
//        TaskDialog.Show("PEP", "GET NEXT CONNECTOR ERROR");
//    }

//    return null;
//}
#endregion