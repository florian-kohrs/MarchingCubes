using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct IndexNeighbourPair
{

    public int first;

    public int second;

    public Vector2Int firstEdgeIndices;
    public Vector2Int sndEdgeIndice;

    public IndexNeighbourPair(int first, int second, Vector2Int firstEdgeIndices, Vector2Int sndEdgeIndice)
    {
        this.first = first;
        this.second = second;
        this.firstEdgeIndices = firstEdgeIndices;
        this.sndEdgeIndice = sndEdgeIndice;
    }


}
