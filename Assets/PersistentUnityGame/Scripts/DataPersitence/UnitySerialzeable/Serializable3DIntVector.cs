using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;


[Serializable]
public class Serializable3DIntVector : ISerializable
{

    public override bool Equals(object obj)
    {
        if (obj != null && obj is Serializable3DIntVector)
        {
            return v.Equals(((Serializable3DIntVector)obj).v);
        }
        else
        {
            return v.Equals(obj);
        }
    }

    public override int GetHashCode()
    {
        return v.GetHashCode();
    }

    protected Serializable3DIntVector(SerializationInfo info, StreamingContext context)
    {
        v.x = info.GetInt32("x");
        v.y = info.GetInt32("y");
        v.z = info.GetInt32("z");
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("x", v.x);
        info.AddValue("y", v.y);
        info.AddValue("z", v.z);
    }

    public Vector3Int v;


    public static implicit operator Vector3Int(Serializable3DIntVector vec)
    {
        return vec.v;
    }

    public static implicit operator Serializable3DIntVector(Vector3Int vec)
    {
        return new Serializable3DIntVector(vec);
    }

    public Serializable3DIntVector(int x, int y, int z) : this(new Vector3Int(x,y,z)) { }

    public Serializable3DIntVector(Vector3Int vector)
    {
        v = vector;
    }
    
}
