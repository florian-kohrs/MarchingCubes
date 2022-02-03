using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkLodIncreaseTrigger : BaseChunkLodTrigger
    {

        private void OnTriggerEnter(Collider other)
        {
            ICompressedMarchingCubeChunk chunk = other.GetComponent<ChunkLodCollider>()?.chunk;
            if (chunk != null)
            {
                chunk.TargetLODPower = Mathf.Min(chunk.TargetLODPower, lod);
            }
        }

    }
}