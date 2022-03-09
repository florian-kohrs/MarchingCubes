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

        public bool TrySetChild(T leaf);

        bool TryGetLeafAtGlobalPosition(int[] pos, out T chunk);


        bool HasChild { get; }

        bool HasChunkAtGlobalPosition(int[] pos);

        bool RemoveChunkAtGlobalPosition(int[] pos);


    }
}