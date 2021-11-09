using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IMarchingCubeChunk : ISizeManager
    {

        bool IsReady { get; set; }

        int ChunkSize { get; }

        int TargetChunkSizePower { get; set; }

        WorldUpdater ChunkUpdater { set; }

        Vector3Int AnchorPos { get; set; }

        Vector3Int CenterPos { get; }

        bool HasStarted { get; }

        bool[] HasNeighbourInDirection { get; }

        IMarchingCubeChunkHandler ChunkHandler { set; }

        ChunkLodCollider ChunkSimpleCollider { set; }

        void FreeSimpleChunkCollider();

        void SoftResetMeshDisplayers();

        void PrepareDestruction();

        bool IsEmpty { get; }

        bool IsCompletlyAir { get; }

        float SurfaceLevel { set; }

        bool IsCompletlySolid { get; }

        int LOD { get; }

        int LODPower { get; set; }

        int TargetLODPower { get; set; }

        Material Material { set; }

        float[] Points { get; set; }

        int PointsPerAxis { get; }
        bool IsChanneled { get; set; }
        bool IsSpawner { get; set; }

        // void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        void InitializeWithMeshDataParallel(TriangleChunkHeap triangleData, Queue<IThreadedMarchingCubeChunk> readyChunks);

        void InitializeWithMeshDataParallel(TriangleChunkHeap triangleData, Action<IThreadedMarchingCubeChunk> OnChunkDone);

        void InitializeWithMeshData(TriangleChunkHeap triangleData);

        void ResetChunk(bool removeSimpleCollider = true);

        void AddDisplayer(MarchingCubeMeshDisplayer b);

        void ChangeNeighbourLodTo(int newLodPower, Vector3Int dir);

        void SetLeaf(ChunkGroupTreeLeaf leaf);

        ChunkGroupTreeLeaf GetLeaf();
        void GiveUnusedDisplayerBack();

        //void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel);



    }

}