using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MeshDisplayerPool : BaseMarchingCubePool<MarchingCubeMeshDisplayer, CompressedMarchingCubeChunk>
    {

        public MeshDisplayerPool(Transform t)
        {
            transform = t;
        }

        protected Transform transform;

        protected override void ApplyChunkToItem(MarchingCubeMeshDisplayer item, CompressedMarchingCubeChunk c)
        {
        }

        protected override MarchingCubeMeshDisplayer CreateItem()
        {
            return new MarchingCubeMeshDisplayer(transform, false);
        }

        protected override void ResetReturnedItem(MarchingCubeMeshDisplayer item)
        {
            item.Reset();
        }

    }
}