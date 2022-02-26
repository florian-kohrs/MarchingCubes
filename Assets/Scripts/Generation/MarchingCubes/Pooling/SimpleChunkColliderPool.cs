using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class SimpleChunkColliderPool : BaseMarchingCubePool<ChunkLodCollider, CompressedMarchingCubeChunk>
    {
        public SimpleChunkColliderPool(Transform t)
        {
            colliderParent = t;
        }

        protected Transform colliderParent;

        protected override void ApplyChunkToItem(ChunkLodCollider item, CompressedMarchingCubeChunk c)
        {
            item.chunk = c;
            item.transform.position = c.CenterPos;
            c.ChunkSimpleCollider = item;
            item.coll.enabled = true;
        }

        protected override ChunkLodCollider CreateItem()
        {
            GameObject g = new GameObject();
            g.transform.SetParent(colliderParent, true);
            //TODO:maybe have layer for each lod level
            g.layer = 6;

            SphereCollider sphere = g.AddComponent<SphereCollider>();
            sphere.radius = 1;
            sphere.isTrigger = true;

            ChunkLodCollider coll = g.AddComponent<ChunkLodCollider>();
            coll.coll = sphere;
           
            return coll;
        }

        protected override void ResetReturnedItem(ChunkLodCollider item)
        {
            item.coll.enabled = false;
            item.chunk = null;
        }

    }
}