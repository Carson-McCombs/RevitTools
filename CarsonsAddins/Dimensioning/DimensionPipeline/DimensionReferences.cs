﻿using Autodesk.Revit.DB;
using CarsonsAddins.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CarsonsAddins.Utils.DimensioningUtils;

namespace CarsonsAddins.Settings.Dimensioning.Models
{
    class DimensionReferences
    {
        
        public Element[] orderedElements;
        public ReferenceNode[] nodes;
        private DimensionPreferences dimensionPreferences;
        public DimensionReferences(ElementId[] validStyleIds, View activeView, DimensionPreferences dimensionPreferences, Element[] orderedElements)
        {
            this.orderedElements = orderedElements;
            this.dimensionPreferences = dimensionPreferences;
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
                SetFirstConnector();
                SetLastConnector();
                
            }
            else if (nodes[0].isLinear)
            {
                SetSingleElementConnectors();
            }
            PopulateNodeReferences(validStyleIds, activeView);
            FillAdjacentNodeReferences();
            SetAdjacentNonLinear();
            MoveEdges();
            SubtractFlanges();
        }
        private void SetFirstConnector()
        {
            if (!nodes[0].isLinear) return;
            Connector connector = ConnectionUtils.GetParallelConnector(ConnectionUtils.GetConnectors(orderedElements[0]).Where(con => ConnectionUtils.IsConnectedTo(orderedElements[1], con)).FirstOrDefault());
            nodes[0].firstConnector = connector;
        }
        private void SetLastConnector()
        {
            if (!nodes[nodes.Length - 1].isLinear) return;
            Connector connector = ConnectionUtils.GetParallelConnector(ConnectionUtils.GetConnectors(orderedElements[nodes.Length - 1]).Where(con => ConnectionUtils.IsConnectedTo(orderedElements[nodes.Length - 2], con)).FirstOrDefault());
            nodes[nodes.Length - 1].lastConnector = connector;
        }
        private void SetSingleElementConnectors()
        {
            Connector[] connectors = ConnectionUtils.GetConnectors(orderedElements[0]);
            nodes[0].firstConnector = connectors[0];
            nodes[0].lastConnector = connectors[1];
        }
        private void CreateReferenceNode(int index)
        {
            Element element = orderedElements[index];
            Connector firstConnector = null;
            if (index > 0)
            {
                (Connector, Connector) connection = ConnectionUtils.TryGetConnection(orderedElements[index - 1], element);
                firstConnector = connection.Item2;
                nodes[index - 1].lastConnector = connection.Item1;
            }
            bool isLinear = ConnectionUtils.IsLinearElement(element);
            bool isStart = index == 0;
            bool isEnd = index == orderedElements.Length - 1;
            bool isNonConnector = IsNonConnector(element);
            FlangeDimensionMode mode = dimensionPreferences.GetMode(element);
            nodes[index] = new ReferenceNode
            {
                builtInCategory = (BuiltInCategory)element.Category.Id.IntegerValue,
                mode = mode,
                origin = GeometryUtils.GetOrigin(element.Location),
                isStart = isStart,
                isEnd = isEnd,
                isLinear = isLinear,
                adjacentNonLinear = false,
                isNonConnector = isNonConnector,
                ignore = isNonConnector,
                firstConnector = firstConnector
            };
        }
        private bool IsNonConnector(Element element)
        {
            if (element == null) return false;
            if (!(element is FamilyInstance familyInstance)) return false;
            return (familyInstance.Symbol.FamilyName == "Non-Connector");

        }
        private void MoveEdges()
        {
            if (nodes[0].isNonConnector || nodes[0].isFlange)
            {
                int nextIndex = GetAdjacentElementIndex(1, 1, false, false);
                if (nextIndex > 0) 
                {
                    for (int i = 0; i < nextIndex; i++) nodes[i].ignore = true;
                    nodes[nextIndex].isStart = true;
                }
            }
            if (nodes[nodes.Length - 1].isNonConnector || nodes[nodes.Length - 1].isFlange)
            {
                int previousIndex = GetAdjacentElementIndex(nodes.Length - 2, -1, false, false);
                if (previousIndex >= 0)
                {
                    for (int i = nodes.Length - 1; i > previousIndex; i--) nodes[i].ignore = true;
                    nodes[previousIndex].isEnd = true;
                }
            }
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
                    nodes[i].centerReference = FindReference(nodeReferencesWithView[i], plane, origin) ?? FindReference(nodeReferencesWithoutView[i], plane, origin);
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
                    Reference reference = nodes[i].lastReference ?? nodes[i + 1].firstReference;
                    nodes[i].lastReference = reference;
                    nodes[i + 1].firstReference = reference;
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
                    case (FlangeDimensionMode.Exact):
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
        private void IgnoreFlange(int index)
        {
            if (index < 0 || index >= nodes.Length) return;
            nodes[index].firstReference = null;
            nodes[index].lastReference = null;
        }
        private void NegateFlange(int index)
        {
            if (index < 0 || index >= nodes.Length) return;
            nodes[index].firstReference = null;
            nodes[index].lastReference = null;
            RemoveLastReference(index - 1);
            RemoveFirstReference(index + 1);
        }
        private void PartialFlange(int index)
        {
            if (index < 0 || index >= nodes.Length) return;
            else if (nodes[index].isStart || nodes[index].isEnd || NextToNonLinear(index)) IgnoreFlange(index);
            else NegateFlange(index);
        }
        private int GetAdjacentElementIndex(int index, int direction, bool allowNonConnectors, bool allowFlanges)
        {
            if (direction == 0 || index < 0 || index >= nodes.Length) return -1;
            bool passesFlangeFilter = (allowFlanges || (!allowFlanges && !nodes[index].isFlange));
            bool passesNonConnectorFilter = (allowNonConnectors || (!allowNonConnectors && !nodes[index].isNonConnector));
            if (passesFlangeFilter && passesNonConnectorFilter) return index;
            return GetAdjacentElementIndex(index + direction, direction, allowNonConnectors, allowFlanges);
        }
        private bool NextToNonLinear(int index) 
        {
            if (index < 0 || index >= nodes.Length) return false;
            int previousIndex = GetAdjacentElementIndex(index - 1, -1, false, true);
            int nextIndex = GetAdjacentElementIndex(index + 1, 1, false, true);
            if (previousIndex >= 0 && !nodes[previousIndex].isLinear) return true;
            if (nextIndex >= 0 && !nodes[nextIndex].isLinear) return true;
            return false;
        }

        private void RemoveLastReference(int index)
        {
            if (index < 0 || index >= nodes.Length) return;
            if (nodes[index].isNonConnector) RemoveLastReference(index - 1);
            nodes[index].lastReference = null;
        }
        private void RemoveFirstReference(int index)
        {
            if (index < 0 || index >= nodes.Length) return;
            if (nodes[index].isNonConnector) RemoveFirstReference(index + 1);
            nodes[index].firstReference = null;
        }
        public struct ReferenceNode
        {
            public BuiltInCategory builtInCategory;
            public FlangeDimensionMode mode;
            public bool isFlange => mode != FlangeDimensionMode.Default;
            public XYZ origin;
            public bool isStart;
            public bool isEnd;
            public bool isEdge => isStart || isEnd;
            public int referenceCount => ((firstReference != null) ? 1 : 0) + ((lastReference != null) ? 1 : 0) + ((centerReference != null) ? 1 : 0);
            public bool isLinear;
            public bool adjacentNonLinear;
            public bool isNonConnector;
            public bool ignore;
            public Reference firstReference;
            public Reference centerReference;
            public Reference lastReference;
            public Connector firstConnector;
            public Connector lastConnector;
        }
    }

    
}

