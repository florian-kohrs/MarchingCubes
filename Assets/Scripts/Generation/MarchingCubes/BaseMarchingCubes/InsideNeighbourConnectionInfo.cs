using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InsideNeighbourConnectionInfo
{


    public InsideNeighbourConnectionInfo(int neighbourTriangleIndex, int edgeIndex1, int edgeIndex2)
    {
        this.neighbourTriangleIndex = neighbourTriangleIndex;
        this.edgeIndex1 = edgeIndex1;
        this.edgeIndex2 = edgeIndex2;
    }

    public int neighbourTriangleIndex;

    public int edgeIndex1;
    public int edgeIndex2;

}
