using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vector3EqualityComparer : /*IEqualityComparer<Vector3>,*/ IEqualityComparer<Vector3Int>
{

    public static Vector3EqualityComparer instance;

    static Vector3EqualityComparer()
    {
        instance = new Vector3EqualityComparer();
    }

    public bool Equals(Vector3 x, Vector3 y)
    {
        return x.x == y.x && x.y == y.y && x.z == y.z;
    }

    public bool Equals(Vector3Int x, Vector3Int y)
    {
        return x.x == y.x && x.y == y.y && x.z == y.z;
    }

    public int GetHashCode(Vector3 obj)
    {
        return obj.GetHashCode();
    }

    public int GetHashCode(Vector3Int obj)
    {
        return obj.GetHashCode();
    }
}
