using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ReadyChunkExchange
    {

        public ReadyChunkExchange(CompressedMarchingCubeChunk old, List<CompressedMarchingCubeChunk> chunks)
        {
            this.old = new List<CompressedMarchingCubeChunk>() { old };
            this.chunks = chunks;
        }

        public ReadyChunkExchange(List<CompressedMarchingCubeChunk> old, CompressedMarchingCubeChunk chunks)
        {
            this.old = old ;
            this.chunks = new List<CompressedMarchingCubeChunk>() { chunks };
        }

        public ReadyChunkExchange(CompressedMarchingCubeChunk old, CompressedMarchingCubeChunk chunks)
        {
            this.old = new List<CompressedMarchingCubeChunk>() { old };
            this.chunks = new List<CompressedMarchingCubeChunk>() { chunks };
        }

        public ReadyChunkExchange(List<CompressedMarchingCubeChunk> old, List<CompressedMarchingCubeChunk> chunks)
        {
            this.old = old;
            this.chunks = chunks;
        }

        public List<CompressedMarchingCubeChunk> old;

        public List<CompressedMarchingCubeChunk> chunks;

    }
}
