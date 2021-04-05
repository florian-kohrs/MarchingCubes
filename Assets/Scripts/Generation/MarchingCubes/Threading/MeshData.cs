﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshData
{

    public int[] triangles;
    public Vector3[] vertices;
    public Color[] colorData;

    public MeshData(int[] triangles, Vector3[] vertices, Color[] colorData)
    {
        this.triangles = triangles;
        this.vertices = vertices;
        this.colorData = colorData;
    }
}