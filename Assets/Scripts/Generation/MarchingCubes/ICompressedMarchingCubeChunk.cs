using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MarchingCubes
{
    public interface ICompressedMarchingCubeChunk : ISizeManager, IEnvironmentSurface
    {

        bool IsReady { get; }

        int NumTris { get; }

        bool UseCollider { get; }

        int ChunkSize { get; }

        WorldUpdater ChunkUpdater { set; }

        Vector3Int CenterPos { get; }

        bool HasStarted { get; }

        bool[] HasNeighbourInDirection { get; }

        IMarchingCubeChunkHandler ChunkHandler { set; }

        ChunkLodCollider ChunkSimpleCollider { set; }

        void FreeSimpleChunkCollider();

        void PrepareDestruction();

        bool IsEmpty { get; }

        int LOD { get; }

        int LODPower { get; set; }

        int TargetLODPower { get; set; }

        Material Material { set; }

        int PointsPerAxis { get; }

        void ResetChunk();

        bool IsSpawner { get; set; }

        // void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        void InitializeWithTriangleDataParallel(TriangleChunkHeap triangleData, Queue<ICompressedMarchingCubeChunk> readyChunks);

        void InitializeWithTriangleDataParallel(TriangleChunkHeap triangleData, Action<ICompressedMarchingCubeChunk> OnChunkDone);


        void InitializeWithTriangleData(TriangleChunkHeap triangleData);

        void InitializeWithMeshData(MeshData meshData, bool buildNeighbours);


        void DestroyChunk();

        void AddDisplayer(MarchingCubeMeshDisplayer b);

        ChunkGroupTreeLeaf Leaf { get; set; }

        void GiveUnusedDisplayerBack();

        //void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel);

        void SetChunkOnMainThread();
    }

}