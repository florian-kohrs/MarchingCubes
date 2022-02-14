using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshData
{

    public Vector3[] vertices;
    public Color32[] colorData;
    public bool useCollider;

    public bool IsEmpty => vertices == null || vertices.Length <= 0;

    public MeshData(Vector3[] vertices, Color32[] colorData, bool useCollider)
    {
        this.vertices = vertices;
        this.colorData = colorData;
        this.useCollider = useCollider;
    }

}
