using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IChunkGroupRoot : IChunkGroupOrganizer
    {
        void SetRootChild(IChunkGroupOrganizer child);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        /// <param name="lodPower"></param>
        /// <param name="chunk"></param>
        /// <returns>returns the anchor position of the chunk</returns>
        Vector3Int SetChunkAtGlobalPosition(Vector3Int pos, int size, int lodPower, IMarchingCubeChunk chunk);

        bool HasChild { get; }
    }
}