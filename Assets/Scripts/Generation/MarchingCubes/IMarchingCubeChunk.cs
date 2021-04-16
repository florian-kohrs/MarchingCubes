﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IMarchingCubeChunk 
    {

        bool IsReady { get; }

        float Spacing { set; }

        Vector3 AnchorPos { set; }

        bool HasStarted { get; }

        Vector3Int ChunkOffset { get; set; }

        IEnumerable<Vector3Int> NeighbourIndices { get; }

        int NeighbourCount { get; }

        bool IsEmpty { get; }

        bool IsCompletlyAir { get; }

        bool IsCompletlySolid { get; }

        int LOD { get; set; }

        Material Material { set; }

        float[] Points { get; }

       // void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLod, float surfaceLevel, Action OnDone);

        void InitializeWithMeshData(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLod, float surfaceLevel);

        //void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel);

 

    }

}