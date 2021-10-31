using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class ChunkLodDecreaseTrigger : BaseChunkLodTrigger
    {

        private void OnTriggerExit(Collider other)
        {
            IMarchingCubeChunk chunk = other.GetComponent<ChunkLodCollider>()?.chunk;
            Debug.Log("Decrease");
            if (chunk != null)
            {
                chunk.TargetLODPower = Mathf.Max(chunk.TargetLODPower, lod);
            }
        }

    }

}
