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
            nodes[0] = new ReferenceNode
            {
                builtInCategory = (BuiltInCategory)orderedElements[0].Category.Id.IntegerValue,
                mode = GetMode(orderedElements[0]),
                origin = GeometryUtils.GetOrigin(orderedElements[0].Location),
                isStart = true, 
                isEnd = false,
                adjacentNonLinear = false,
                isLinear = ConnectionUtils.IsLinearElement(orderedElements[0]),
                centerReference = (!ElementCheckUtils.IsPipeFlange(orderedElements[0])) ? DimensioningUtils.GetEndReference(activeView, validStyleIds, orderedElements[0]) : null
            };
            nodes[0].adjacentNonLinear = nodes[0].isLinear;
            for (int i = 1; i < orderedElements.Length; i++)
            {
                CreateReferenceNode(validStyleIds, activeView, plane, i);
            }
            SetAdjacentNonLinear();
        }
        private void CreateReferenceNode(ElementId[] validStyleIds, View activeView, Plane plane, int index)
        {
            Element element = orderedElements[index];

            (Connector, Connector) connection = ConnectionUtils.TryGetConnection(orderedElements[index - 1], element);
            Reference currentFirstReference = GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(), plane, connection.Item2)
                ?? GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(activeView), plane, connection.Item2);
            
            nodes[index - 1].lastReference = GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(), plane, connection.Item1)
                ?? GeometryUtils.GetPseudoReferenceOfConnector(GeometryUtils.GetGeometryOptions(activeView), plane, connection.Item1) ?? currentFirstReference;
            bool isLinear = ConnectionUtils.IsLinearElement(element);
            bool isEnd = index == orderedElements.Length - 1;
            bool isFlange = ElementCheckUtils.IsPipeFlange(element);
            nodes[index] = new ReferenceNode
            {
                builtInCategory = (BuiltInCategory)element.Category.Id.IntegerValue,
                mode = GetMode(element),
                origin = GeometryUtils.GetOrigin(element.Location),
                isStart = false,
                isEnd = isEnd,
                isLinear = isLinear,
                adjacentNonLinear = false,
                centerReference = isFlange ? null : (isEnd) ? DimensioningUtils.GetEndReference(activeView, validStyleIds, element) : (!isLinear) ? DimensioningUtils.GetCenterReference(validStyleIds, element) : null,
                firstReference = currentFirstReference ?? nodes[index - 1].lastReference,
        };
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
            
        }
    }

    
}

