﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class PathTriangle : INavField
    {


        public PathTriangle(ICubeEntity e, in Triangle t, Func<PathTriangle, int, int[]> f)
        {
            this.e = e;
            tri = t;
            steepness = (byte)(Mathf.Acos(Vector3.Dot(Normal, MiddlePoint.normalized)) * 180 / Mathf.PI);
            int[] c = f(this, steepness);
            r = (byte)c[0];
            g = (byte)c[1];
            b = (byte)c[2];
        }

        public PathTriangle(ICubeEntity e, in Triangle t, byte r, byte g, byte b, byte steepness)
        {
            this.e = e;
            this.steepness = steepness;
            tri = t;
            this.r = r;
            this.g = g;
            this.b = b;
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

        public int Steepness => steepness;


        public const int TRIANGLE_NEIGHBOUR_COUNT = 3;

        protected const float MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING = 45;

        public int[] CornerIndices
        {
            get
            {
                int[] result = new int[3];

                int index = e.IndexOfTri(this) * 3;

                int[] triangulation = TriangulationTable.triangulation[e.TriangulationIndex];

                result[0] = TriangulationTable.cornerIndexAFromEdge[triangulation[index]];
                result[1] = TriangulationTable.cornerIndexAFromEdge[triangulation[index + 1]];
                result[2] = TriangulationTable.cornerIndexAFromEdge[triangulation[index + 2]];

                return result;
            }
        }

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

        public byte steepness;
        public byte b;
        public byte g;
        public byte r;


        private const int step = 1 << 8;

        public Color GetColor()
        {
            Color c = new Color(
                r / 255f,
                g / 255f,
                b / 255f, 1);
            return c;
        }

        /// <summary>
        /// maybe stop storing? but couldnt recalculate normal.
        /// since every point is shared at least three times maybe reference points? (store less)
        /// </summary>
        public Triangle tri;

        public int activeInPathIteration;
        public int activeInBackwardsPathIteration;
        public PathTriangle pathParent;
        public float prevDistance;
        //public List<PathTriangle> neighbours;

        public void BuildPath(List<PathTriangle> result)
        {
            result.Add(this);
            if (pathParent != null)
                pathParent.BuildPath(result);
        }

        public List<PathTriangle> Neighbours => GetNeighbours();

        public virtual List<PathTriangle> GetReachableCircumjacent()
        {
            List<PathTriangle> neighbours = GetNeighbours();
            List<PathTriangle> result = new List<PathTriangle>();
            int count = neighbours.Count;
            PathTriangle tri;
            for (int i = 0; i < count; ++i)
            {
                tri = neighbours[i];
                if (tri.Slope < MAX_SLOPE_TO_BE_USEABLE_IN_PATHFINDING)
                {
                    result.Add(tri);
                }
            }
            return result;
        }

        public List<PathTriangle> GetNeighbours()
        {
            return e.GetNeighboursOf(this);
            
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

        public float DistanceTo(PathTriangle tri)
        {
            return 0.5f;/*(e.Origin - tri.e.Origin).magnitude;*/
        }

        public void SetUsedInBackwardsPathIteration(int iteration)
        {
            activeInBackwardsPathIteration = iteration;
        }

        public float Slope => steepness;

        public Vector3 EstimatedMiddlePoint => tri.a;

        public int LastUsedInBackwardsPathIteration => activeInBackwardsPathIteration;

    }

}