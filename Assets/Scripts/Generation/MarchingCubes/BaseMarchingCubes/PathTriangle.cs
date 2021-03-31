using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    public class PathTriangle : INavigatable<PathTriangle, PathTriangle>
    {

        public PathTriangle(MarchingCubeChunk chunk, Triangle t)
        {
            this.chunk = chunk;
            tri = t;
        }

        MarchingCubeChunk chunk;

        public Triangle tri;

        public List<PathTriangle> neighbours = new List<PathTriangle>(3);

        protected Dictionary<PathTriangle, Ray> neighbourTransitionRayMapping;

        protected Dictionary<PathTriangle, Ray> NeighbourTransitionRayMapping
        {
            get
            {
                if (neighbourTransitionRayMapping == null)
                {
                    neighbourTransitionRayMapping = new Dictionary<PathTriangle, Ray>(3);
                }
                return neighbourTransitionRayMapping;
            }
        }

        protected Dictionary<PathTriangle, int> neighbourSharedIndexMapping;

        protected Dictionary<PathTriangle, float> neighbourDistanceMapping;

        protected Dictionary<PathTriangle, float> NeighbourDistanceMapping
        {
            get
            {
                if (neighbourDistanceMapping == null)
                {
                    neighbourDistanceMapping = new Dictionary<PathTriangle, float>(3);
                }
                return neighbourDistanceMapping;
            }
        }

        public float GetDistanceToNeighbour(PathTriangle neighbour)
        {
            float distance;
            if (!NeighbourDistanceMapping.TryGetValue(neighbour, out distance))
            {
                Ray r = NeighbourTransitionRayMapping[neighbour];
                NeighbourTransitionRayMapping.Remove(neighbour);
                neighbour.NeighbourTransitionRayMapping.Remove(this);
                distance = Vector3.Cross(r.direction, UnrotatedMiddlePointOfTriangle - r.origin).magnitude;
                distance += Vector3.Cross(r.direction, neighbour.UnrotatedMiddlePointOfTriangle - r.origin).magnitude;
                NeighbourDistanceMapping[neighbour] = distance;
                neighbour.NeighbourDistanceMapping[this] = distance;
            }
            return distance;
        }


        public List<Vector2> edgesWithoutNeighbour = null;

        public List<Vector2> EdgesWithoutNeighbour
        {
            get
            {
                if (edgesWithoutNeighbour == null)
                {
                    edgesWithoutNeighbour = new List<Vector2>();
                }
                return edgesWithoutNeighbour;
            }
        }


        //public void BuildNeighboursIn(PathTriangle t, int index, Vector2Int edges)
        //{
        //    Ray r = new Ray();
        //    int firstEdge = TriangulationTable.GetEdgeIndex(tri.triangulationIndex, index, edges.x);
        //    int secondEdge = TriangulationTable.GetEdgeIndex(tri.triangulationIndex, index, edges.y);
        //    r.origin = tri[firstEdge];
        //    r.direction = tri[secondEdge] - tri[firstEdge];

        //    neighbours.Add(t);

        //}

        public bool AddNeighbourTwoWay(PathTriangle p, int index, Vector2Int edges)
        {
            bool result = !neighbours.Contains(p);
            if (result)
            {
                Ray r = new Ray();
                int firstEdge = TriangulationTable.GetEdgeIndex(tri.triangulationIndex, index, edges.x);
                int secondEdge = TriangulationTable.GetEdgeIndex(tri.triangulationIndex, index, edges.y);
                r.origin = tri[firstEdge];
                r.direction = tri[secondEdge] - tri[firstEdge];

                NeighbourTransitionRayMapping[p] = r;
                neighbours.Add(p);
                p.neighbours.Add(this);
                p.NeighbourTransitionRayMapping[this] = r;
                GetDistanceToNeighbour(p);
            }
            return result;
        }

        public void AddNeighbourTwoWay(PathTriangle p, Vector2Int edgeIndices)
        {
            if (!neighbours.Contains(p))
            {
                Ray r = new Ray();
                int firstEdge = edgeIndices.x;
                int secondEdge = edgeIndices.y;

                r.origin = tri[firstEdge];
                r.direction = tri[secondEdge] - tri[firstEdge];

                NeighbourTransitionRayMapping[p] = r;
                neighbours.Add(p);
                p.neighbours.Add(this);
                p.NeighbourTransitionRayMapping[this] = r;
                GetDistanceToNeighbour(p);
            }
        }

        public IEnumerable<PathTriangle> GetCircumjacent(PathTriangle field)
        {
            return field.neighbours;
        }

        public float DistanceToTarget(PathTriangle from, PathTriangle to)
        {
            return (to.UnrotatedMiddlePointOfTriangle - from.UnrotatedMiddlePointOfTriangle).magnitude;
        }

        public float DistanceToField(PathTriangle from, PathTriangle to)
        {
            return from.GetDistanceToNeighbour(to);
        }

        public bool ReachedTarget(PathTriangle current, PathTriangle destination)
        {
            return current == destination;
        }

        public bool IsEqual(PathTriangle t1, PathTriangle t2)
        {
            return t1 == t2;
        }

        private Vector3 middlePointOfTriangle;



        public Vector3 UnrotatedMiddlePointOfTriangle
        {
            get
            {
                if (middlePointOfTriangle == default)
                {
                    middlePointOfTriangle = (tri.a + tri.b + tri.c) / 3;
                }

                return Vector3.Scale(middlePointOfTriangle + chunk.transform.position, chunk.transform.lossyScale);
            }
        }

    }

}