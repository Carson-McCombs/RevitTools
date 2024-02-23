using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Newtonsoft.Json.Linq;
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
    /// <summary>
    ///  Default: None,
    ///  Bell: Female end of the Pipe.
    ///  Spigot: Male end of the Pipe.
    /// </summary>
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

        //Checks to determine element type and element properties
        #region PipingElementCheck

        /// <summary>
        /// Gets the Part Type of the MEP Fitting
        /// </summary>
        /// <param name="fitting">Family Instance of a MEP Fitting</param>
        /// <returns>Returns the Part Type of the MEP Fitting. Will return PartType Undefined if the Family Instance is null, the Family Instance Symbol, or Symbol Family is null.</returns>
        public static PartType GetPartType(FamilyInstance fitting)
        {
            if (fitting == null) return PartType.Undefined;
            if (fitting.Symbol == null) return PartType.Undefined;
            if (fitting.Symbol.Family == null) return PartType.Undefined;
            Parameter param = fitting.Symbol.Family.get_Parameter(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
            if (param == null) return PartType.Undefined;
            return (PartType)param.AsInteger();
        }

        /// <summary>
        /// Checks if the Element provided is a Family Instance with the Pipe Fitting Category and the PartType Flange, Union, or Multiport.
        /// </summary>
        /// <param name="element">Element to be checked.</param>
        /// <returns>Returns true if the element is an MEP Flange or Multiport. Returns false otherwise.</returns>

        public static bool IsPipeFlange(Element element)
        {
            if (element == null) return false;
            if (!(element is FamilyInstance)) return false;
            if (!element.Category.Equals(BuiltInCategory.OST_PipeFitting)) return false;
            return FlangePartTypes.Contains(GetPartType(element as FamilyInstance));
        }

        /// <summary>
        /// Checks if the Element provided is a Family Instance with the PartType of a Pipe Bend or Junction (i.e. Tee, Wye, Cross, etc. )
        /// </summary>
        /// <param name="element">Element to be checked.</param>
        /// <returns>Returns true if the element is a Family Instance with the PartType Elbow, Tee, Wye, Lateral Tee, Cross, or Lateral Cross. Returns false otherwise. </returns>
        public static bool IsPipeBend(Element element)
        {
            if (element == null) return false;
            if (!(element is FamilyInstance)) return false;
            return BendPartTypes.Contains(GetPartType(element as FamilyInstance));
        }

        /// <summary>
        /// Checks if the Element provided is a Pipe Accessory with the corresponding Category.
        /// </summary>
        /// <param name="element">Element to be checked.</param>
        /// <returns>Returns true if the Element is a Pipe Accessory. Returns false otherwise.</returns>
        public static bool IsPipeAccessory(Element element)
        {
            return (BuiltInCategory.OST_PipeAccessory.Equals(element.Category.BuiltInCategory));
        }

        /// <summary>
        /// Checks if the Element provided is a Pipe with the corresponding Category.
        /// </summary>
        /// <param name="element">Element to be checked.</param>
        /// <returns>Returns true if the Element is a Pipe. Returns false otherwise.</returns>
        public static bool IsPipe(Element element)
        {
            return (BuiltInCategory.OST_PipeCurves.Equals(element.Category.BuiltInCategory));
        }

        /// <summary>
        /// Part Types that are considered a "Flange" ( Flange, Union, MultiPort ).
        /// </summary>
        public static readonly List<PartType> FlangePartTypes = new List<PartType>() { PartType.PipeFlange, PartType.Union, PartType.MultiPort };

        /// <summary>
        /// Part Types that are considered a "Bend" ( or junction )
        /// </summary>
        public static readonly List<PartType> BendPartTypes = new List<PartType>() { PartType.Elbow, PartType.Cross, PartType.Tee, PartType.Wye, PartType.LateralTee, PartType.LateralCross };
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


        /// <summary>
        /// Gets all of the Connector Elements within the ConnectorManager.
        /// </summary>
        /// <param name="cm">The ConnectorManager whose connectors are being retrieved.</param>
        /// <returns>a List containing all of the Connector Elements within the ConnectorManager.</returns>
        public static List<Connector> GetConnectors(ConnectorManager cm)
        {
            List<Connector> connectors = new List<Connector>();
            foreach (Connector con in cm.Connectors)
            {
                connectors.Add(con);
            }
            return connectors;
        }

        /// <summary>
        /// Gets all of the Connector Elements from the Pipe's ConnectorManager.
        /// </summary>
        /// <param name="pipe"></param>
        /// <returns>a List connecting all of the Connector Elements within the provided Pipe's ConnectorManager</returns>
        public static List<Connector> GetConnectors(Pipe pipe) 
        {
            return GetConnectors(pipe.ConnectorManager); 
        }

        /// <summary>
        /// Gets all of the Connector Elements from the FamilyInstace's ConnectorManager.
        /// </summary>
        /// <param name="familyInstance">An MEP Family Instance (i.e. Pipe Fitting or Pipe Accessory).</param>
        /// <returns>a List connecting all of the Connector Elements within the provided Family Instance's ConnectorManager</returns>
        public static List<Connector> GetConnectors(FamilyInstance familyInstance) 
        { 
            return GetConnectors(familyInstance.MEPModel.ConnectorManager); 
        }
        
        /// <summary>
        /// Attempts to find the Connector Element that is valid and has a physical connection to the connector passed as a parameter.
        /// </summary>
        /// <param name="connector">A Connector Element</param>
        /// <returns>The physically "connected" Connector Element with a valid owner. Returns null if the connector is null or if there are no valid physically connected Connector Elements. </returns>
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

        /// <summary>
        /// Attempts to find the Connected Family Instance. If there is a "Non-Connector" that is found, instead return the next Family Instance that is connected.
        /// </summary>
        /// <param name="connector">A Connector Element</param>
        /// <returns>A Family Instance that is connected to the connector.</returns>
        public static FamilyInstance GetConnectedFamilyInstance(Connector connector)
        {
            if (connector == null) return null; 
            Connector connected = TryGetConnected(connector);
            if (connected == null) return null;
            if (connected.Owner == null) return null;
            FamilyInstance familyInstance = connected.Owner as FamilyInstance;
            if (familyInstance == null) return null;
            if (familyInstance.Name.Equals("Non-Connector")) //Non-Connector Pipe Fitting Element - originally called by the ID for the Non-Connector fitting, but it changes from project to project, so using name as a quick fix
            {
                return GetConnectedFamilyInstance(GetAdjacentConnector(connected));
            };
            return connected.Owner as FamilyInstance;
        }
        
        /// <summary>
        /// Attempts to find an "adjacent" Connector Element. In other words, it finds another Connector Element that shares the same ConnectorManager and Owner.
        /// </summary>
        /// <param name="connector">A Connector Element</param>
        /// <returns>A Connector Element that shares the same ConnectorManager and Owner.</returns>
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
                TaskDialog.Show("MEP CONNECTOR ELEMENT ERROR", "ERROR GETTING ADJACENT CONNECTOR.");
            }

            return null;
        }

        /// <summary>
        /// Checks to see if the connector's "adjacent" Connector Element is connected to an MEP Junction (i.e. a Bend, Tee, Wye, Cross, etc. ).
        /// </summary>
        /// <param name="connector">A Connector Element</param>
        /// <returns>Returns true if the adjacent Connector Element is connected to an MEP Junction, and otherwise false. Returns false also if there is not an adjacent connector, if that adjacent connector is not connected to anything, or if that "next" connector's owner is not a FamilyInstance or is not a valid MEP Junction PartType.</returns>
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


        /// <summary>
        /// Gets the direction from the Pipe to the connected Pipe Fitting and checks to see if it lines up with the Pipe Fitting's "Hand Orientation" to determine if the pipe end is a Spigot-End or a Bell-End.
        /// Spigot-End: Pipe end acts as a "male" connector.
        /// Bell-End: Pipe end acts as a "female" connector.
        /// </summary>
        /// <param name="pipe">A Pipe Element connected to the fitting.</param>
        /// <param name="fitting">A Pipe Fitting Family Instance that is connected to the pipe.</param>
        /// <returns></returns>
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
        
        
        //Will comment this region once the dimensioning issue is fixed.

        #region PipingElementDimensioning
        //public static Line GetParallelLine(XYZ pointA, XYZ pointB, double offset)
        //{
        //    XYZ perpendicularVector = pointA.CrossProduct(pointB);
        //    perpendicularVector = perpendicularVector.Normalize();
        //    perpendicularVector.Multiply(offset);
        //    return Line.CreateBound(pointA + perpendicularVector, pointB + perpendicularVector);
        //}
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

        #region GeneralDimensioning
        public static Line GetDimensionSegmentLine(DimensionSegment segment, XYZ direction)
        {
            if (segment.Value == null) return null;
            XYZ offset = direction.Multiply((double)segment.Value / 2);
            return Line.CreateBound(segment.Origin - offset, segment.Origin + offset); ;
        }

        public static Dictionary<XYZ, (Dimension, DimensionSegment)> ExtractPseudoDimensions(Document doc, Dimension dimension)
        {
            Dictionary<XYZ, (Dimension, DimensionSegment)> dimensionSegmentsByOrigin = new Dictionary<XYZ, (Dimension, DimensionSegment)>();
            XYZ direction = (dimension.Curve as Line).Direction;
            for (int i = 0; i < dimension.Segments.Size; i++)
            {
                DimensionSegment segment = dimension.Segments.get_Item(i);
                dimensionSegmentsByOrigin.Add(segment.Origin, (null, segment));
                Line line = GetDimensionSegmentLine(segment, direction);

                ReferenceArray ary = GetDetailReference(doc, line);
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

        public static ReferenceArray GetDetailReference(Document doc, Line dimensionLine)
        {
            ReferenceArray rf = new ReferenceArray();
            View activeView = doc.ActiveView;

            XYZ perp = Line.CreateBound(dimensionLine.GetEndPoint(0), dimensionLine.GetEndPoint(1)).Direction.CrossProduct(activeView.UpDirection);

            DetailCurve line = doc.Create.NewDetailCurve(activeView, dimensionLine.CreateOffset(0.000001d, perp));

            rf.Append(line.GeometryCurve.GetEndPointReference(0));
            rf.Append(line.GeometryCurve.GetEndPointReference(1));
            return rf;
        }


        public class DimensionAndSegment
        {
            private Dimension dimension = null;
            private DimensionSegment dimensionSegment = null;
            private enum DimensionOrSegmentState { None, Dimension, DimensionSegment }
            private DimensionOrSegmentState state = DimensionOrSegmentState.None;
            
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


        #endregion

        public static double GetPipeLength(Pipe pipe)
        {
            return pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
            
        }

        /// <summary>
        /// Returns the position of the ConnectorManager based on the average of all the Connector Elements' Origins. Not the same as the Element position / location.
        /// </summary>
        /// <param name="cm">A ConnectorManager Instance.</param>
        /// <returns>Average position of all of the Connector Elements within the ConnectorManager.</returns>
        public static XYZ GetConnectorManagerCenter(ConnectorManager cm)
        {
            XYZ origin = XYZ.Zero;
            List<Connector> connectors = GetConnectors(cm);
            foreach (Connector con in connectors)
            {
                origin += con.Origin;
            }
            origin /= connectors.Count;
            return origin;
        }
    }

    #region Filters

    /// <summary>
    /// A Selection Filter that only allows a specific PartType
    /// </summary>
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

    /// <summary>
    /// A Selection Filter that only allows Pipe Elements
    /// </summary>
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


    /// <summary>
    /// A Selection Filter that can be set to allow one, some, or all Piping Elements ( i.e. Pipes, Flanges, Bends/Junctions, and Accessories ).
    /// </summary>
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

