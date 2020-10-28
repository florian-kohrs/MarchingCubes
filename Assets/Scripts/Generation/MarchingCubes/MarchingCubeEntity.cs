using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubeEntity : ICubeEntity
{

    public List<Triangle> triangles = new List<Triangle>();

    public Vector3Int origin;

    public IList<ICubeEntity> Neighbours
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    public void UpdateMesh()
    {
        throw new System.NotImplementedException();
    }

}
