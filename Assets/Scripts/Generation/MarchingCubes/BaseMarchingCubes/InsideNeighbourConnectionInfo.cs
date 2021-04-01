using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct InsideNeighbourConnectionInfo
{


    public InsideNeighbourConnectionInfo(byte neighbourTriangleIndex, byte edgeIndex1, byte edgeIndex2)
    {
        this.neighbourTriangleIndex = neighbourTriangleIndex;
        this.edgeIndex1 = edgeIndex1;
        this.edgeIndex2 = edgeIndex2;
    }

    public InsideNeighbourConnectionInfo(int neighbourTriangleIndex, int edgeIndex1, int edgeIndex2)
    {
        this.neighbourTriangleIndex = (byte)neighbourTriangleIndex;
        this.edgeIndex1 = (byte)edgeIndex1;
        this.edgeIndex2 = (byte)edgeIndex2;
    }

    public byte neighbourTriangleIndex;

    public byte edgeIndex1;
    public byte edgeIndex2;

}
