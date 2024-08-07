﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Linq;
using static CarsonsAddins.Utils.GeometryUtils;

namespace CarsonsAddins.Utils
{
    public static class GeometryUtils
    {

        public static Options GetGeometryOptions()
        {
            return new Options()
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true,
                DetailLevel = ViewDetailLevel.Fine
            };
        }
        public static Options GetGeometryOptions(View activeView)
        {
            return new Options()
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true,
                View = activeView
            };
        }

        public static Plane GetOrCreatePlane(Document doc)
        {

            if (doc.ActiveView.SketchPlane == null)
            {
                Plane plane = Plane.CreateByNormalAndOrigin(doc.ActiveView.ViewDirection, doc.ActiveView.Origin);

                SubTransaction sketchplaneTransaction = new SubTransaction(doc);
                sketchplaneTransaction.Start();
                SketchPlane sketchplane = SketchPlane.Create(doc, plane);
                doc.ActiveView.SketchPlane = sketchplane;
                doc.ActiveView.HideElements(new List<ElementId>() { sketchplane.Id });
                sketchplaneTransaction.Commit();
                return plane;
            }
            return doc.ActiveView.SketchPlane.GetPlane();
        }

        public static T[] GetGeometryObjectsFromSolid<T>(Solid solid) where T : GeometryObject
        {
            if (solid == null) return new T[0];
            Type t = typeof(T);
            if (t.Equals(typeof(Face)) || t.Equals(typeof(PlanarFace)) || t.Equals(typeof(CylindricalFace))) return solid.Faces.OfType<T>().ToArray();
            else if (typeof(T).Equals(typeof(Edge))) return solid.Edges.OfType<T>().ToArray();
            return new T[0];
        }

        public static T[] GetGeometryObjectFromSymbolGeometry<T>(Options geometryOptions, Element element) where T : GeometryObject
        {
            if (element == null) return null;
            GeometryElement geometry = element.get_Geometry(geometryOptions);
            List<T> geometryObjects = new List<T>();
            foreach (GeometryObject goA in geometry)
            {
                if (goA is T goT) geometryObjects.Add(goT);
                if (goA is Solid solid) geometryObjects.AddRange(GetGeometryObjectsFromSolid<T>(solid));
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                geometryObjects.AddRange(GetGeometryObjectFromGeometry<T>(instance.GetSymbolGeometry()));

            }
            return geometryObjects.ToArray();
        }
        public static T[] GetGeometryObjectFromInstanceGeometry<T>(Options geometryOptions, Element element) where T : GeometryObject
        {
            if (element == null) return null;
            GeometryElement geometry = element.get_Geometry(geometryOptions);
            List<T> gos = new List<T>();
            foreach (GeometryObject goA
                in geometry)
            {
                if (goA is T goT) gos.Add(goT);
                if (goA is Solid solid) gos.AddRange(GetGeometryObjectsFromSolid<T>(solid));
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                gos.AddRange(GetGeometryObjectFromGeometry<T>(instance.GetInstanceGeometry()));
            }
            return gos.ToArray();
        }
        public static T[] GetGeometryObjectFromGeometry<T>(GeometryElement geometry) where T : GeometryObject
        {
            if (geometry == null) return null;
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
            return geometryObjects.ToArray();
        }
        public static (PlanarFace, PlanarFace) GetPlanarFaceFromConnector(Options geometryOptions, Connector connector)
        {
            if (connector.Owner == null) return (null, null);
            if (!connector.Owner.IsValidObject) return (null, null);
            PlanarFace[] instanceFaces = GetGeometryObjectFromInstanceGeometry<PlanarFace>(geometryOptions, connector.Owner);
            PlanarFace[] symbolFaces = GetGeometryObjectFromSymbolGeometry<PlanarFace>(geometryOptions, connector.Owner);

            for (int i = 0; i < symbolFaces.Length; i++)
            {
                if (instanceFaces[i].Origin.IsAlmostEqualTo(connector.Origin)) return (symbolFaces[i], instanceFaces[i]);
            }
            return (null, null);
        }
        public static Reference GetPseudoReferenceOfConnector(ElementId[] validElementIds, Options geometryOptions, Plane plane, Connector connector)
        {
            return GetExposedPseudoReferenceOfConnectorByFaces(geometryOptions, plane, connector) ??
                GetExposedPseudoReferenceOfConnectorByLines(validElementIds, geometryOptions, plane, connector) ??
                GetPseudoReferenceOfConnectorByFace(geometryOptions, plane, connector) ?? 
                GetPseudoReferenceOfConnector(validElementIds, geometryOptions, plane, connector);
        }
        public static Reference GetPseudoReferenceOfConnectorByFace(Options geometryOptions, Plane plane, Connector connector)
        {
            if (connector == null) return null;
            XYZ projectedOrigin = ProjectPointOntoPlane(plane, connector.Origin);
            if (projectedOrigin == null) return null;
            PlanarFace[] instancePlanarFaces = GetGeometryObjectFromInstanceGeometry<PlanarFace>(geometryOptions, connector.Owner);
            Lookup<int, PlanarFace> symbolPlanarFacesLookup = GetGeometryObjectFromSymbolGeometry<PlanarFace>(geometryOptions, connector.Owner).ToLookup(planarFace => planarFace.Id) as Lookup<int, PlanarFace>;
            PlanarFace instanceConnectorFace = instancePlanarFaces.Where(planarFace => projectedOrigin.IsAlmostEqualTo(ProjectPointOntoPlane(plane, planarFace.Origin), 0.000001)).FirstOrDefault();
            if (instanceConnectorFace == default(PlanarFace)) return null;
            PlanarFace symbolFace = symbolPlanarFacesLookup[instanceConnectorFace.Id].FirstOrDefault();
            Reference referenceConnectorFace = symbolFace.Reference;
            return referenceConnectorFace;
        }
        public static Reference GetPseudoReferenceOfConnectorByLines(ElementId[] validElementIds, Options geometryOptions, Plane plane, Connector connector)
        {
            if (connector == null || validElementIds == null || validElementIds.Length == 0) return null;
            XYZ projectedOrigin = ProjectPointOntoPlane(plane, connector.Origin);
            if (projectedOrigin == null) return null;
            Line[] instanceLines = GetGeometryObjectFromInstanceGeometry<Line>(geometryOptions, connector.Owner).Where(line => validElementIds.Contains(line.GraphicsStyleId)).ToArray();
            Lookup<int, Reference[]> referenceLookup = GetGeometryObjectFromSymbolGeometry<Line>(geometryOptions, connector.Owner).ToLookup(line => line.Id, line => GetEndPointReferences(line)) as Lookup<int, Reference[]>;
            foreach (Line line in instanceLines)
            {
                for (int i = 0 ; i < 2; i++)
                {
                    XYZ endPoint = line.GetEndPoint(i);
                    XYZ projectedEndPoint = ProjectPointOntoPlane(plane, endPoint);
                    if (projectedOrigin.IsAlmostEqualTo(projectedEndPoint) && referenceLookup.Contains(line.Id)) return referenceLookup[line.Id].FirstOrDefault()?[i];
                }
                
            }
            return null;
        }

        public static Reference GetExposedPseudoReferenceOfConnectorByFaces(Options geometryOptions, Plane plane, Connector connector)
        {
            if (connector == null) return null;
            XYZ projectedOrigin = ProjectPointOntoPlane(plane, connector.Origin);
            if (projectedOrigin == null) return null;
            PlanarFace[] faces = GetExposedGeometryObjects<PlanarFace>(geometryOptions, connector.Owner);
            foreach (PlanarFace face in faces)
            {
                XYZ projectedFaceOrigin = ProjectPointOntoPlane(plane, face.Origin);
                if (projectedOrigin.IsAlmostEqualTo(projectedFaceOrigin)) return face.Reference;

            }
            return null;
        }

        public static Reference GetExposedPseudoReferenceOfConnectorByLines(ElementId[] validElementIds, Options geometryOptions, Plane plane, Connector connector)
        {
            if (connector == null || validElementIds == null || validElementIds.Length == 0) return null;
            XYZ projectedOrigin = ProjectPointOntoPlane(plane, connector.Origin);
            if (projectedOrigin == null) return null;
            Line[] lines = GetExposedGeometryObjects<Line>(geometryOptions, connector.Owner).Where(line => validElementIds.Contains(line.GraphicsStyleId)).ToArray();
            foreach (Line line in lines)
            {
                for (int i = 0; i < 2; i++)
                {
                    XYZ endPoint = line.GetEndPoint(i);
                    XYZ projectedEndPoint = ProjectPointOntoPlane(plane, endPoint);
                    if (projectedOrigin.IsAlmostEqualTo(projectedEndPoint)) return line.GetEndPointReference(i);
                }

            }
            return null;
        }



        public static GeometryObject[] GetGeometryLinesOfBend(View activeView, Element element, ElementId validStyleId)
        {
            if (element == null) return null;
            if (ElementId.InvalidElementId.Equals(validStyleId)) return null;
            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            List<GeometryObject> references = new List<GeometryObject>();
            foreach (GeometryObject goA in geometry)
            {
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                foreach (GeometryObject goB in instance.SymbolGeometry)
                {
                    if (!(goB is Line)) continue;
                    if (!ElementId.InvalidElementId.Equals(validStyleId) && (goB.GraphicsStyleId.Equals(validStyleId))) references.Add(goB);
                }

            }
            if (references.Count == 0) return null;
            return references.ToArray();
        }
        public static T GetInstanceGeometryObjectFromId<T>(Options geometryOptions, Element element, int geometryId) where T : GeometryObject
        {
            if (element == null) return null;
            GeometryElement geometry = element.get_Geometry(geometryOptions);
            foreach (GeometryObject goA in geometry)
            {
                if (goA is Solid solid) return GetGeometryObjectsFromSolid<T>(solid).Where(geom => geometryId.Equals(geom.Id)).FirstOrDefault();
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                foreach (GeometryObject goB in instance.GetInstanceGeometry())
                {
                    if (!(goB is T)) continue;
                    if (goB.Id.Equals(geometryId)) return (T)goB;
                }

            }
            return null;
        }
        public static T[] GetExposedGeometryObjects<T>(Options geometryOptions, Element element) where T : GeometryObject
        {
            if (element == null) return null;
            List<T> geometryObjects = new List<T>();
            GeometryElement geometry = element.get_Geometry(geometryOptions);
            foreach (GeometryObject go in geometry)
            {
                if (go is Solid solid) geometryObjects.AddRange(GetGeometryObjectsFromSolid<T>(solid));
                if (go is T goT) geometryObjects.Add(goT);

            }
            return geometryObjects.ToArray();
        }
        public static T GetSymbolGeometryObjectFromId<T>(Options geometryOptions, Element element, int geometryId) where T : GeometryObject
        {
            if (element == null) return null;
            GeometryElement geometry = element.get_Geometry(geometryOptions);
            foreach (GeometryObject goA in geometry)
            {
                if (goA is Solid solid) return GetGeometryObjectsFromSolid<T>(solid).Where(geom => geometryId.Equals(geom.Id)).FirstOrDefault();
                if (geometryId.Equals(goA.Id) && goA is T goTA) return goTA;
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                foreach (GeometryObject goB in instance.SymbolGeometry)
                {

                    if (goB.Id.Equals(geometryId)) return (T)goB;
                }

            }
            return null;
        }
        public static T[] GetSymbolGeometryObjectFromId<T>(Options geometryOptions, Element element, int[] geometryIds) where T : GeometryObject
        {
            if (element == null) return null;
            GeometryElement geometry = element.get_Geometry(geometryOptions);
            List<T> symbolGeometry = new List<T>();
            foreach (GeometryObject goA in geometry)
            {
                if (goA is Solid solid) symbolGeometry.AddRange(GetGeometryObjectsFromSolid<T>(solid).Where(geom => geometryIds.Contains(geom.Id)).ToArray());
                if (geometryIds.Contains(goA.Id) && goA is T goTA) symbolGeometry.Add(goTA);
                if (!(goA is GeometryInstance)) continue;
                GeometryInstance instance = goA as GeometryInstance;
                foreach (GeometryObject goB in instance.SymbolGeometry)
                {

                    if (!geometryIds.Contains(goB.Id)) continue;
                    if (goB is T goTB) symbolGeometry.Add(goTB);
                }

            }
            return symbolGeometry.ToArray();
        }
        public static T[] GetSymbolGeometryObjectsWithStyleIds<T>(Options geometryOptions, Element element, ElementId[] validStyleIds) where T : GeometryObject
        {
            if (element == null) return null;
            if (validStyleIds == null || validStyleIds.Length == 0) return null;
            GeometryElement geometry = element.get_Geometry(geometryOptions);

            List<T> geometryObjects = new List<T>();
            foreach (GeometryObject goA in geometry)
            {
                if (validStyleIds.Contains(goA.GraphicsStyleId) && goA is T goTA) geometryObjects.Add(goTA);
                else if (goA is Solid solid) geometryObjects.AddRange(GetGeometryObjectsFromSolid<T>(solid));

                else if (goA is GeometryInstance)
                {
                    GeometryInstance instance = goA as GeometryInstance;
                    foreach (GeometryObject goB in instance.SymbolGeometry)
                    {

                        if (!validStyleIds.Contains(goB.GraphicsStyleId)) continue;
                        if (goB is T goTB) geometryObjects.Add(goTB);
                    }
                }


            }
            return geometryObjects.ToArray();
        }

        public static T[] GetInstanceGeometryObjectsWithStyleIds<T>(Options geometryOptions, Element element, ElementId[] validStyleIds) where T : GeometryObject
        {
            if (element == null) return null;
            if (validStyleIds == null || validStyleIds.Length == 0) return null;
            GeometryElement geometry = element.get_Geometry(geometryOptions);

            List<T> geometryObjects = new List<T>();
            foreach (GeometryObject goA in geometry)
            {
                if (validStyleIds.Contains(goA.GraphicsStyleId) && goA is T goTA) geometryObjects.Add(goTA);
                if (goA is Solid solid) geometryObjects.AddRange(GetGeometryObjectsFromSolid<T>(solid));
                if (goA is GeometryInstance)
                {
                    GeometryInstance instance = goA as GeometryInstance;
                    foreach (GeometryObject goB in instance.GetInstanceGeometry())
                    {

                        if (!validStyleIds.Contains(goB.GraphicsStyleId)) continue;
                        if (goB is T goTB) geometryObjects.Add(goTB);
                    }
                }


            }
            return geometryObjects.ToArray();
        }
        public static GeometryObject[] BreakdownGeometryElementIntoDimensionableGeometryObjects(ElementId[] validStyleIds, GeometryElement geometryInstance)
        {
            if (geometryInstance == null) return new GeometryObject[0];
            List<GeometryObject> geometryObjects = new List<GeometryObject>();
            foreach (GeometryObject geometryObject in geometryInstance)
            {
                if (geometryObject == null) continue;
                else if (geometryObject is Solid solid) geometryObjects.AddRange(solid.Faces.OfType<PlanarFace>().ToArray());
                else if (geometryObject is Point) geometryObjects.Add(geometryObject);
                else if (geometryObject is Curve && (validStyleIds.Contains(geometryObject.GraphicsStyleId) || validStyleIds == null)) geometryObjects.Add(geometryObject);

            }
            return geometryObjects.ToArray();
        }
        public static GeometryObject[] StripExposedGeometryObjects(ElementId[] validStyleIds, GeometryElement geometryElement)
        {
            if (geometryElement == null) return new GeometryObject[0];
            List<GeometryObject> geometryObjects = new List<GeometryObject>();
            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (geometryObject == null || !geometryObject.IsElementGeometry || geometryObject is GeometryInstance) continue;
                else if (geometryObject is Solid solid) geometryObjects.AddRange(solid.Faces.OfType<PlanarFace>().ToArray());
                else if (geometryObject is Point point && point.Reference != null) geometryObjects.Add(geometryObject);
                else if (geometryObject is Line && (validStyleIds.Contains(geometryObject.GraphicsStyleId) || validStyleIds == null)) geometryObjects.Add(geometryObject);
            }
            return geometryObjects.ToArray();
        }
        public struct XYZWithReference
        {
            public XYZ xyz;
            public Reference reference;
            public XYZWithReference(XYZ xyz, Reference reference)
            {
                this.xyz = xyz;
                this.reference = reference;
            }
            public override string ToString()
            {
                return xyz.ToString() + " -> " + reference.ToString();
            }
            public static XYZWithReference[] StripGeometryObjectsWithReferences(ElementId[] validStyleIds, Options geometryOptions, Element element)
            {

                GeometryElement geometryElement = element.get_Geometry(geometryOptions);
                if (geometryElement == null) return new XYZWithReference[0];
                List<XYZWithReference> geometryObjectWithReferences = new List<XYZWithReference>();
                geometryObjectWithReferences.AddRange(StripExposedGeometryObjectsWithReferences(validStyleIds, geometryOptions, element));
                GeometryInstance geometryInstance = geometryElement.OfType<GeometryInstance>().FirstOrDefault();
                if (geometryInstance == null) return geometryObjectWithReferences.ToArray();

                IdWithXYZ[] idsWithXYZs = IdWithXYZ.BreakdownGeometryObjects(BreakdownGeometryElementIntoDimensionableGeometryObjects(validStyleIds, geometryInstance.GetInstanceGeometry()));
                Lookup<(int, int), IdWithReference> idsWithReferences = IdWithReference.BreakdownGeometryObjects(BreakdownGeometryElementIntoDimensionableGeometryObjects(validStyleIds, geometryInstance.GetSymbolGeometry()))
                    .ToLookup(idWithReference => (idWithReference.id, idWithReference.secondaryId)) as Lookup<(int, int), IdWithReference>;

                XYZWithReference[] xyzsWithReferences = idsWithXYZs.Select(xyzWithRef => new XYZWithReference(xyzWithRef.xyz, idsWithReferences[(xyzWithRef.id, xyzWithRef.secondaryId)]
                    .Where(idWithRef => xyzWithRef.secondaryId == idWithRef.secondaryId).FirstOrDefault().reference)).ToArray();
                return xyzsWithReferences;
            }
            
            public static XYZWithReference[] StripExposedGeometryObjectsWithReferences(ElementId[] validStyleIds, Options geometryOptions, Element element)
            {
                if (element == null) return default;
                GeometryObject[] geometryObjects = StripExposedGeometryObjects(validStyleIds, element.get_Geometry(geometryOptions));
                List<XYZWithReference> xyzWithReferences = new List<XYZWithReference>();
                foreach (GeometryObject geometryObject in geometryObjects)
                {
                    if (geometryObject == null) continue;
                    else if (geometryObject is Solid solid) xyzWithReferences.AddRange(solid.Faces.OfType<PlanarFace>().SelectMany(face => StripExposedFacesWithReferences(face)).ToArray());
                    else if (geometryObject is Point point) xyzWithReferences.Add(new XYZWithReference(point.Coord, point.Reference));
                    else if (geometryObject is Curve curve && (validStyleIds.Contains(geometryObject.GraphicsStyleId) || validStyleIds == null))
                    {
                        xyzWithReferences.Add(new XYZWithReference(curve.GetEndPoint(0), curve.GetEndPointReference(0)));
                        xyzWithReferences.Add(new XYZWithReference(curve.GetEndPoint(1), curve.GetEndPointReference(1)));
                    }
                }
                return xyzWithReferences.ToArray();
            }
            
            public static XYZWithReference[] StripExposedFacesWithReferences(PlanarFace face)
            {
                Edge[] edges = face.EdgeLoops.OfType<Edge>().ToArray();
                List<XYZWithReference> xyzWithReferences = new List<XYZWithReference>();
                foreach (Edge edge in edges)
                {
                    if (edge == null) continue;
                    xyzWithReferences.AddRange(edge.Tessellate().Select(xyz => new XYZWithReference(xyz, face.Reference)));
                }
                return xyzWithReferences.ToArray();
            }
        }
        public struct IdWithXYZ
        {
            public int id;
            public int secondaryId; //only used for line endPoint indices
            public XYZ xyz;
            public IdWithXYZ(int id, int secondaryId, XYZ xyz)
            {
                this.id = id;
                this.secondaryId = secondaryId;
                this.xyz = xyz;
            }
            public IdWithXYZ(int id, XYZ xyz)
            {
                this.id = id;
                this.xyz = xyz;
                secondaryId = -1;
            }
            public static IdWithXYZ[] BreakdownGeometryObject(GeometryObject geometryObject)
            {
                if (geometryObject == null) return new IdWithXYZ[0];
                if (geometryObject is Point point) return new IdWithXYZ[1] { new IdWithXYZ(point.Id, point.Coord) };
                if (geometryObject is Curve curve) return new IdWithXYZ[2] {
                    new IdWithXYZ(curve.Id, 0, curve.GetEndPoint(0)),
                    new IdWithXYZ(curve.Id, 1, curve.GetEndPoint(1))
                };
                if (geometryObject is PlanarFace planarFace) return new IdWithXYZ[1] { new IdWithXYZ(planarFace.Id, planarFace.Origin) };
                return new IdWithXYZ[0];
            }
            public static IdWithXYZ[] BreakdownGeometryObjects(GeometryObject[] geometryObjects)
            {
                List<IdWithXYZ> idsWithXYZs = new List<IdWithXYZ>();
                foreach (GeometryObject geometryObject in geometryObjects)
                {
                    idsWithXYZs.AddRange(BreakdownGeometryObject(geometryObject));
                }
                return idsWithXYZs.ToArray();
            }
            public override string ToString()
            {
                if (secondaryId >= 0) return "( " + id + ", " + secondaryId + " ): " + xyz.ToString();
                return "( " + id + " ): " + xyz.ToString();
            }
        }

        public struct IdWithReference
        {
            public int id;
            public int secondaryId; //only used for line endPoint indices
            public Reference reference;
            public IdWithReference(int id, int secondaryId, Reference reference)
            {
                this.id = id;
                this.secondaryId = secondaryId;
                this.reference = reference;
            }
            public IdWithReference(int id, Reference reference)
            {
                this.id = id;
                this.reference = reference;
                secondaryId = -1;
            }
            public static IdWithReference[] BreakdownGeometryObject(GeometryObject geometryObject)
            {
                if (geometryObject == null) return new IdWithReference[0];
                if (!geometryObject.IsElementGeometry) return new IdWithReference[0];
                if (geometryObject is Point point) return new IdWithReference[1] { new IdWithReference(point.Id, point.Reference) };
                if (geometryObject is Curve curve) return new IdWithReference[2] {
                    new IdWithReference(curve.Id, 0, curve.GetEndPointReference(0)),
                    new IdWithReference(curve.Id, 1, curve.GetEndPointReference(1))
                };
                if (geometryObject is PlanarFace planarFace) return new IdWithReference[1] { new IdWithReference(planarFace.Id, planarFace.Reference) };
                return new IdWithReference[0];
            }
            public static IdWithReference[] BreakdownGeometryObjects(GeometryObject[] geometryObjects)
            {
                if (geometryObjects == null) return new IdWithReference[0];
                if (geometryObjects.Length == 0) return new IdWithReference[0];
                List<IdWithReference> IdsWithReferences = new List<IdWithReference>();
                foreach (GeometryObject geometryObject in geometryObjects)
                {
                    IdsWithReferences.AddRange(BreakdownGeometryObject(geometryObject));
                }
                return IdsWithReferences.ToArray();
            }
            public override string ToString()
            {
                if (secondaryId >= 0) return "( " + id + ", " + secondaryId + " ): " + reference.ToString();
                return "( " + id + " ): " + reference.ToString();
            }
        }



        /// <summary>
        /// Projects a 3D point onto a plane. Meant to be used for projecting dimensioning reference points onto the active view's plane Made by @jeremytammik on https://thebuildingcoder.typepad.com/blog/2014/09/planes-projections-and-picking-points.html
        /// </summary>
        /// <param name="plane">Plane to be projected onto.</param>
        /// <param name="point">Point not on plane.</param>
        /// <returns>Projected point on plane.</returns>
        public static XYZ ProjectPointOntoPlane(Plane plane, XYZ point)
        {
            double distance = plane.Normal.DotProduct(point);
            return point - distance * plane.Normal;
        }

        public static Element[] GetConnectedElements(Connector[] connectors)
        {
            if (connectors == null || connectors.Length == 0) return null;
            List<Element> connected = new List<Element>();
            foreach (Connector connector in connectors)
            {
                Connector other = ConnectionUtils.TryGetConnected(connector);
                if (other == null) continue;
                if (other.Owner == null) continue;
                if (!other.Owner.IsValidObject) continue;

                connected.Add(other.Owner);
            }
            return connected.ToArray();
        }
        public static PlanarFace[] GetPlanarFacesOfBend(View activeView, Element element)
        {
            if (element == null) return null;
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
            if (element == null) return null;
            GeometryElement geometry = element.get_Geometry(GetGeometryOptions(activeView));
            foreach (GeometryObject goA in geometry)
            {
                if (!(goA is GeometryInstance)) continue;
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
            if (element == null) return null;
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
        public static Line GetGeometryLineOfPipe(View activeView, Pipe pipe)
        {
            if (pipe == null) return null;
            return pipe.get_Geometry(GetGeometryOptions(activeView)).OfType<Line>().Where(line => line.Id.Equals(0)).FirstOrDefault();
        }
        public static Reference[] GetEndPointReferences(Line line)
        {
            if (line == null) return null;
            Reference refA = line.GetEndPointReference(0);
            Reference refB = line.GetEndPointReference(1);
            return new Reference[2] { refA, refB };
        }
        public static XYZ GetOrigin(Location location)
        {
            if (location == null) return null;
            if (location is LocationPoint point) return point.Point;
            if (location is LocationCurve curve)
            {
                if (curve.Curve is Line line) return line.Origin;
            }
            return null;
        }
    }
}
