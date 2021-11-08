using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [System.Serializable]
    public abstract class BaseChunkGroupOrganizer<T> : IChunkGroupOrganizer<T>
    {

        public int[] GroupAnchorPosition { get; set; }

        public int[] GroupAnchorPositionCopy
        {
            get
            {
                return new int[] { GroupAnchorPosition[0], GroupAnchorPosition[1], GroupAnchorPosition[2] };
            }
        }

        public abstract int[] GroupRelativeAnchorPosition { get; }

        public Vector3Int GroupAnchorPositionVector { get => new Vector3Int(GroupAnchorPosition[0], GroupAnchorPosition[1], GroupAnchorPosition[2]); }

        public abstract int SizePower { get; }

        public abstract bool IsLeaf { get; }

        public abstract T GetChunkAtLocalPosition(int[] pos);
        public abstract void SetChunkAtLocalPosition(int[] pos, T chunk, bool allowOverride);
        public abstract bool TryGetLeafAtLocalPosition(int[] pos, out T chunk);
        public abstract bool HasChunkAtLocalPosition(int[] pos);
        public abstract bool RemoveLeafAtLocalPosition(int[] pos);
    }
}