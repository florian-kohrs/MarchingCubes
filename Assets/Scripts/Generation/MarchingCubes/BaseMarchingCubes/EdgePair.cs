using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EdgePair
{

    public int edge1;

    public int edge2;

    public EdgePair(Vector2Int v2) : this(v2.x,v2.y)
    {
    }

    public EdgePair(int edge1, int edge2)
    {
        this.edge1 = edge1;
        this.edge2 = edge2;
    }
}
