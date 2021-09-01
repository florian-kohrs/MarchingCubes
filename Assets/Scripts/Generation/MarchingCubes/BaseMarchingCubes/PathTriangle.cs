using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class PathTriangle : INavigatable<PathTriangle, PathTriangle>
    {



        public PathTriangle(/*MarchingCubeChunkObject chunk,*/ Triangle t)
        {
            //this.chunk = chunk;
            tri = t;
            normal = (Vector3.Cross(tri.b - tri.a, tri.c - tri.a));
            float normMagnitude = normal.magnitude;
            normal.x /= normMagnitude;
            normal.y /= normMagnitude;
            normal.z /= normMagnitude;
            //middlePoint = (tri.a + tri.b + tri.c) / 3;
            middlePoint = new Vector3(
                (tri.a.x + tri.b.x + tri.c.x) / 3,
                (tri.a.y + tri.b.y + tri.c.y) / 3,
                (tri.a.z + tri.b.z + tri.c.z) / 3);
            slope = Mathf.Acos(Vector3.Dot(normal, middlePoint.normalized)) * 180 / Mathf.PI;
        }

        public const int TRIANGLE_NEIGHBOUR_COUNT = 3;

        protected const float MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING = 45;

        public Vector3 normal;

        public Vector3 middlePoint;

        public float slope;

        //MarchingCubeChunkObject chunk;

        public Triangle tri;

        public PathTriangle[] neighbours = new PathTriangle[TRIANGLE_NEIGHBOUR_COUNT];

        protected float[] neighbourDistanceMapping = new float[TRIANGLE_NEIGHBOUR_COUNT];


        //public void AddNeighbourTwoWay(PathTriangle p, int myEdge1, int myEdge2, int otherEdge1, int otherEdge2)
        //{
        //    if (!neighbours.Contains(p))
        //    {
        //        AddNeighbourTwoWayUnchecked(p, myEdge1, myEdge2, otherEdge1, otherEdge2);
        //    }
        //}

        public void SoftSetNeighbourTwoWay(PathTriangle p, int myEdge1, int myEdge2, int otherEdge1, int otherEdge2)
        {
            int myKey = (myEdge1 + myEdge2) % 3;

            if (neighbours[myKey] != null)
                return;

            int otherKey = (otherEdge1 + otherEdge2) % 3;
            neighbours[myKey] = p;
            p.neighbours[otherKey] = this;
            BuildDistance(p, myEdge1, myEdge2, myKey, otherKey);
        }

        public void OverrideNeighbourTwoWay(PathTriangle p, int myEdge1, int myEdge2, int otherEdge1, int otherEdge2)
        {
            int myKey = (myEdge1 + myEdge2) % 3;

            int otherKey = (otherEdge1 + otherEdge2) % 3;
            neighbours[myKey] = p;
            p.neighbours[otherKey] = this;
            BuildDistance(p, myEdge1, myEdge2, myKey, otherKey);
        }


        public void SoftSetNeighbourTwoWay(PathTriangle p, Vector2Int myEdges, Vector2Int otherEdges)
        {
            SoftSetNeighbourTwoWay(p, myEdges.x, myEdges.y, otherEdges.x, otherEdges.y);
        }


        public void OverrideNeighbourTwoWay(PathTriangle p, Vector2Int myEdges, Vector2Int otherEdges)
        {
            OverrideNeighbourTwoWay(p, myEdges.x, myEdges.y, otherEdges.x, otherEdges.y);
        }

        //public void AddNeighbourTwoWay(PathTriangle p, Vector2Int edgeIndices)
        //{
        //    AddNeighbourTwoWay(p, edgeIndices.x, edgeIndices);
        //}

        public void BuildDistance(PathTriangle p, int edge1, int edge2, int myKey, int otherKey)
        {
            //Vector3 middleEdgePoint = tri[edge1] + ((tri[edge2] - tri[edge1]) / 2);
            //float distance = (middlePoint - middleEdgePoint).magnitude;
            //distance += (middlePoint - p.middlePoint).magnitude;
            float distance = 1;
            neighbourDistanceMapping[myKey] = distance;
            p.neighbourDistanceMapping[otherKey] = distance;
        }


        public List<PathTriangle> GetCircumjacent(PathTriangle field)
        {
            List<PathTriangle> result = new List<PathTriangle>(TRIANGLE_NEIGHBOUR_COUNT);
            for (int i = 0; i < TRIANGLE_NEIGHBOUR_COUNT; i++)
            {
                if (field.neighbours != null && field.Slope < MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING)
                {
                    result.Add(field.neighbours[i]);
                }
            }
            return result;
        }

        public float DistanceToTarget(PathTriangle from, PathTriangle to)
        {
            return (to.OriginalLocalMiddlePointOfTriangle - from.OriginalLocalMiddlePointOfTriangle).magnitude;
        }

        public float DistanceToField(PathTriangle from, PathTriangle to)
        {
            return from.neighbourDistanceMapping[Array.IndexOf(from.neighbours, to)];
        }

        public bool ReachedTarget(PathTriangle current, PathTriangle destination)
        {
            return current == destination;
        }

        public bool IsEqual(PathTriangle t1, PathTriangle t2)
        {
            return t1 == t2;
        }

        public Vector3 Normal => normal;

        public float Slope => slope;


        public Vector3 OriginalLocalMiddlePointOfTriangle => middlePoint;

    }

}