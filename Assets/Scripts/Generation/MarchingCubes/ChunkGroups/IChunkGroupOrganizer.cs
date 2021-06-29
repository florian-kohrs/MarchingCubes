using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IChunkGroupOrganizer
    {


        IChunkBuilder ChunkBuilder { set; }


        IMarchingCubeChunk GetChunkAtLocalPosition(Vector3Int pos);

        IMarchingCubeChunk SetChunkAtLocalPosition(Vector3Int pos, int size, int lodPower, IMarchingCubeChunk chunk);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos">relative position from group anchor point</param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        bool TryGetChunkAtLocalPosition(Vector3Int pos, out IMarchingCubeChunk chunk);

        bool HasChunkAt(Vector3Int pos);


        bool RemoveChunkAt(Vector3Int pos);

        int Size { get; } 

        bool IsEmpty { get; }

        Vector3Int GroupAnchorPosition { get; set; }

    }
}