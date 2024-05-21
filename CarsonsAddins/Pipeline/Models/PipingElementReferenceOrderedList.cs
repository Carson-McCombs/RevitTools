using Autodesk.Revit.DB;
using CarsonsAddins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace CarsonsAddins.Pipeline.Models
{
    class PipingElementReferenceOrderedList
    {
        public enum FlangeDimensionMode { None, Ignore, Partial, Negate }
        public Element[] orderedElements;
        public ReferenceNode[] nodes;

        public PipingElementReferenceOrderedList(ElementId[] validStyleIds, View activeView, Element[] orderedElements)
        {
            this.orderedElements = orderedElements;
            nodes = new ReferenceNode[orderedElements.Length];
            PopulateNodes(validStyleIds, activeView);
        }

        private void PopulateNodes(ElementId[] validStyleIds, View activeView)
        {
            Plane plane = activeView.SketchPlane.GetPlane();
            for (int i = 0; i < orderedElements.Length; i++)
            {
                CreateReferenceNode(i);
            }
            if (nodes.Length > 1)
            {
                if (nodes[0].isLinear )
                {
                    Connector connector = ConnectionUtils.GetParallelConnector(ConnectionUtils.GetConnectors(orderedElements[0]).Where(con => ConnectionUtils.IsConnectedTo(orderedElements[1], con)).FirstOrDefault());
                    //nodes[0].firstReference = GetPseudoConnectorReference(validStyleIds, activeView, plane, connector);
                    nodes[0].firstConnector = connector;
                }
                if (nodes[nodes.Length - 1].isLinear)
                {
                    Connector connector = ConnectionUtils.GetParallelConnector(ConnectionUtils.GetConnectors(orderedElements[nodes.Length - 1]).Where(con => ConnectionUtils.IsConnectedTo(orderedElements[nodes.Length - 2], con)).FirstOrDefault());
                    //nodes[nodes.Length - 1].lastReference = GetPseudoConnectorReference(validStyleIds, activeView, plane, connector);
                    nodes[0].lastConnector = connector;

                }
            }
            else if (nodes[0].isLinear)
            {
                Connector[] connectors = ConnectionUtils.GetConnectors(orderedElements[0]);
                nodes[0].firstConnector = connectors[0];
                nodes[0].lastConnector = connectors[1];
            }
            PopulateNodeReferences(validStyleIds, activeView);
            FillAdjacentNodeReferences();
            SetAdjacentNonLinear();
        }
        private void CreateReferenceNode(int index)
        {
            Element element = orderedElements[index];
            //Reference currentFirstReference = null;
            Connector firstConnector = null;
            if (index > 0)
            {
                (Connector, Connector) connection = ConnectionUtils.TryGetConnection(orderedElements[index - 1], element);
                firstConnector = connection.Item2;
                nodes[index - 1].lastConnector = connection.Item1;
                //currentFirstReference = GetPseudoConnectorReference(validStyleIds, activeView, plane, connection.Item2);

                //nodes[index - 1].lastReference = GetPseudoConnectorReference(validStyleIds, activeView, plane, connection.Item1) ?? currentFirstReference;
                //currentFirstReference = currentFirstReference ?? nodes[index - 1].lastReference;
            }
            bool isLinear = ConnectionUtils.IsLinearElement(element);
            bool isStart = index == 0;
            bool isEnd = index == orderedElements.Length - 1;
            //bool isFlange = ElementCheckUtils.IsPipeFlange(element);
            nodes[index] = new ReferenceNode
            {
                builtInCategory = (BuiltInCategory)element.Category.Id.IntegerValue,
                mode = GetMode(element),
                origin = GeometryUtils.GetOrigin(element.Location),
                isStart = isStart,
                isEnd = isEnd,
                isLinear = isLinear,
                adjacentNonLinear = false,
                //centerReference = isFlange ? null :  (!isLinear) ? DimensioningUtils.GetProjectedCenterReference(GeometryUtils.GetGeometryOptions(), validStyleIds, plane, element) ?? DimensioningUtils.GetProjectedCenterReference(GeometryUtils.GetGeometryOptions(activeView), validStyleIds, plane, element) : null,
                //firstReference = currentFirstReference,
                firstConnector = firstConnector
        };
        }
        private Reference GetPseudoConnectorReference(ElementId[] validStyleIds, View activeView, Plane plane, Connector connector)
        {
            return GeometryUtils.GetPseudoReferenceOfConnector(validStyleIds, GeometryUtils.GetGeometryOptions(activeView), plane, connector) ??
                GeometryUtils.GetPseudoReferenceOfConnector(validStyleIds, GeometryUtils.GetGeometryOptions(), plane, connector);
        }

        private Reference FindReference(GeometryUtils.XYZWithReference[] xyzReferences, Plane plane, XYZ xyz)
        {
            Reference reference = xyzReferences.Where(xyzReference => xyz.IsAlmostEqualTo(xyzReference.xyz)).FirstOrDefault().reference;
            if (reference != null) return reference;
            XYZ projectedXYZ = GeometryUtils.ProjectPointOntoPlane(plane, xyz);
            Reference projectedReference = xyzReferences.Where(xyzReference => xyz.IsAlmostEqualTo(GeometryUtils.ProjectPointOntoPlane(plane, xyzReference.xyz))).FirstOrDefault().reference;
            return projectedReference;
        }

        private void PopulateNodeReferences(ElementId[] validStyleIds, View activeView)
        {
            Plane plane = activeView.SketchPlane.GetPlane();
            GeometryUtils.XYZWithReference[][] nodeReferencesWithView = Enumerable.Range(0,nodes.Length).Select(i => GeometryUtils.XYZWithReference.StripGeometryObjectsWithReferences(validStyleIds, GeometryUtils.GetGeometryOptions(activeView), orderedElements[i])).ToArray();
            GeometryUtils.XYZWithReference[][] nodeReferencesWithoutView = Enumerable.Range(0, nodes.Length).Select(i => GeometryUtils.XYZWithReference.StripGeometryObjectsWithReferences(validStyleIds, GeometryUtils.GetGeometryOptions(), orderedElements[i])).ToArray();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].firstConnector != null)
                {
                    XYZ firstConnectorOrigin = nodes[i].firstConnector.Origin;
                    nodes[i].firstReference = FindReference(nodeReferencesWithView[i], plane, firstConnectorOrigin) ?? FindReference(nodeReferencesWithoutView[i], plane, firstConnectorOrigin);
                }
                if (!nodes[i].isLinear)
                {
                    XYZ origin = nodes[i].origin;
                    nodes[i].firstReference = FindReference(nodeReferencesWithView[i], plane, origin) ?? FindReference(nodeReferencesWithoutView[i], plane, origin);
                }
                if (nodes[i].lastConnector != null)
                {
                    XYZ lastConnectorOrigin = nodes[i].lastConnector.Origin;
                    nodes[i].lastReference = FindReference(nodeReferencesWithView[i], plane, lastConnectorOrigin) ?? FindReference(nodeReferencesWithoutView[i], plane, lastConnectorOrigin);
                }

            }
        }
        private void FillAdjacentNodeReferences()
        {
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                if ((nodes[i].firstReference == null) != (nodes[i + 1].lastReference == null))
                {
                    Reference reference = nodes[i].firstReference ?? nodes[i].lastReference;
                    nodes[i].firstReference = reference;
                    nodes[i + 1].lastReference = reference;
                }

            }
        }
        private void SetAdjacentNonLinear()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].isLinear) continue;
                nodes[i].adjacentNonLinear = true;
                if (i > 0) nodes[i - 1].adjacentNonLinear = true;
                if (i < nodes.Length - 1) nodes[i + 1].adjacentNonLinear = true;
            }
        }


        public void SubtractFlanges()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].isNonConnector) continue;
                switch(nodes[i].mode)
                {
                    case (FlangeDimensionMode.None): 
                        break;
                    case (FlangeDimensionMode.Ignore):
                        IgnoreFlange(i); 
                        break;
                    case (FlangeDimensionMode.Partial):
                        PartialFlange(i);
                        break;
                    case (FlangeDimensionMode.Negate):
                        NegateFlange(i);
                        break;

                } 
            }
        }
        private FlangeDimensionMode GetMode(Element element)
        {
            if (!ElementCheckUtils.IsPipeFlange(element)) return FlangeDimensionMode.None;
            
            string familyName = (element as FamilyInstance).Symbol.FamilyName;
            if (familyName.Contains("FLG")) return FlangeDimensionMode.Ignore;
            if (familyName.Contains("MJ") || familyName.Contains("PO")) return FlangeDimensionMode.Negate;
            return FlangeDimensionMode.Partial;
        }
        private void IgnoreFlange(int index)
        {
            if (index < 0 && index >= nodes.Length) return;
            nodes[index].firstReference = null;
            nodes[index].lastReference = null;
        }
        private void NegateFlange(int index)
        {
            if (index < 0 && index >= nodes.Length) return;
            nodes[index].firstReference = null;
            nodes[index].lastReference = null;
            RemoveLastReference(index - 1);
            RemoveFirstReference(index + 1);
        }
        private void PartialFlange(int index)
        {
            if (index < 0 && index >= nodes.Length) return;
            if (nodes[index].isStart || nodes[index].isEnd || NextToNonLinear(index)) IgnoreFlange(index);
            else NegateFlange(index);
        }
        private bool NextToNonLinear(int index) => IsPreviousNonLinear(index) || IsNextNonLinear(index);
        private bool IsNextNonLinear(int index)
        {
            if (index < 0 || index >= nodes.Length - 1) return false;
            if (nodes[index + 1].isNonConnector) return IsNextNonLinear(index + 1);
            return !nodes[index + 1].isLinear;
        }
        private bool IsPreviousNonLinear(int index)
        {
            if (index < 1 || index > nodes.Length - 1) return false;
            if (nodes[index - 1].isNonConnector) return IsNextNonLinear(index - 1);
            return !nodes[index - 1].isLinear;
        }
        private void RemoveLastReference(int index)
        {
            if (index < 0 && index >= nodes.Length) return;
            if (nodes[index].isNonConnector) RemoveLastReference(index - 1);
            nodes[index].lastReference = null;
        }
        private void RemoveFirstReference(int index)
        {
            if (index < 0 && index >= nodes.Length) return;
            if (nodes[index].isNonConnector) RemoveFirstReference(index + 1);
            nodes[index].firstReference = null;
        }
        public struct ReferenceNode
        {
            //public ElementCheckUtils.PipingCategory pipingCategory;
            public BuiltInCategory builtInCategory;
            public FlangeDimensionMode mode;
            public XYZ origin;
            public bool isStart;
            public bool isEnd;
            public bool isEdge => isStart || isEnd;
            public int referenceCount => ((firstReference != null) ? 1 : 0) + ((lastReference != null) ? 1 : 0) + ((centerReference != null) ? 1 : 0);
            public bool isLinear;
            public bool adjacentNonLinear;
            public bool isNonConnector;
            public Reference firstReference;
            public Reference centerReference;
            public Reference lastReference;
            public Connector firstConnector;
            public Connector lastConnector;
        }
    }

    
}

