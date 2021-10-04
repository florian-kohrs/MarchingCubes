using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public interface IMarchingCubeInteractableChunk : IMarchingCubeChunk
    {

        PathTriangle GetTriangleFromRayHit(RaycastHit hit);

        MarchingCubeEntity GetClosestEntity(Vector3 v3);

        void EditPointsAroundRayHit(float delta, RaycastHit hit, int editDistance);

        MarchingCubeEntity GetEntityAt(Vector3Int v3);

        MarchingCubeEntity GetEntityAt(int x, int y, int z);

        IMarchingCubeChunkHandler GetChunkHandler { get; }

        void RebuildAround(List<Vector3Int> changedPoints);

        int PointIndexFromCoord(Vector3Int v);

        bool IsPointInBounds(int x, int y, int z);

        bool IsPointInBounds(Vector3Int v);

    }
}