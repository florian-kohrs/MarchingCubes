using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class PathTriangle : INavigatable<PathTriangle, PathTriangle>
    {


        public PathTriangle(Triangle t, Func<PathTriangle, Color> f)
        {

            tri = t;
            int steepness = (int)(Mathf.Acos(Vector3.Dot(Normal, MiddlePoint.normalized)) * 180 / Mathf.PI);
            Color c = f(this);
            steepnessAndColorData = TriangleBuilder.zipData(steepness, (int)(c.r * 255), (int)(c.g * 255), (int)(c.b * 255));
        }

        public PathTriangle(Triangle t, uint steepnessAndColorData)
        {
            this.steepnessAndColorData = steepnessAndColorData;
            tri = t;
        }


        public Vector3 MiddlePoint
        {
            get
            {
                return new Vector3(
                (tri.a.x +tri.b.x + tri.c.x) / 3,
                (tri.a.y + tri.b.y + tri.c.y) / 3,
                (tri.a.z + tri.b.z + tri.c.z) / 3);
            }
        }

        public int Steepness => (int)(steepnessAndColorData >> 24);


        public const int TRIANGLE_NEIGHBOUR_COUNT = 3;

        protected const float MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING = 45;

        /// <summary>
        /// doesnt need to store normal -> cube is found by position and normal can be recalculated for each tri in cube
        /// </summary>
        //public Vector3 normal;

        public Vector3 Normal
        {
            get
            {
                Vector3 normal = (Vector3.Cross(tri.b - tri.a, tri.c - tri.a));
                float normMagnitude = normal.magnitude;
                normal.x /= normMagnitude;
                normal.y /= normMagnitude;
                normal.z /= normMagnitude;
                return normal;
            }
        }

        /// <summary>
        /// just give steepness in 8 bit int?
        /// </summary>
        public uint steepnessAndColorData;


        public Color GetColor()
        {
            Color c = new Color(0, 0, 0, 1);
            int step = 1 << 8;
            c.r = (int)(steepnessAndColorData % step) / 255f;
            c.g = (int)((steepnessAndColorData >> 8) % step) / 255f;
            c.b = (int)((steepnessAndColorData >> 16) % step) / 255f;
            return c;
        }


        //MarchingCubeChunkObject chunk;

        /// <summary>
        /// maybe stop storing? but couldnt recalculate normal
        /// </summary>
        public Triangle tri;

        /// <summary>
        /// dont need if this is in lookup table
        /// </summary>
        public PathTriangle[] neighbours = new PathTriangle[TRIANGLE_NEIGHBOUR_COUNT];


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
            {
                return;
            }

            int otherKey = (otherEdge1 + otherEdge2) % 3;
            neighbours[myKey] = p;
            p.neighbours[otherKey] = this;
        }

        public void OverrideNeighbourTwoWay(PathTriangle p, int myEdge1, int myEdge2, int otherEdge1, int otherEdge2)
        {
            int myKey = (myEdge1 + myEdge2) % 3;

            int otherKey = (otherEdge1 + otherEdge2) % 3;
            neighbours[myKey] = p;
            p.neighbours[otherKey] = this;
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



        public List<PathTriangle> GetCircumjacent(PathTriangle field)
        {
            List<PathTriangle> result = new List<PathTriangle>(TRIANGLE_NEIGHBOUR_COUNT);
            for (int i = 0; i < TRIANGLE_NEIGHBOUR_COUNT; ++i)
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
            return (to.EstimatedMiddlePoint - from.EstimatedMiddlePoint).magnitude;
        }

        public float DistanceToField(PathTriangle from, PathTriangle to)
        {
            return 1;
        }

        public bool ReachedTarget(PathTriangle current, PathTriangle destination)
        {
            return current == destination;
        }

        public bool IsEqual(PathTriangle t1, PathTriangle t2)
        {
            return t1 == t2;
        }

        public Vector3 LazyNormal
        {
            get
            {
                Vector3 normal = (Vector3.Cross(tri.b - tri.a, tri.c - tri.a));
                float normMagnitude = normal.magnitude;
                normal.x /= normMagnitude;
                normal.y /= normMagnitude;
                normal.z /= normMagnitude;
                return normal;
            }
        }

        public float Slope => steepnessAndColorData;

        public Vector3 EstimatedMiddlePoint => tri.a;

        //public Vector3 OriginalLocalMiddlePointOfTriangle => MiddlePoint;

    }

}