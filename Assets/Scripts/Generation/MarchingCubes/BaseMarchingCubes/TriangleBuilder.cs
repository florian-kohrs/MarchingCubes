using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public readonly struct TriangleBuilder
    {

        public readonly Triangle tri;

        public readonly byte triIndex;
        public readonly byte z;
        public readonly byte y;
        public readonly byte x;

        public readonly byte steepness;
        public readonly byte b;
        public readonly byte g;
        public readonly byte r;

        //public uint steepnessAndColorData;

        //public int colorData;

        public Vector3Int Origin
        {
            get
            {
                return new Vector3Int(x,y, z);
            }
        }

        public static uint zipData(int x, int y, int z, int triIndex)
        {
            return (uint)((x << 24) + (y << 16) + (z << 8) + triIndex);
        }

        private const int step = 1 << 8;

        public Color GetColor()
        {
            return new Color(
                r / 255f,
                g / 255f,
                b / 255f, 1);
        }


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