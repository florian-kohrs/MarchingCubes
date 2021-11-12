using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ReadyChunkExchange
    {

        public ReadyChunkExchange(IMarchingCubeChunk old, List<IMarchingCubeChunk> chunks)
        {
            this.old = new List<IMarchingCubeChunk>() { old };
            this.chunks = chunks;
        }

        public ReadyChunkExchange(List<IMarchingCubeChunk> old, IMarchingCubeChunk chunks)
        {
            this.old = old ;
            this.chunks = new List<IMarchingCubeChunk>() { chunks };
        }

        public ReadyChunkExchange(IMarchingCubeChunk old, IMarchingCubeChunk chunks)
        {
            this.old = new List<IMarchingCubeChunk>() { old };
            this.chunks = new List<IMarchingCubeChunk>() { chunks };
        }

        public ReadyChunkExchange(List<IMarchingCubeChunk> old, List<IMarchingCubeChunk> chunks)
        {
            this.old = old;
            this.chunks = chunks;
        }

        public List<IMarchingCubeChunk> old;

        public List<IMarchingCubeChunk> chunks;

    }
}
