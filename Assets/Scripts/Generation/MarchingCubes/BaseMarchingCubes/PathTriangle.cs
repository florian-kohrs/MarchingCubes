using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    public class PathTriangle : INavigatable<PathTriangle, PathTriangle>
    {

        public PathTriangle(/*MarchingCubeChunkObject chunk,*/ Triangle t)
        {
            //this.chunk = chunk;
            tri = t;
            normal = (Vector3.Cross(tri.b - tri.a, tri.c - tri.a)).normalized;
            middlePoint = (tri.a + tri.b + tri.c) / 3;
            slope = Mathf.Acos(Vector3.Dot(normal, middlePoint.normalized)) * 180 / Mathf.PI;
        }

        protected const float MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING = 45;

        public Vector3 normal;

        public Vector3 middlePoint;

        public float slope;

        //MarchingCubeChunkObject chunk;

        public Triangle tri;

        public PathTriangle[] neighbours = new PathTriangle[3];

        protected float[] neighbourDistanceMapping = new float[3];


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
            Vector3 middleEdgePoint = tri[edge1] + ((tri[edge2] - tri[edge1]) / 2);
            float distance = (OriginalLOcalMiddlePointOfTriangle - middleEdgePoint).magnitude;
            distance += (OriginalLOcalMiddlePointOfTriangle - p.OriginalLOcalMiddlePointOfTriangle).magnitude;
            neighbourDistanceMapping[myKey] = distance;
            p.neighbourDistanceMapping[otherKey] = distance;
        }


        public IEnumerable<PathTriangle> GetCircumjacent(PathTriangle field)
        {
            for (int i = 0; i < 3; i++)
            {
                if(field.neighbours != null && field.Slope < MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING)
                {
                    yield return field.neighbours[i];
                }
            }
        }

        public float DistanceToTarget(PathTriangle from, PathTriangle to)
        {
            return (to.OriginalLOcalMiddlePointOfTriangle - from.OriginalLOcalMiddlePointOfTriangle).magnitude;
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


        public Vector3 OriginalLOcalMiddlePointOfTriangle => middlePoint;

    }

}