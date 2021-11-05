using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IChunkGroupOrganizer<T>
    {

        T GetChunkAtLocalPosition(int[] pos);

        void SetChunkAtLocalPosition(int[] pos, T chunk, bool allowOverride);

        int[] GroupRelativeAnchorPosition { get; }

        bool TryGetLeafAtLocalPosition(int[] pos, out T chunk);

        bool HasChunkAtLocalPosition(int[] pos);


        bool RemoveLeafAtLocalPosition(int[] pos);

        int SizePower { get; }

        int[] GroupAnchorPositionCopy { get; }

        int[] GroupAnchorPosition { get; }

    }
}