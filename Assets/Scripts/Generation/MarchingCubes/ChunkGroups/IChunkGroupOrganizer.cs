using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IChunkGroupOrganizer<T> 
    {

        T GetChunkAtLocalPosition(int[] pos);

        void SetLeafAtLocalPosition(int[] pos, T chunk, bool allowOverride);

        int[] GroupRelativeAnchorPosition { get; }

        bool TryGetLeafAtLocalPosition(int[] pos, out T chunk);
        
        bool HasChunkAtLocalPosition(int[] pos);


        bool RemoveLeafAtLocalPosition(int[] pos);

        int SizePower { get; }

        bool IsLeaf { get; }

        int[] GroupAnchorPositionCopy { get; }

        int[] GroupRelativeAnchorPositionCopy { get; }

        int[] GroupAnchorPosition { get; }



    }
}