using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class PathTriangle : INavField
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


        /// <summary>
        /// maybe stop storing? but couldnt recalculate normal.
        /// since every point is shared at least three times maybe reference points? (store less)
        /// </summary>
        public Triangle tri;

        public int activeInPathIteration;

        public List<PathTriangle> Neighbours => GetCircumjacent();

        public virtual List<PathTriangle> GetCircumjacent()
        {
            List<PathTriangle> result = e.GetNeighboursOf(this);
            int count = result.Count;
            PathTriangle tri;
            for (int i = 0; i < count; ++i)
            {
                tri = result[i];
                if (tri.Slope > MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING)
                {
                    result.RemoveAt(i);
                    i--;
                    count--;
                }
            }
            if (result.Count == 0)
            {
                Debug.Log("empty");
            }
            return result;
        }

        public float DistanceToTarget(PathTriangle to)
        {
            return (to.EstimatedMiddlePoint - EstimatedMiddlePoint).magnitude;
        }

        public bool IsEqual(PathTriangle t2)
        {
            return this == t2;
        }

        public void SetUsedInPathIteration(int iteration)
        {
            activeInPathIteration = iteration;
        }

        public int LastUsedInPathIteration => activeInPathIteration;
        

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