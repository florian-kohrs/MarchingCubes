using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public struct TriangleBuilder
    {

        public Triangle tri;

        public byte triIndex;
        public byte z;
        public byte y;
        public byte x;

        public byte steepness;
        public byte b;
        public byte g;
        public byte r;  

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
            Color c = new Color(
                r / 255f,
                g / 255f,
                b / 255f, 1);
            return c;
        }

        public int TriIndex => triIndex;

        public int Steepness => steepness;

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