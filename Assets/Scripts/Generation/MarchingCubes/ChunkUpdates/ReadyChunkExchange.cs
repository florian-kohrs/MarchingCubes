using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ReadyChunkExchange
    {

        public ReadyChunkExchange(ICompressedMarchingCubeChunk old, List<ICompressedMarchingCubeChunk> chunks)
        {
            this.old = new List<ICompressedMarchingCubeChunk>() { old };
            this.chunks = chunks;
        }

        public ReadyChunkExchange(List<ICompressedMarchingCubeChunk> old, ICompressedMarchingCubeChunk chunks)
        {
            this.old = old ;
            this.chunks = new List<ICompressedMarchingCubeChunk>() { chunks };
        }

        public ReadyChunkExchange(ICompressedMarchingCubeChunk old, ICompressedMarchingCubeChunk chunks)
        {
            this.old = new List<ICompressedMarchingCubeChunk>() { old };
            this.chunks = new List<ICompressedMarchingCubeChunk>() { chunks };
        }

        public ReadyChunkExchange(List<ICompressedMarchingCubeChunk> old, List<ICompressedMarchingCubeChunk> chunks)
        {
            this.old = old;
            this.chunks = chunks;
        }

        public List<ICompressedMarchingCubeChunk> old;

        public List<ICompressedMarchingCubeChunk> chunks;

    }
}
