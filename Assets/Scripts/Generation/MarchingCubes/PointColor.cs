using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

	[System.Serializable]
    public struct PointColor
    {

        //public float rFlat;
        //public float gFlat;
        //public float bFlat;

        //public float rSteep;
        //public float gSteep;
        //public float bSteep;

        public uint index;
        
        public const int SIZE = sizeof(uint) * 1;

    }
}