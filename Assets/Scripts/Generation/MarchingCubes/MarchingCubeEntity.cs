using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubeEntity : ICubeEntity
{

    public List<PathTriangle> triangles = new List<PathTriangle>();

    public Vector3Int origin;

    public int triangulationIndex;


    /// <summary>
    /// generate a matrix in the beginning, saying which triangulation has neighbours in which triangulation,
    /// also with offsets at exactly one difference with the abs value of 1 in a single axis 
    /// (see for edgeindex reference:http://paulbourke.net/geometry/polygonise/)
    /// </summary>
    public void BuildInternNeighbours()
    {
        for (int i = 0; i < triangles.Count-1; i++)
        {
            for (int x = i + 1; x < triangles.Count; x++)
            {
                triangles[i].BuildNeighboursIn(triangles[x]);
            }
        }
    }

    public void BuildExternalNeighboursWith(MarchingCubeEntity e)
    {
        ///maybe stop after finding one neighbour, because it can only have one neighbour?
        foreach (PathTriangle t1 in triangles)
        {
            foreach (PathTriangle t2 in e.triangles)
            {
                t1.BuildNeighboursIn(t2);
            }
        }
    }

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
