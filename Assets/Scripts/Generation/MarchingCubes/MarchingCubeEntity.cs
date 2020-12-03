using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubeEntity : ICubeEntity
{

    public List<PathTriangle> triangles = new List<PathTriangle>();

    public Vector3Int origin;

    public int triangulationIndex;

    public bool hasBuildIntern;

    /// <summary>
    /// generate a matrix in the beginning, saying which triangulation has neighbours in which triangulation,
    /// also with offsets at exactly one difference with the abs value of 1 in a single axis 
    /// (see for edgeindex reference:http://paulbourke.net/geometry/polygonise/)
    /// </summary>
    public void BuildInternNeighbours()
    {
        hasBuildIntern = true;
        int count = 0;
        foreach (PathTriangle tri in triangles)
        {
            List<int> neighbourIndices = TriangulationTable.GetInternNeighbourIndiceces(triangulationIndex, count);
            if (neighbourIndices != null)
            {
                foreach (int i in neighbourIndices)
                {
                    triangles[count].BuildNeighboursIn(triangles[i]);
                }
            }
            count++;
        }
    }


    public void BuildExternalNeighbours(MarchingCubeEntity[,,] cubes, System.Func<Vector3Int, bool> IsInBounds)
    {
        if (!hasBuildIntern)
        {
            BuildInternNeighbours();
        }
        int count = 0;
        foreach (PathTriangle tri in triangles)
        {
            foreach (System.Tuple<Vector2Int, Vector3Int> t in TriangulationTable.GetNeighbourOffsetForTriangle(this, count))
            {
                Vector3Int newPos = origin + t.Item2;

                if (IsInBounds(newPos))
                {
                    Vector2Int rotatedEdge = TriangulationTable.RotateEdgeOn(
                        t.Item1,
                        TriangulationTable.GetAxisFromDelta(t.Item2));
                    MarchingCubeEntity neighbourCube = cubes[newPos.x, newPos.y, newPos.z];

                    int i = TriangulationTable.GetIndexWithEdges(neighbourCube.triangulationIndex, rotatedEdge);
                    tri.AddNeighbourTwoWay(neighbourCube.triangles[i]);
                }
            }
            count++;
        }
    }

    public void BuildExternalNeighboursWith(MarchingCubeEntity e, TriangulationTable.MirrorAxis axis)
    {
        bool found = false;
        for (int i = 0; i < triangles.Count && !found; i++)
        {
            PathTriangle tri = triangles[i];
            if (tri.neighbours.Count < 3)
            {
                foreach (Vector2Int v2 in TriangulationTable.GetEdges(TriangulationTable.GetTriangleAt(triangulationIndex, i)))
                {
                    Vector2Int rotatedEdge = TriangulationTable.RotateEdgeOn(v2, axis);

                    int neighbourIndex;
                    if(TriangulationTable.TryGetIndexWithEdges(e.triangulationIndex, rotatedEdge, out neighbourIndex))
                    {
                        tri.AddNeighbourTwoWay(e.triangles[neighbourIndex]);
                        found = true;
                        break;
                    }
                }
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
