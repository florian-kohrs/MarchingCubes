using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Vector3Byte
{

    public Vector3Byte(Vector3Int v3)
    {
        x = (byte)v3.x;
        y = (byte)v3.y;
        z = (byte)v3.z;
    }

    public byte x;

    public byte y;

    public byte z;

}
