using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public interface IMarchingCubeInteractableChunk
    {

        PathTriangle GetTriangleFromRayHit(RaycastHit hit);

        MarchingCubeEntity GetClosestEntity(Vector3 v3);

        void EditPointsAroundRayHit(int sign, RaycastHit hit, int editDistance);
    
    }
}