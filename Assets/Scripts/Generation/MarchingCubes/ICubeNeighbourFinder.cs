using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public interface ICubeNeighbourFinder
    {
        bool IsCubeInBounds(int x, int y, int z);

        MarchingCubeEntity GetEntityInNeighbourAt(Vector3Int outsidePos, Vector3Int offset);

        MarchingCubeEntity GetEntityAt(Vector3Int outsidePos);

    }

}
