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

        protected List<float> neighbourDistanceMapping;

        protected List<float> NeighbourDistanceMapping
        {
            get
            {
                if (neighbourDistanceMapping == null)
                {
                    neighbourDistanceMapping = new List<float> (3);
                }
                return neighbourDistanceMapping;
            }
        }

        //public float GetDistanceToNeighbour(PathTriangle neighbour)
        //{
        //    float distance;
        //    if (!NeighbourDistanceMapping.TryGetValue(neighbour, out distance))
        //    {
        //        Ray r = NeighbourTransitionRayMapping[neighbour];
        //        NeighbourTransitionRayMapping.Remove(neighbour);
        //        neighbour.NeighbourTransitionRayMapping.Remove(this);
        //        distance = Vector3.Cross(r.direction, UnrotatedMiddlePointOfTriangle - r.origin).magnitude;
        //        distance += Vector3.Cross(r.direction, neighbour.UnrotatedMiddlePointOfTriangle - r.origin).magnitude;
        //        NeighbourDistanceMapping[neighbour] = distance;
        //        neighbour.NeighbourDistanceMapping[this] = distance;
        //    }
        //    return distance;
        //}


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
                neighbours.Add(p);
                p.neighbours.Add(this);

                int firstEdge = TriangulationTable.GetEdgeIndex(tri.triangulationIndex, index, edges.x);
                int secondEdge = TriangulationTable.GetEdgeIndex(tri.triangulationIndex, index, edges.y);

                BuildDistance(p, new Vector2Int(firstEdge,secondEdge));
            }
            return result;
        }

        public void BuildDistance(PathTriangle p, Vector2Int edgeIndices)
        {
            Vector3 middleEdgePoint = tri[edgeIndices.x] + (tri[edgeIndices.y] - tri[edgeIndices.x] / 2);
            float distance = (UnrotatedMiddlePointOfTriangle - middleEdgePoint).magnitude;
            distance += (UnrotatedMiddlePointOfTriangle - p.UnrotatedMiddlePointOfTriangle).magnitude;
            NeighbourDistanceMapping.Add(distance);
            p.NeighbourDistanceMapping.Add(distance);
        }

        public void AddNeighbourTwoWay(PathTriangle p, Vector2Int edgeIndices)
        {
            if (!neighbours.Contains(p))
            {
                neighbours.Add(p);
                p.neighbours.Add(this);
                BuildDistance(p, edgeIndices);
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
            return from.NeighbourDistanceMapping[neighbours.IndexOf(to)];
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

                //return Vector3.Scale(middlePointOfTriangle + chunk.transform.position, chunk.transform.lossyScale);
                return middlePointOfTriangle + chunk.transform.position;
            }
        }

    }

}