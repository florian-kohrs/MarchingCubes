﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtension
{

    public static Vector2 RotateVector(this Vector2 original, float rad, Vector2 offset = default)
    {
        Vector2 result = Vector2.zero;
        float cos = Mathf.Cos(rad * Mathf.PI);
        float sin = Mathf.Sin(rad * Mathf.PI);
        Vector2 rotateCenter = original - offset;
        result.x = (cos * (rotateCenter.x) - sin * rotateCenter.y) + offset.x;
        result.y = (sin * (rotateCenter.x) + cos * rotateCenter.y) + offset.y;
        return result;
    }

    public static Vector3 RotateTowardsAngle(this Vector3 from, Vector3 to, float speed)
    {
        return new Vector3(
            TowardsAngle(from.x, to.x, speed),
            TowardsAngle(from.y, to.y, speed),
            TowardsAngle(from.z, to.z, speed)
        );
    }

    public static Vector3 Mul(this Vector3Int v, float f)
    {
        return new Vector3(v.x * f, v.y * f, v.z * f);
    }

    private static float TowardsAngle(float from, float to, float speed)
    {
        float deltaEuler = from - to;
        if (deltaEuler > 180)
        {
            deltaEuler -= 360;
        }
        else if (deltaEuler < -180)
        {
            deltaEuler += 360;
        }
        float maxMagnitudeDelta = deltaEuler;
        float sign = Mathf.Sign(maxMagnitudeDelta);
        float deltaX = sign * Mathf.Min(Mathf.Abs(maxMagnitudeDelta), speed);
        from += deltaX;
        return from;
    }

    public static Vector3 GetXYZ(this Vector4 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }

    public static Vector3 LerpAngle(this Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            Mathf.LerpAngle(a.x, b.x, t),
            Mathf.LerpAngle(a.y, b.y, t),
            Mathf.LerpAngle(a.z, b.z, t)
        );
    }

    public static Vector3 Abs(this Vector3 v)
    {
        return v.Map(Mathf.Abs);
    }

    public static int[] Ints(this Vector3Int v)
    {
        int[] r = new int[3];
        r[0] = v.x;
        r[1] = v.y;
        r[2] = v.z;
        return r;
    }

    public static float[] Values(this Vector3 v)
    {
        float[] r = new float[3];
        r[0] = v.x;
        r[1] = v.y;
        r[2] = v.z;
        return r;
    }

    public static List<float> ValueList(this Vector3 v)
    {
        return new List<float>() { v.x, v.y, v.z };
    }

    public static bool SharesExactNValuesWith(this Vector3 v1, Vector3 v2, int n)
    {
        int sameValues = 0;

        for (int i = 0; i < 3; i++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (v1[i] == v2[x])
                {
                    sameValues++;
                    if (sameValues == n)
                    {
                        return true;
                    }
                    break;
                }
            }
        }
        return false;
    }

    public static bool SharesExactThisNValuesWith(this Vector3 v1, Vector3 v2, out Vector2Int sharedIndices, int n)
    {
        int sameValues = 0;
        sharedIndices = new Vector2Int();
        for (int i = 0; i < 3; i++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (v1[i] == v2[x])
                {
                    sharedIndices[sameValues] = i;
                    sameValues++;
                    if (sameValues == n)
                    {
                        return true;
                    }
                    break;
                }
            }
        }
        return false;
    }

    public static bool Contains(this Vector3 v1, float f)
    {
        return v1.x == f || v1.y == f || v1.z == f;
    }

    public static bool SharesAnyNValuesWith(this Vector3 v1, Vector3 v2, int n)
    {
        int sameValues = 0;

        for (int i = 0; i < 3 && sameValues < n; i++)
        {
            if (v2.Contains(v1[i]))
            {
                sameValues++;
            }
        }
        return sameValues >= n;
    }

    public static int CountAndMapIndiciesWithSameValues(this Vector3 v1, Vector3 v2, out Vector3Int v1ConnectedVertices, out Vector3Int v2ConnectedVertices)
    {
        int sameValues = 0;
        v1ConnectedVertices = new Vector3Int();
        v2ConnectedVertices = new Vector3Int(-1, -1, -1);
        List<float> list = v2.ValueList();
        for (int i = 0; i < 3; i++)
        {
            v1ConnectedVertices[i] = list.IndexOf(v1[i]);

            if (v1ConnectedVertices[i] >= 0)
            {
                v2ConnectedVertices[v1ConnectedVertices[i]] = i;
                sameValues++;
            }
        }
        return sameValues;
    }

    /// <summary>
    /// creates a vector 2 by filtering values using p
    /// </summary>
    /// <param name="v3"></param>
    /// <returns></returns>
    public static Vector2Int ReduceToVector2(this Vector3Int v3, Func<int, bool> p)
    {
        Vector2Int r = new Vector2Int();
        int used = 0;
        for (int i = 0; i < 3; i++)
        {
            if (p(v3[i]))
            {
                r[used] = v3[i];
                used++;
            }
        }
        return r;
    }

    public static bool SharesNValuesWith(this Vector3 v1, Vector3 v2, int n)
    {
        int sameValues = 0;

        for (int i = 0; i < 3 && sameValues < n; i++)
        {
            if (v1[i] == v2[i])
            {
                sameValues++;
            }
        }
        return sameValues >= n;
    }

    public static bool SharesNValuesInOrderWith(this Vector3 v1, Vector3 v2, int n)
    {
        int sameValues = 0;

        int start = Array.IndexOf(v2.Values(),v1.x);

        for (int i = 0; i < 3 && sameValues < n && start != -1; i++)
        {
            if (v1[i] == v2[(i + start) % 3])
            {
                sameValues++;
            }
        }
        return sameValues >= n;
    }

    public static Vector3Int Min(this Vector3Int v, int min)
    {
        return v.Map((f) => Mathf.Min(f, min));
    }

    public static Vector3Int Max(this Vector3Int v, int min)
    {
        return v.Map((f) => Mathf.Max(f, min));
    }

    /// <summary>
    /// applies Function f to all three coordinates
    /// </summary>
    /// <param name="v"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public static Vector3Int Map(this Vector3Int v, Func<int, int> f)
    {
        return new Vector3Int(f(v.x), f(v.y), f(v.z));
    }

    /// <summary>
    /// applies Function f to all three coordinates
    /// </summary>
    /// <param name="v"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public static Vector3 Map(this Vector3Int v, Func<int, float> f)
    {
        return new Vector3(f(v.x), f(v.y), f(v.z));
    }

    public static Vector3 Min(this Vector3 v, float min)
    {
        return v.Map((f) => Mathf.Min(f, min));
    }

    /// <summary>
    /// applies Function f to all three coordinates
    /// </summary>
    /// <param name="v"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public static Vector3 Map(this Vector3 v, Func<float, float> f)
    {
        return new Vector3(f(v.x), f(v.y), f(v.z));
    }

    public static Vector3 LerpAngleFunc(Vector3 a, Vector3 b, float t)
    {
        return a.LerpAngle(b, t);
    }

    public static Vector3Int ToVector3Int(Vector3 v)
    {
        return new Vector3Int((int)v.x, (int)v.y, (int)v.x);
    }

    public static int ToInt(this Vector3 v, Vector3 size)
    {
        return (int)(v.x * size.x * size.y + v.y * size.y + v.z);
    }

    public static int ToInt(int x, int y, int z, Vector3Int size)
    {
        return (x * size.x * size.y + y * size.y + z);
    }

    public static Vector3Int ToVector(int i, Vector3Int size)
    {
        return new Vector3Int
            (i / (size.x * size.y)
            , (i % (size.x * size.y)) / size.y
            , (i % (size.x * size.y)) % size.y);
    }

    public static Vector3Int[] GetAllCombination(this Vector3Int v)
    {
        Vector3Int[] r = new Vector3Int[7];
        r[0] = new Vector3Int(v.x, int.MinValue, int.MinValue);
        r[1] = new Vector3Int(v.x, v.y, int.MinValue);
        r[2] = new Vector3Int(v.x, v.y, v.z);
        r[3] = new Vector3Int(v.x, int.MinValue, v.z);
        r[4] = new Vector3Int(int.MinValue, v.y, v.z);
        r[5] = new Vector3Int(int.MinValue, v.y, int.MinValue);
        r[6] = new Vector3Int(int.MinValue, int.MinValue, v.z);
        return r;
    }

    public static Vector3Int[] GetAllAdjacentDirections =
        new Vector3Int[] {
            new Vector3Int(1, 0, 0),
            new Vector3Int(- 1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, - 1, 0),
            new Vector3Int(0, 0, + 1),
            new Vector3Int(0, 0, - 1) };
    

    //public static Vector3Int[] GetAllDirectNeighbours(this Vector3Int v3)
    //{
    //    Vector3Int[] r = new Vector3Int[6];
    //    r[0] = new Vector3Int(v3.x + 1, v3.y, v3.z);
    //    r[1] = new Vector3Int(v3.x - 1, v3.y, v3.z);
    //    r[2] = new Vector3Int(v3.x, v3.y + 1, v3.z);
    //    r[3] = new Vector3Int(v3.x, v3.y - 1, v3.z);
    //    r[4] = new Vector3Int(v3.x, v3.y, v3.z + 1);
    //    r[5] = new Vector3Int(v3.x, v3.y, v3.z - 1);
    //    return r;
    //}

}
