using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IMarchingCubeChunk
    {

        bool IsReady { get; }

        float Spacing { set; }

        bool HasStarted { get; }

        void SetActive(bool b);

        Vector3Int ChunkOffset { get; set; }

        IEnumerable<Vector3Int> NeighbourIndices { get; }

        int NeighbourCount { get; }

        bool IsEmpty { get; }

        bool IsCompletlyAir { get; }

        bool IsCompletlySolid { get; }

       // void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        void InitializeWithMeshDataParallel(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel, Action OnDone);
        void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        MarchingCubeEntity GetEntityAt(Vector3Int v3);

        MarchingCubeEntity GetEntityAt(int x, int y, int z);

    }

}