using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubeVertex : IVertex
{

    protected Vector3 vertex;

    public Vector3 Vertex
    {
        get
        {
            return vertex;
        }
        set
        {
            vertex = value;
        }
    }
}