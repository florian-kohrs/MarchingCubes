using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public interface IMarchingCubeInteractableChunk
    {

        PathTriangle GetTriangleFromRayHit(RaycastHit hit);

        MarchingCubeEntity GetClosestEntity(Vector3 v3);

        void EditPointsAroundRayHit(int sign, RaycastHit hit, int editDistance);

        void EditPointsNextToChunk(IMarchingCubeChunk chunk, MarchingCubeEntity e, Vector3Int offset, float delta);

        MarchingCubeEntity GetEntityAt(Vector3Int v3);

        MarchingCubeEntity GetEntityAt(int x, int y, int z);

    }
}