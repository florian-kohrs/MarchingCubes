using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ReadyChunkExchange
    {

        public ReadyChunkExchange(IMarchingCubeChunk old, List<IThreadedMarchingCubeChunk> chunks)
        {
            this.old = new List<IMarchingCubeChunk>() { old };
            this.chunks = chunks;
        }

        public ReadyChunkExchange(List<IMarchingCubeChunk> old, IThreadedMarchingCubeChunk chunks)
        {
            this.old = old ;
            this.chunks = new List<IThreadedMarchingCubeChunk>() { chunks };
        }

        public ReadyChunkExchange(IMarchingCubeChunk old, IThreadedMarchingCubeChunk chunks)
        {
            this.old = new List<IMarchingCubeChunk>() { old };
            this.chunks = new List<IThreadedMarchingCubeChunk>() { chunks };
        }

        public ReadyChunkExchange(List<IMarchingCubeChunk> old, List<IThreadedMarchingCubeChunk> chunks)
        {
            this.old = old;
            this.chunks = chunks;
        }

        public List<IMarchingCubeChunk> old;

        public List<IThreadedMarchingCubeChunk> chunks;

    }
}
