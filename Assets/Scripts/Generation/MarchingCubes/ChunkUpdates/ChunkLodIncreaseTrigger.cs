using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkLodIncreaseTrigger : BaseChunkLodTrigger
    {

        private void OnTriggerEnter(Collider other)
        {
            IMarchingCubeChunk chunk = other.GetComponent<ChunkLodCollider>()?.chunk;
            Debug.Log("Increase");
            if (chunk != null)
            {
                chunk.TargetLODPower = Mathf.Min(chunk.TargetLODPower, lod);
            }
        }

    }
}