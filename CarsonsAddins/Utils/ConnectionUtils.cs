using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace CarsonsAddins.Utils
{
    public static class ConnectionUtils
    {
        /// <summary>
        ///  Default: None,
        ///  Bell: Female end of the Pipe.
        ///  Spigot: Male end of the Pipe.
        /// </summary>
        public enum BellOrSpigot { NONE, BELL, SPIGOT };

        public struct EndPrepInfo
        {
            public XYZ position;
            public BellOrSpigot endType;
            public string endPrep;
            public bool isTapped;
            public bool isDomestic;
            public EndPrepInfo(XYZ position, BellOrSpigot endType, string endPrep, bool isTapped, bool isDomestic)
            {
                this.position = position;
                this.endType = endType;
                this.endPrep = endPrep;
                this.isTapped = isTapped;
                this.isDomestic = isDomestic;
            }

            public static EndPrepInfo GetEndPrepByConnector(Connector connector)
            {
                if (connector == null) return new EndPrepInfo(XYZ.Zero, BellOrSpigot.NONE, "NULL", false, false);
                if (!connector.IsConnected) return new EndPrepInfo(connector.Origin, BellOrSpigot.SPIGOT, "PE", false, false);

                Connector adjacent = TryGetConnectedSkipNC(connector);
                if (adjacent == null) return new EndPrepInfo(connector.Origin, BellOrSpigot.SPIGOT, "PE", false, false);

                string[] descriptionArray = adjacent.Description.Split('-', ';');
                string endTypeString = descriptionArray[0].Trim().ToUpper();
                string endPrepString = descriptionArray[1].Trim().ToUpper() ?? "NULL";
                
                switch (endTypeString)
                {
                    case ("B"):
                    case ("BELL"):
                        return new EndPrepInfo(connector.Origin, BellOrSpigot.BELL, endPrepString, false, false);

                    case ("PE"):
                    case ("S"):
                    case ("SPIGOT"):
                        return new EndPrepInfo(connector.Origin, BellOrSpigot.SPIGOT, endPrepString, false, false);

                    default:
                        return new EndPrepInfo(connector.Origin, BellOrSpigot.NONE, endPrepString, false, false);
                }

                
                
            }
            public override string ToString()
            {
                return (endPrep == "PE") ? "PE" : (isTapped ? "T" : "") + (isDomestic ? "D": "") + endPrep;
            }
        }


        

        
        public static bool CheckIfReorder(EndPrepInfo endPrepA, EndPrepInfo endPrepB)
        {
            if (endPrepA.endType.Equals(endPrepB.endType)) // same ending types (i.e. either as bell x bell or pe x pe or none x none)
            {
                if (endPrepA.endPrep == "PE") return true;
                else if (endPrepB.endPrep == "PE") return false;
                else if (endPrepA.endPrep == endPrepB.endPrep && endPrepB.isTapped && !endPrepA.isTapped) return true;
                else return endPrepA.endPrep.CompareTo(endPrepB.endPrep) > 0;

            }
            return ((int)endPrepA.endType > (int)endPrepB.endType); //reorders end preps such that a bell will always be before a spigot and a spigot will always be before a 'none' type
        }

        public static bool CheckIfReorder(EndPrepInfo endPrepA, EndPrepInfo endPrepB, double minDistanceToWallA, double minDistanceToWallB)
        {
            if (endPrepA.endType.Equals(endPrepB.endType)) // same ending types (i.e. either as bell x bell or pe x pe or none x none)
            {
                if (endPrepA.endPrep == endPrepB.endPrep)
                {
                    if (endPrepB.isTapped && !endPrepA.isTapped) return true;
                    return (minDistanceToWallB < minDistanceToWallA);
                }
                if (endPrepA.endPrep == "PE") return true;
                else if (endPrepB.endPrep == "PE") return false;
                else return endPrepA.endPrep.CompareTo(endPrepB.endPrep) > 0;

            }
            return ((int)endPrepA.endType > (int)endPrepB.endType); //reorders end preps such that a bell will always be before a spigot and a spigot will always be before a 'none' type
        }

        public static string GetOrderedCombinedEndPrep(EndPrepInfo endPrepA, EndPrepInfo endPrepB)
        {
            if (CheckIfReorder(endPrepA,endPrepB)) return endPrepB.endPrep + " x " + endPrepA.endPrep;
            return endPrepA.endPrep + " x " + endPrepB.endPrep;
        }


        /// <summary>
        /// Gets the connectors from elementA and elementB, then checks each combination of connectors to see if they are connected. 
        /// </summary>
        /// <param name="elementA">The primary element whose connectors is being checked to see if they connect to elementB.</param>
        /// <param name="elementB">The secondary element which is being connected to.</param>
        /// <returns>Returns the connector from elementA that connects to elementB. Returns null if there is no connection.</returns>
        public static Connector TryGetConnection(FamilyInstance elementA, FamilyInstance elementB)
        {
            Connector[] connectorsA = GetConnectors(elementA.MEPModel.ConnectorManager);
            Connector[] connectorsB = GetConnectors(elementB.MEPModel.ConnectorManager);
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
        /// <returns>an array containing all of the Connector Elements within the ConnectorManager.</returns>
        public static Connector[] GetConnectors(ConnectorManager cm)
        {
            List<Connector> connectors = new List<Connector>();
            foreach (Connector con in cm.Connectors)
            {
                connectors.Add(con);
            }
            return connectors.ToArray();
        }

        /// <summary>
        /// Gets all of the Connector Elements from the Pipe's ConnectorManager.
        /// </summary>
        /// <param name="pipe"></param>
        /// <returns>an array connecting all of the Connector Elements within the provided Pipe's ConnectorManager</returns>
        public static Connector[] GetConnectors(Pipe pipe)
        {
            return GetConnectors(pipe.ConnectorManager);
        }

        /// <summary>
        /// Gets all of the Connector Elements from the FamilyInstace's ConnectorManager.
        /// </summary>
        /// <param name="familyInstance">An MEP Family Instance (i.e. Pipe Fitting or Pipe Accessory).</param>
        /// <returns>an array connecting all of the Connector Elements within the provided Family Instance's ConnectorManager</returns>
        public static Connector[] GetConnectors(FamilyInstance familyInstance)
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
            if (!(connected.Owner is FamilyInstance familyInstance)) return null;
            if (familyInstance.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString().Equals("Non-Connector")) //Non-Connector Pipe Fitting Element - originally called by the ID for the Non-Connector fitting, but it changes from project to project, so using name as a quick fix
            {
                return GetConnectedFamilyInstance(GetAdjacentConnector(connected));
            };
            return connected.Owner as FamilyInstance;
        }

        /// <summary>
        /// Attempts to find the Connected Family Instance with the joining Connector. If there is a "Non-Connector" that is found, instead return the next Family Instance that is connected with the connector that joins to the Non-Connector.
        /// </summary>
        /// <param name="connector">A Connector Element</param>
        /// <returns>A Family Instance and the joining connector that is connected to the provided connector.</returns>
        public static (FamilyInstance, Connector) GetConnectedFamilyInstanceWithConnector(Connector connector)
        {
            if (connector == null) return (null, null);
            Connector connected = TryGetConnected(connector);
            if (connected == null) return (null, null);
            if (connected.Owner == null) return (null, null);
            if (!(connected.Owner is FamilyInstance familyInstance)) return (null, null);
            if (familyInstance.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString().Equals("Non-Connector")) //Non-Connector Pipe Fitting Element - originally called by the ID for the Non-Connector fitting, but it changes from project to project, so using name as a quick fix
            {
                return GetConnectedFamilyInstanceWithConnector(GetAdjacentConnector(connected));
            };
            return (connected.Owner as FamilyInstance, connected);
        }

        /// <summary>
        /// Attempts to find the Connected Family Instance with the joining Connector. If there is a "Non-Connector" that is found, instead return the next connector that joins to the Non-Connector.
        /// </summary>
        /// <param name="connector">A Connector Element</param>
        /// <returns>The joining connector that is connected to the provided connector.</returns>
        public static Connector TryGetConnectedSkipNC(Connector connector)
        {
            if (connector == null) return null;
            Connector connected = TryGetConnected(connector);
            if (connected == null) return null;
            if (connected.Owner == null) return null;
            if (!(connected.Owner is FamilyInstance familyInstance)) return null;
            if (familyInstance.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString().Equals("Non-Connector")) //Non-Connector Pipe Fitting Element - originally called by the ID for the Non-Connector fitting, but it changes from project to project, so using name as a quick fix
            {
                return TryGetConnectedSkipNC(GetAdjacentConnector(connected));
            };
            return connected;
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
        /// Determines which end of a pipe the connector is joined to.
        /// </summary>
        /// <param name="fittingConnector">Connector Element of a Fitting / Union Fitting FamilyInstance.</param>
        /// <returns>BellOrSpigot Enum value representing if the connector is joined to the "Bell" end or "Spigot" end of a pipe.</returns>
        public static BellOrSpigot GetPipeEnd(Connector fittingConnector)
        {
            MEPConnectorInfo connectorInfo = fittingConnector.GetMEPConnectorInfo();
            if (connectorInfo == null) return BellOrSpigot.NONE;
            BellOrSpigot bos = connectorInfo.IsPrimary ? BellOrSpigot.BELL : BellOrSpigot.SPIGOT;
            return bos;
        }
    }

   
}
