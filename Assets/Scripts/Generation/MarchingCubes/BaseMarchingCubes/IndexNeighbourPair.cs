using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct IndexNeighbourPair
{

    public int first;

    public int second;

    public EdgePair edge;

    public IndexNeighbourPair(int first, int second, EdgePair edge)
    {
        this.first = first;
        this.second = second;
        this.edge = edge;
    }

    public IndexNeighbourPair(int first, int second, Vector2Int edge) : this(first, second, new EdgePair(edge))
    {
    }

}
