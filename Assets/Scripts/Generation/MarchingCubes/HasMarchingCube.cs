using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class HasMarchingCube : MonoBehaviour, IHasInteractableMarchingCubeChunk
    {

        public MarchingCubeChunk chunk;

        public MarchingCubeChunk GetChunk => chunk;

        public Vector3 NormalFromRay(RaycastHit hit)
        {
            return chunk.NormalFromRay(hit);
        }

    }
}