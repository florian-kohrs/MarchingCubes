using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IMarchingCubeChunk : ISizeManager
    {

        bool IsReady { get; }

        bool UseCollider { get; }

        int ChunkSize { get; }

        WorldUpdater ChunkUpdater { set; }

        Vector3Int AnchorPos { get; set; }

        Vector3Int CenterPos { get; }

        bool HasStarted { get; }

        bool[] HasNeighbourInDirection { get; }

        IMarchingCubeChunkHandler ChunkHandler { set; }

        ChunkLodCollider ChunkSimpleCollider { set; }

        void FreeSimpleChunkCollider();

        void PrepareDestruction();

        bool IsEmpty { get; }

        bool IsCompletlyAir { get; }

        float SurfaceLevel { set; }

        int LOD { get; }

        int LODPower { get; set; }

        int TargetLODPower { get; set; }

        Material Material { set; }

        float[] Points { get; set; }

        int PointsPerAxis { get; }

        void ResetChunk();

        bool IsSpawner { get; set; }

        // void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        void InitializeWithMeshDataParallel(TriangleChunkHeap triangleData, Queue<IMarchingCubeChunk> readyChunks);

        void InitializeWithMeshDataParallel(TriangleChunkHeap triangleData, Action<IMarchingCubeChunk> OnChunkDone);

        void InitializeWithMeshData(TriangleChunkHeap triangleData);

        void DestroyChunk();

        void AddDisplayer(MarchingCubeMeshDisplayer b);

        ChunkGroupTreeLeaf Leaf { get; set; }

        void GiveUnusedDisplayerBack();

        //void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel);

        void SetChunkOnMainThread();
    }

}