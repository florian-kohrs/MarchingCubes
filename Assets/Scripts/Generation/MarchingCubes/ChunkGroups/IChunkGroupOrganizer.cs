using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IChunkGroupOrganizer
    {


        IChunkBuilder ChunkBuilder { set; }


        IMarchingCubeChunk GetChunkAtLocalPosition(int[] pos);

        void SetChunkAtLocalPosition(int[] pos, IMarchingCubeChunk chunk, bool allowOverride);

        int[] GroupRelativeAnchorPosition { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos">relative position from group anchor point</param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        bool TryGetChunkAtLocalPosition(int[] pos, out IMarchingCubeChunk chunk);

        bool HasChunkAtLocalPosition(int[] pos);


        bool RemoveChunkAtLocalPosition(int[] pos);

        int SizePower { get; }

        int[] GroupAnchorPosition { get; }

    }
}