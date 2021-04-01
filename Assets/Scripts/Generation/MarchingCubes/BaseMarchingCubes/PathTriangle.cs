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
                    neighbourDistanceMapping = new List<float>(3);
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

        public bool AddNeighbourTwoWay(PathTriangle p, int index, int edge1, int edge2)
        {
            bool result = !neighbours.Contains(p);
            if (result)
            {
                neighbours.Add(p);
                p.neighbours.Add(this);

                int firstEdge = TriangulationTableStaticData.GetEdgeIndex(tri.triangulationIndex, index, edge1);
                int secondEdge = TriangulationTableStaticData.GetEdgeIndex(tri.triangulationIndex, index, edge2);

                BuildDistance(p, firstEdge, secondEdge);
            }
            return result;
        }

        public bool AddNeighbourTwoWay(PathTriangle p, int index, Vector2Int edges)
        {
            return AddNeighbourTwoWay(p, index, edges.x, edges.y);
        }

        public void AddNeighbourTwoWay(PathTriangle p, int edge1, int edge2)
        {
            if (!neighbours.Contains(p))
            {
                AddNeighbourTwoWayUnchecked(p, edge1, edge2);
            }
        }

        public void AddNeighbourTwoWayUnchecked(PathTriangle p, int edge1, int edge2)
        {
                neighbours.Add(p);
                p.neighbours.Add(this);
                BuildDistance(p, edge1, edge2);
            
        }

        public void AddNeighbourTwoWayUnchecked(PathTriangle p, byte edge1, byte edge2)
        {
            AddNeighbourTwoWayUnchecked(p, (int)edge1, (int)edge2);
        }

        public void AddNeighbourTwoWayUnchecked(PathTriangle p, EdgePair edges)
        {
            AddNeighbourTwoWayUnchecked(p, (int)edges.edge1, (int)edges.edge2);
        }

        public void AddNeighbourTwoWay(PathTriangle p, byte edge1, byte edge2)
        {
            AddNeighbourTwoWay(p, (int)edge1, (int)edge2);
        }

        public void AddNeighbourTwoWay(PathTriangle p, Vector2Int edgeIndices)
        {
            AddNeighbourTwoWay(p, edgeIndices.x, edgeIndices);
        }

        public void BuildDistance(PathTriangle p, int edge1, int edge2)
        {
            Vector3 middleEdgePoint = tri[edge1] + ((tri[edge2] - tri[edge1]) / 2);
            float distance = (UnrotatedMiddlePointOfTriangle - middleEdgePoint).magnitude;
            distance += (UnrotatedMiddlePointOfTriangle - p.UnrotatedMiddlePointOfTriangle).magnitude;
            NeighbourDistanceMapping.Add(distance);
            p.NeighbourDistanceMapping.Add(distance);
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
            return from.NeighbourDistanceMapping[from.neighbours.IndexOf(to)];
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