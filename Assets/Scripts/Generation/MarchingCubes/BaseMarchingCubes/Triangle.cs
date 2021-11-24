using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public readonly struct Triangle
    {

        public Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public const int SIZE_OF_TRI = sizeof(float) * 9;
        //TODO:
        //could share this with their neighbour
        //do i need this at all?
        public readonly Vector3 a;
        public readonly Vector3 b;
        public readonly Vector3 c;


    }
}