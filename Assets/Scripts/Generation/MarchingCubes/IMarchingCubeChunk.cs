using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IMarchingCubeChunk
    {

        bool IsReady { get; }

        int ChunkSize { get; set; }

        Vector3Int AnchorPos { get; set; }
        
        Vector3Int CenterPos { get; }

        bool HasStarted { get; }

        bool[] HasNeighbourInDirection { get; }

        IMarchingCubeChunkHandler ChunkHandler {set;}

        bool IsEmpty { get; }

        bool IsCompletlyAir { get; }

        float SurfaceLevel { set; }

        bool IsCompletlySolid { get; }

        int LOD { get; }

        int LODPower { get; set; }

        Material Material { set; }

        float[] Points { get; set; }

        int PointsPerAxis { get; }

       // void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel);

        void InitializeWithMeshDataParallel(TriangleBuilder[] tris, Action OnDone, bool keepPoints);

        void InitializeWithMeshData(TriangleBuilder[] tris, bool keepPoints);

        void ResetChunk();

        void ChangeNeighbourLodTo(int newLodPower, Vector3Int dir);

        void SetLeaf(ChunkGroupTreeLeaf leaf);

        //void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel);

 

    }

}