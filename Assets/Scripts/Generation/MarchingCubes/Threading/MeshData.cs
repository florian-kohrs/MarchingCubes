using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MeshData
{

    public int[] triangles;
    public Vector3[] vertices;
    public Color[] colorData;
    public bool useCollider;
    public bool isBorderConnector;

    public MeshData(int[] triangles, Vector3[] vertices, Color[] colorData, bool useCollider, bool isBorderConnector)
    {
        this.triangles = triangles;
        this.vertices = vertices;
        this.colorData = colorData;
        this.useCollider = useCollider;
        this.isBorderConnector = isBorderConnector;
    }

}
