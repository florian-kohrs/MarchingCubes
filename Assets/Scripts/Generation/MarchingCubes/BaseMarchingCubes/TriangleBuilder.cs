using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public struct TriangleBuilder
    {

        public Triangle tri;

        public uint data;

        //public int colorData;

        public Vector3Int Origin
        {
            get
            {
                Vector3Int r = new Vector3Int();
                int step = 1 << 8;
                r.z = (int)(data % step);
                r.y = (int)((data >> 8) % step);
                r.x = (int)((data >> 16) % step);
                return r;
            }
        }

        public static uint zipData(int x, int y, int z, int triIndex)
        {
            return (uint)((triIndex << 24) + (x << 16) + (y << 8) + z);
        }

        public int TriIndex => (int)(data >> 24);

        //public Color Color
        //{
        //    get
        //    {
        //        Color c = new Color();
        //        float step = 1 << 8;
        //        c.a = 1;
        //        c.b = (colorData % step) / step;
        //        c.g = ((colorData >> 8) % step) / step;
        //        c.r = ((colorData >> 16) % step) / step;
        //        //c = Color.yellow;
        //        return c;
        //    }
        //}

    //    public float angleFromCenter;

        public const int SIZE_OF_TRI_BUILD = Triangle.SIZE_OF_TRI + sizeof(int) * 1;

    }
}