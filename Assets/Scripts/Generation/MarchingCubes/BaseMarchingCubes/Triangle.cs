using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Triangle
{

    public Vector3 a;
    public Vector3 b;
    public Vector3 c;

    public Vector3Int origin;

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


}
