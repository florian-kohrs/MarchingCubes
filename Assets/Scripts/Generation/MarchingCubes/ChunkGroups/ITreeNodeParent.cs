using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface ITreeNodeParent<T>
    {

        T[] GetLeafs();

        bool AreAllChildrenLeafs(int targetLodPower);

        int[] GroupRelativeAnchorPosition { get; }

        int[] GroupAnchorPosition { get; }

        Vector3Int GroupAnchorPositionVector { get; }

    }
}