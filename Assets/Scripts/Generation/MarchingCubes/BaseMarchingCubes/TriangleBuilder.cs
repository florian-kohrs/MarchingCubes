using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public struct TriangleBuilder
    {

        public Triangle tri;

        public uint data;

        public Vector3Int Origin
        {
            get
            {
                Vector3Int r = new Vector3Int();
                int step = 1 << 8;
                r.z = (int)(data % step);
                r.y = (int)(data >> 8) % step;
                r.x = (int)(data >> 16) % step;
                return r;
            }
        }

        public int TriIndex => (int)(data >> 24);

    //    public float angleFromCenter;

        public const int SIZE_OF_TRI_BUILD = Triangle.SIZE_OF_TRI + sizeof(int) * 1;

    }
}