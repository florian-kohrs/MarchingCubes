using System;
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

        public const int SIZE_OF_TRI_BUILD = Triangle.SIZE_OF_TRI + sizeof(int) * 2;

        
        public TriangleBuilder(Triangle tri) : this(tri, default, default, default, default, default, default, default, default) { }
        
        public TriangleBuilder(Triangle tri, byte triIndex, byte z, byte y, byte x, byte steepness, byte b, byte g, byte r)
        {
            this.tri = tri;
            this.triIndex = triIndex;
            this.z = z;
            this.y = y;
            this.x = x;
            this.steepness = steepness;
            this.b = b;
            this.g = g;
            this.r = r;
        }
    }
}