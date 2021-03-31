using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeTriangle : ITriangle
    {

        public MarchingCubeTriangle(Vector3Int triIndices)
        {
            this.triIndices = triIndices;
        }

        public int V1 => TriIndices.x;

        public int V2 => TriIndices.y;

        public int V3 => TriIndices.z;

        public IEnumerable<int> Corners
        {
            get
            {
                yield return V1;
                yield return V2;
                yield return V3;
            }
        }

        protected Vector3Int triIndices;

        public Vector3Int TriIndices => triIndices;

    }
}