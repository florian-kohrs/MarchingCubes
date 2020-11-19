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
        int count = 0;
        foreach (PathTriangle tri in triangles)
        {
            List<int> neighbourIndices = TriangulationTable.GetInternNeighbourIndiceces(triangulationIndex, count);
            if (neighbourIndices != null)
            {
                foreach (int  i in neighbourIndices)
                {
                    triangles[count].BuildNeighboursIn(triangles[i]);
                }
            }
            count++;
        }
    }

    public void BuildExternalNeighboursWith(MarchingCubeEntity e, TriangulationTable.MirrorAxis axis)
    {
        for (int i = 0; i < triangles.Count; i++)
        {
            int? neighbourIndex = TriangulationTable.GetNeighbourIndicesIn(triangulationIndex, i, e.triangulationIndex, axis);
            if(neighbourIndex != null && neighbourIndex.HasValue)
            {
                triangles[i].AddNeighbourTwoWay(triangles[neighbourIndex.Value]);
            }
        }
    }

    public void BuildExternalNeighboursWith(MarchingCubeEntity e)
    {
        TriangulationTable.MirrorAxis axis;
        if (e.origin.x != origin.x)
            axis = TriangulationTable.MirrorAxis.X;
        else if (e.origin.y != origin.y)
            axis = TriangulationTable.MirrorAxis.Y;
        else
            axis = TriangulationTable.MirrorAxis.Z;

        BuildExternalNeighboursWith(e, axis);
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
