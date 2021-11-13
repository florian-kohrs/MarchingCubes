using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MeshDisplayerPool : BaseMarchingCubePool<MarchingCubeMeshDisplayer, IMarchingCubeChunk>
    {

        public MeshDisplayerPool(Transform t)
        {
            transform = t;
        }

        protected Transform transform;

        protected int creationCount = 0;

        protected override void ApplyChunkToItem(MarchingCubeMeshDisplayer item, IMarchingCubeChunk c)
        {
        }

        protected override MarchingCubeMeshDisplayer CreateItem()
        {
            creationCount++;
            Debug.Log("Created default number " + creationCount);
            return new MarchingCubeMeshDisplayer(transform, false);
        }

        protected override void ResetReturnedItem(MarchingCubeMeshDisplayer item)
        {
            item.Reset();
        }

    }
}