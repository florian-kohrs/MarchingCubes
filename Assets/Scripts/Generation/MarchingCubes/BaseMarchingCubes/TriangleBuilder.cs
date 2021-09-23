using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public struct TriangleBuilder
    {

        public Triangle tri;

        public uint cubeData;
        public uint steepnessAndColorData;

        //public int colorData;

        public Vector3Int Origin
        {
            get
            {
                Vector3Int r = new Vector3Int();
                int step = 1 << 8;
                r.z = (int)(cubeData % step);
                r.y = (int)((cubeData >> 8) % step);
                r.x = (int)((cubeData >> 16) % step);
                return r;
            }
        }

        public static uint zipData(int x, int y, int z, int triIndex)
        {
            return (uint)((triIndex << 24) + (x << 16) + (y << 8) + z);
        }
        public Color GetColor()
        {
            Color c = new Color(0, 0, 0, 1);
            int step = 1 << 8;
            c.r = (int)(steepnessAndColorData % step) / 255f;
            c.g = (int)((steepnessAndColorData >> 8) % step) / 255f;
            c.b = (int)((steepnessAndColorData >> 16) % step) / 255f;
            return c;
        }

        public int TriIndex => (int)(cubeData >> 24);

        public int Steepness => (int)(steepnessAndColorData >> 24);

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

        public const int SIZE_OF_TRI_BUILD = Triangle.SIZE_OF_TRI + sizeof(int) * 2;

    }
}