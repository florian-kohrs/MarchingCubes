using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IMarchingCubeChunk
    {

        bool IsReady { get; set; }

        int ChunkSize { get; }

        int ChunkSizePower { get; set; }

        int TargetChunkSizePower { get; set; }

        WorldUpdater ChunkUpdater { set; }

        Vector3Int AnchorPos { get; set; }

        Vector3Int CenterPos { get; }

        bool HasStarted { get; }

        bool[] HasNeighbourInDirection { get; }

        IMarchingCubeChunkHandler ChunkHandler { set; }

        public ChunkLodCollider ChunkSimpleCollider { set; }

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

        // void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        void InitializeWithMeshDataParallel(TriangleBuilder[] tris, Queue<IThreadedMarchingCubeChunk> readyChunks, bool keepPoints);

        void InitializeWithMeshData(TriangleBuilder[] tris, bool keepPoints);

        void ResetChunk();

        void ChangeNeighbourLodTo(int newLodPower, Vector3Int dir);

        void SetLeaf(ChunkGroupTreeLeaf leaf);

        ChunkGroupTreeLeaf GetLeaf();

        //void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel);



    }

}