using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{
    public interface ICubeEntity
    {

        List<PathTriangle> GetNeighboursOf(PathTriangle tri);

        Vector3Int Origin { get; }

        int IndexOfTri(PathTriangle tri);

        int TriangulationIndex { get; }

    }
}