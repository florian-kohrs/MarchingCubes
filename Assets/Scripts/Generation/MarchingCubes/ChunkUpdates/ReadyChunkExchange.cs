using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ReadyChunkExchange
    {
        public ReadyChunkExchange(IMarchingCubeChunk old, List<IThreadedMarchingCubeChunk> chunks)
        {
            this.old = old;
            this.chunks = chunks;
        }

        public IMarchingCubeChunk old;

        public List<IThreadedMarchingCubeChunk> chunks;

    }
}
