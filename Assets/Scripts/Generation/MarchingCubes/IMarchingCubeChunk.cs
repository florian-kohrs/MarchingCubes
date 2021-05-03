using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IMarchingCubeChunk 
    {

        bool IsReady { get; }

        int SizeGrower { set; }

        Vector3 AnchorPos { get; set; }

        bool HasStarted { get; }

        Vector3Int ChunkOffset { get; set; }

        IEnumerable<Vector3Int> NeighbourIndices { get; }

        int NeighbourCount { get; }

        bool IsEmpty { get; }

        bool IsCompletlyAir { get; }

        bool IsCompletlySolid { get; }

        int LOD { get; }

        int LODPower { get; set; }

        Material Material { set; }

        float[] Points { get; }

        int PointsPerAxis { get; }

       // void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLod, float surfaceLevel, Action OnDone = null);

        void InitializeWithMeshData(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLod, float surfaceLevel);

        void ResetChunk();

        //void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel);

 

    }

}