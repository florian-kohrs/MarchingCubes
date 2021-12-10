using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshData
{

    public int[] triangles;
    public Vector3[] vertices;
    public Color32[] colorData;
    public bool useCollider;

    public MeshData(int[] triangles, Vector3[] vertices, Color32[] colorData, bool useCollider)
    {
        this.triangles = triangles;
        this.vertices = vertices;
        this.colorData = colorData;
        this.useCollider = useCollider;
    }

}
