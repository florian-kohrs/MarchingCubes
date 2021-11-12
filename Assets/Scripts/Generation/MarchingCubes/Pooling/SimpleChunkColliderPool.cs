using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class SimpleChunkColliderPool : BaseMarchingCubePool<ChunkLodCollider, IMarchingCubeChunk>
    {
        public SimpleChunkColliderPool(Transform t)
        {
            colliderParent = t;
        }

        protected Transform colliderParent;

        protected override void ApplyChunkToItem(ChunkLodCollider item, IMarchingCubeChunk c)
        {
            item.chunk = c;
            item.transform.position = c.CenterPos;
            c.ChunkSimpleCollider = item;
            item.coll.enabled = true;
        }

        protected override ChunkLodCollider CreateItem()
        {
            GameObject g = new GameObject();
            SphereCollider sphere = g.AddComponent<SphereCollider>();
            sphere.radius = 1;

            sphere.isTrigger = true;
            ChunkLodCollider coll = g.AddComponent<ChunkLodCollider>();
            coll.coll = sphere;
            //TODO:maybe have layer for each lod level
            g.layer = 6;
            g.transform.SetParent(colliderParent, true);

            return coll;
        }

        protected override void ResetReturnedItem(ChunkLodCollider item)
        {
            item.coll.enabled = false;
            item.chunk = null;
        }

    }
}