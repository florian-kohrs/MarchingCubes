using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Triangle
{

    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3Int origin;
    public int triangulationIndex;

    public Vector3 this[int i]
    {
        get
        {
            switch (i)
            {
                case 0:
                    return a;
                case 1:
                    return b;
                default:
                    return c;
            }
        }
    }

    public bool Contains(Vector3 v)
    {
        return this[0] == v || this[1] == v || this[2] == v;
    }

}
