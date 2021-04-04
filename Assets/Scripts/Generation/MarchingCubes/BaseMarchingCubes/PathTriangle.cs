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
            normal = (Vector3.Cross(tri.b - tri.a, tri.c - tri.a)).normalized;
            slope = Mathf.Acos(Vector3.Dot(normal, OriginalLOcalMiddlePointOfTriangle.normalized)) * 180 / Mathf.PI;
            //Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(normal));
            //Vector3 a1 = Vector3.ProjectOnPlane(t.a, normal);
            //Vector3 a2 = Vector3.ProjectOnPlane(t.b, normal);
            //Vector3 a3= Vector3.ProjectOnPlane(t.c, normal);
        }

        public Vector3 normal;

        protected float slope;

        MarchingCubeChunk chunk;

        public Triangle tri;

        public PathTriangle[] neighbours = new PathTriangle[3];

        protected float[] neighbourDistanceMapping;

        protected float[] NeighbourDistanceMapping
        {
            get
            {
                if (neighbourDistanceMapping == null)
                {
                    neighbourDistanceMapping = new float[3];
                }
                return neighbourDistanceMapping;
            }
        }


        public bool IsInTriangle(Vector3 point)
        {
            float a = AreaOfTriangle(tri.a, tri.b, tri.c);
            float a1 = AreaOfTriangle(point, tri.a, tri.b);
            float a2 = AreaOfTriangle(point, tri.b, tri.c);
            float a3 = AreaOfTriangle(point, tri.a, tri.c);
            return a == a1 + a2 + a3;
        }

        protected float AreaOfTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            return default;
        }


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


        public void SoftSetNeighbourTwoWay(PathTriangle p, Vector2Int myEdges, Vector2Int otherEdges)
        {
            SoftSetNeighbourTwoWay(p, myEdges.x, myEdges.y, otherEdges.x, otherEdges.y);
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
            NeighbourDistanceMapping[myKey] = distance;
            p.NeighbourDistanceMapping[otherKey] = distance;
        }

        public IEnumerable<PathTriangle> GetCircumjacent(PathTriangle field)
        {
            return field.neighbours;
        }

        public float DistanceToTarget(PathTriangle from, PathTriangle to)
        {
            return (to.OriginalLOcalMiddlePointOfTriangle - from.OriginalLOcalMiddlePointOfTriangle).magnitude;
        }

        public float DistanceToField(PathTriangle from, PathTriangle to)
        {
            return from.NeighbourDistanceMapping[Array.IndexOf(from.neighbours, to)];
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


        public Vector3 OriginalLOcalMiddlePointOfTriangle => (tri.a + tri.b + tri.c) / 3;

    }

}