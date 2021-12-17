using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Buildings
{
    public interface IMeshable<T>
    {

        void AddToMeshCheap(Vector3[] verts, int[] tris, T[] displayData, ref int index);

        int NumVertsForCheapDisplay();

    }
}