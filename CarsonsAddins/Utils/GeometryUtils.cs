using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static Reference GetPseudoReferenceOfConnector(Options geometryOptions, Plane plane, Connector connector)
        {
            XYZ projectedOrigin = ProjectPointOntoPlane(plane, connector.Origin);
            if (projectedOrigin == null) return null;
            PlanarFace[] instancePlanarFaces = GetGeometryObjectFromInstanceGeometry<PlanarFace>(geometryOptions, connector.Owner);
            Lookup<int, PlanarFace> symbolPlanarFacesLookup = GetGeometryObjectFromSymbolGeometry<PlanarFace>(geometryOptions, connector.Owner).ToLookup(planarFace => planarFace.Id) as Lookup<int, PlanarFace>;
            PlanarFace instanceConnectorFace = instancePlanarFaces.Where(planarFace => ProjectPointOntoPlane(plane, planarFace.Origin).IsAlmostEqualTo(projectedOrigin)).FirstOrDefault();
            if (instanceConnectorFace == default(PlanarFace)) return null;
            PlanarFace symbolFace = symbolPlanarFacesLookup[instanceConnectorFace.Id].FirstOrDefault();
            Reference referenceConnectorFace = symbolFace.Reference;
            return referenceConnectorFace;
        }
        public static GeometryObject[] GetGeometryLinesOfBend(View activeView, Element element, ElementId validStyleId)
        {
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
        public static XYZWithReference[] StripGeometryObjectsWithReferences(Element element)
        {
            GeometryElement geometryElement = element.get_Geometry(GetGeometryOptions());
            if (geometryElement == null) return new XYZWithReference[0];
            GeometryInstance geometryInstance = geometryElement.OfType<GeometryInstance>().FirstOrDefault();
            if (geometryInstance == null) return new XYZWithReference[0];
            List<XYZWithReference> geometryObjectWithReferences = new List<XYZWithReference>();
            IdWithXYZ[] idsWithXYZs = IdWithXYZ.BreakdownGeometryObjects(BreakdownSolidsIntoDimensionableGeometryObjects(geometryInstance.GetInstanceGeometry()));
            Lookup<int, IdWithReference> idsWithReferences = IdWithReference.BreakdownGeometryObjects(BreakdownSolidsIntoDimensionableGeometryObjects(geometryInstance.GetSymbolGeometry()))
                .ToLookup(idWithReference => idWithReference.id) as Lookup<int, IdWithReference>;

            XYZWithReference[] xyzsWithReferences = idsWithXYZs.Select(xyzWithRef => new XYZWithReference(xyzWithRef.xyz, idsWithReferences[xyzWithRef.id]
                .Where(idWithRef => xyzWithRef.secondaryId == idWithRef.secondaryId).FirstOrDefault().reference)).ToArray();
            return xyzsWithReferences;
        }

        public static GeometryObject[] BreakdownSolidsIntoDimensionableGeometryObjects(GeometryElement geometryElement)
        {
            if (geometryElement == null) return new GeometryObject[0];
            List<GeometryObject> geometryObjects = new List<GeometryObject>();
            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (geometryObject == null) continue;
                else if (geometryObject is Solid solid) geometryObjects.AddRange(solid.Faces.OfType<PlanarFace>().ToArray());
                else if (geometryObject is Point) geometryObjects.Add(geometryObject);
                else if (geometryObject is Line) geometryObjects.Add(geometryObject);

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

        }
        public struct IdWithXYZ
        {
            public int id;
            public int secondaryId; //only used for line endpoint indices
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
                if (!geometryObject.IsElementGeometry) return new IdWithXYZ[0];
                if (geometryObject is Point point) return new IdWithXYZ[1] { new IdWithXYZ(point.Id, point.Coord) };
                if (geometryObject is Line line) return new IdWithXYZ[2] {
                    new IdWithXYZ(line.Id, 0, line.GetEndPoint(0)),
                    new IdWithXYZ(line.Id, 1, line.GetEndPoint(1))
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
        }

        public struct IdWithReference
        {
            public int id;
            public int secondaryId; //only used for line endpoint indices
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
                if (geometryObject is Line line) return new IdWithReference[2] {
                    new IdWithReference(line.Id, 0, line.GetEndPointReference(0)),
                    new IdWithReference(line.Id, 1, line.GetEndPointReference(1))
                };
                if (geometryObject is PlanarFace planarFace) return new IdWithReference[1] { new IdWithReference(planarFace.Id, planarFace.Reference) };
                return new IdWithReference[0];
            }
            public static IdWithReference[] BreakdownGeometryObjects(GeometryObject[] geometryObjects)
            {
                List<IdWithReference> IdsWithReferences = new List<IdWithReference>();
                foreach (GeometryObject geometryObject in geometryObjects)
                {
                    IdsWithReferences.AddRange(BreakdownGeometryObject(geometryObject));
                }
                return IdsWithReferences.ToArray();
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
            return pipe.get_Geometry(GetGeometryOptions(activeView)).OfType<Line>().Where(line => line.Id.Equals(0)).FirstOrDefault();
        }
        public static Reference[] GetEndPointReferences(Line line)
        {
            Reference refA = line.GetEndPointReference(0);
            Reference refB = line.GetEndPointReference(1);
            return new Reference[2] { refA, refB };
        }
        public static bool IsLinearElement(Element element)
        {
            if (element == null) return false;
            if (ElementCheckUtils.IsPipe(element) || ElementCheckUtils.IsPipeFlange(element)) return true;
            if (!(element is FamilyInstance familyInstance)) return false;
            XYZ[] connectorOrigins = ConnectionUtils.GetConnectors(familyInstance).Select(connector => connector.Origin).ToArray();
            if (connectorOrigins == null) return false;
            if (connectorOrigins.Length != 2) return false;
            XYZ origin = (element.Location as LocationPoint).Point;
            Line line = Line.CreateBound(connectorOrigins[0], connectorOrigins[1]);
            line.MakeUnbound();
            IntersectionResult intersectionResult = line.Project(origin);
            return (origin.IsAlmostEqualTo(intersectionResult.XYZPoint));
        }
    }
}
