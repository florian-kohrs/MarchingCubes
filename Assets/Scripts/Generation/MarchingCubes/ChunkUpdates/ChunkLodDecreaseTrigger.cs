using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkLodDecreaseTrigger : BaseChunkLodTrigger
    {

        private void OnTriggerExit(Collider other)
        {
            CompressedMarchingCubeChunk chunk = other.GetComponent<ChunkLodCollider>()?.chunk;
            if (chunk != null)
            {
                chunk.TargetLODPower = Mathf.Max(chunk.TargetLODPower, lod);
            }
        }

    }

}
