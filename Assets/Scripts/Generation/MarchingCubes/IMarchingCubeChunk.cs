using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public interface IMarchingCubeChunk : ICompressedMarchingCubeChunk, IBlockPlaceOrientator
    {

        float[] Points { get; set; }

        bool HasPoints { get; }


        void StoreChunk(StoredChunkEdits storage);

        PathTriangle GetTriangleFromRayHit(RaycastHit hit);

        MarchingCubeEntity GetClosestEntity(Vector3 v3);

        void EditPointsAroundRayHit(float delta, RaycastHit hit, int editDistance);

        MarchingCubeEntity GetEntityAt(Vector3Int v3);

        MarchingCubeEntity GetEntityAt(int x, int y, int z);

        void RebuildAround(Vector3 offset, int radius, Vector3Int clickedIndex, float delta);


    }
}