using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IChunkGroupRoot<T> //: IChunkGroupOrganizer
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        /// <param name="lodPower"></param>
        /// <param name="chunk"></param>
        /// <returns>returns the anchor position of the chunk</returns>
        void SetLeafAtPosition(int[] pos, T chunk, bool allowOverride);


        bool TryGetLeafAtGlobalPosition(Vector3Int pos, out T chunk);


        bool HasChild { get; }

        bool HasChunkAtGlobalPosition(int[] pos);

        bool RemoveChunkAtGlobalPosition(int[] pos);

        bool RemoveChunkAtGlobalPosition(Vector3Int pos);

    }
}