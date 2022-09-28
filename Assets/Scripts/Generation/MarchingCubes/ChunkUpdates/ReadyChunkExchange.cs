using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ReadyChunkExchange
    {

        public ReadyChunkExchange(CompressedMarchingCubeChunk old, List<CompressedMarchingCubeChunk> chunks, IEnumerable<IRegisterableNode> newNodes)
        {
            this.nodes = newNodes;
            this.old = new List<CompressedMarchingCubeChunk>() { old };
            this.chunks = chunks;
            IsMerge = false;
        }

        public ReadyChunkExchange(List<CompressedMarchingCubeChunk> old, CompressedMarchingCubeChunk chunks)
        {
            IsMerge = true;
            nodes = new List<IRegisterableNode>();
            this.old = old ;
            this.chunks = new List<CompressedMarchingCubeChunk>() { chunks };
        }

        public bool IsMerge { get; protected set; }

        public List<CompressedMarchingCubeChunk> old;

        public IEnumerable<IRegisterableNode> nodes;

        public List<CompressedMarchingCubeChunk> chunks;

    }
}
