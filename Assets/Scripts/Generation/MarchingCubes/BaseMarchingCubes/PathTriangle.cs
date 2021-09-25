using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class PathTriangle : INavigatable<PathTriangle, PathTriangle>
    {


        public PathTriangle(ICubeEntity e, Triangle t, Func<PathTriangle, Color> f)
        {
            this.e = e;
            tri = t;
            int steepness = (int)(Mathf.Acos(Vector3.Dot(Normal, MiddlePoint.normalized)) * 180 / Mathf.PI);
            Color c = f(this);
            steepnessAndColorData = TriangleBuilder.zipData(steepness, (int)(c.r * 255), (int)(c.g * 255), (int)(c.b * 255));
        }

        public PathTriangle(ICubeEntity e, Triangle t, uint steepnessAndColorData)
        {
            this.e = e;
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


        protected ICubeEntity e;

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


        private const int step = 1 << 8;

        public Color GetColor()
        {
            Color c = new Color(
                (int)(steepnessAndColorData % step) / 255f,
                (int)((steepnessAndColorData >> 8) % step) / 255f,
                (int)((steepnessAndColorData >> 16) % step) / 255f, 1);
            return c;
        }


        //MarchingCubeChunkObject chunk;

        /// <summary>
        /// maybe stop storing? but couldnt recalculate normal
        /// since every point is shared at least three times maybe reference points? (store less)
        /// </summary>
        public Triangle tri;

        public List<PathTriangle> Neighbours => GetCircumjacent(this);

        public virtual List<PathTriangle> GetCircumjacent(PathTriangle field)
        {
            List<PathTriangle> result = e.GetNeighboursOf(this);
            int count = result.Count;
            PathTriangle tri;
            for (int i = 0; i < count; ++i)
            {
                tri = result[i];
                if (tri != null && tri.Slope < MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING)
                {
                    result.Add(tri);
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

        public float Slope => (int)(steepnessAndColorData >> 24);

        public Vector3 EstimatedMiddlePoint => tri.a;

        //public Vector3 OriginalLocalMiddlePointOfTriangle => MiddlePoint;

    }

}