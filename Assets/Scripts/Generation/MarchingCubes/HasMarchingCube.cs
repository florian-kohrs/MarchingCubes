using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class HasMarchingCube : MonoBehaviour, IHasInteractableMarchingCubeChunk
    {

        public IMarchingCubeInteractableChunk chunk;
        public Vector3Int chunk2;

        private void Update()
        {
            if(chunk != null)
                chunk2 = chunk.AnchorPos;
        }

        public IMarchingCubeInteractableChunk GetChunk => chunk;
    }
}