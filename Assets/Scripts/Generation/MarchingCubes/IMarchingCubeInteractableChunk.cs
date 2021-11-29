﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public interface IMarchingCubeInteractableChunk : IMarchingCubeChunk, IBlockPlaceOrientator
    {

        PathTriangle GetTriangleFromRayHit(RaycastHit hit);

        MarchingCubeEntity GetClosestEntity(Vector3 v3);

        void EditPointsAroundRayHit(float delta, RaycastHit hit, int editDistance);

        MarchingCubeEntity GetEntityAt(Vector3Int v3);

        MarchingCubeEntity GetEntityAt(int x, int y, int z);

        void RebuildAround(float offsetX, float offsetY, float offsetZ, int radius, int posX, int posY, int posZ, float delta);


    }
}