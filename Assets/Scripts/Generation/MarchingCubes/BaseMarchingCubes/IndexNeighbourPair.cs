using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct IndexNeighbourPair
{

    public byte first;

    public byte second;

    public EdgePair edge;

    public IndexNeighbourPair(byte first, byte second, byte edge1, byte edge2) 
        : this(first, second, new EdgePair(edge1, edge2))
    {
    }

    public IndexNeighbourPair(byte first, byte second, Vector2Int edge2)
        : this(first, second, new EdgePair(edge2.x, edge2.y))
    {
    }

    public IndexNeighbourPair(byte first, byte second, EdgePair edge)
    {
        this.first = first;
        this.second = second;
        this.edge = edge;
    }

}
