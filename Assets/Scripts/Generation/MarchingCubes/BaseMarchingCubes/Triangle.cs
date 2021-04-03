using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public struct Triangle
    {

        public const int SIZE_OF_TRI = sizeof(float) * 9;

        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }

        public bool Equals(Triangle tri)
        {
            return a == tri.a && b == tri.b && c == tri.c;
        }

        public bool Contains(Vector3 v)
        {
            return this[0] == v || this[1] == v || this[2] == v;
        }

    }
}