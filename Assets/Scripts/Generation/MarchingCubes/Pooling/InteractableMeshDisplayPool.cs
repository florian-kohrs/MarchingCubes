using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class InteractableMeshDisplayPool : BaseMarchingCubePool<MarchingCubeMeshDisplayer, IMarchingCubeInteractableChunk>
    {

        public InteractableMeshDisplayPool(Transform t)
        {
            transform = t;
        }

        protected Transform transform;


        protected override void ApplyChunkToItem(MarchingCubeMeshDisplayer item, IMarchingCubeInteractableChunk c)
        {
            item.SetInteractableChunk(c);
        }

        protected override MarchingCubeMeshDisplayer CreateItem()
        {
            return new MarchingCubeMeshDisplayer(transform, true);
        }

        protected override void ResetReturnedItem(MarchingCubeMeshDisplayer item)
        {
            item.Reset();
        }

    }
}