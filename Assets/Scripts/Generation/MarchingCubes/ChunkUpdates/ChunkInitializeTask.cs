using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{
    public class ChunkInitializeTask
    {

        public ChunkInitializeTask(System.Action<CompressedMarchingCubeChunk> onChunkDone, CompressedMarchingCubeChunk chunk)
        {
            this.chunk = chunk;
            this.onChunkDone = onChunkDone;
        }

        public System.Action<CompressedMarchingCubeChunk> onChunkDone;

        public CompressedMarchingCubeChunk chunk;

       
    }
}
