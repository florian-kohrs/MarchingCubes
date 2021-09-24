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
                return new Vector3Int(
                    (int)((cubeData >> 16) % step),
                    (int)((cubeData >> 8) % step),
                    (int)(cubeData % step));
            }
        }

        public static uint zipData(int x, int y, int z, int triIndex)
        {
            return (uint)((triIndex << 24) + (x << 16) + (y << 8) + z);
        }

        private const int step = 1 << 8;

        public Color GetColor()
        {
            Color c = new Color(
                (int)(steepnessAndColorData % step) / 255f,
                (int)((steepnessAndColorData >> 8) % step) / 255f,
                (int)((steepnessAndColorData >> 16) % step) / 255f, 1);
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