using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct EdgePair
{

    public byte edge1;

    public byte edge2;

    public EdgePair(byte edge1, byte edge2)
    {
        this.edge1 = edge1;
        this.edge2 = edge2;
    }

    public EdgePair(Vector2Int v2) : this((byte)v2.x,(byte)v2.y)
    {
    }

    public EdgePair(int edge1, int edge2) : this((byte)edge1, (byte)edge2)
    {
    }
}
