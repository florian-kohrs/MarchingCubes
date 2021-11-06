using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class TriangleChunkHeap 
    {

        public TriangleChunkHeap(TriangleBuilder[] tris, int startIndex, int triCount)
        {
            this.tris = tris;
            this.startIndex = startIndex;
            this.triCount = triCount;
        }

        public TriangleBuilder[] tris;

        public int startIndex;

        public int triCount;

    
    }
}