using System;
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
            TowardsAngle(from.x,to.x,speed), 
            TowardsAngle(from.y, to.y, speed), 
            TowardsAngle(from.z, to.z, speed)
        );
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

    public static Vector3 Min(this Vector3 v, float  min)
    {
        return v.Map((f) => Mathf.Min(f, min));
    }

    /// <summary>
    /// applies Function f to all three coordinates
    /// </summary>
    /// <param name="v"></param>
    /// <param name="f"></param>
    /// <returns></returns>
    public static Vector3 Map(this Vector3 v, Func<float,float> f)
    {
        return new Vector3(f(v.x), f(v.y), f(v.z));
    }

    public static Vector3 LerpAngleFunc(Vector3 a, Vector3 b, float t)
    {
        return a.LerpAngle(b, t);
    }

    public static int ToInt(this Vector3 v, Vector3 size)
    {
        return (int)(v.x * size.x * size.y + v.y * size.y + v.z);
    }

    public static int ToInt(int x, int y,  int z, Vector3Int size)
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

}
