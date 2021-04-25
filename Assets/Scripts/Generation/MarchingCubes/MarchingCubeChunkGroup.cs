
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeChunkGroup
    {

        int groupLod;

        protected Dictionary<Vector3Int, IMarchingCubeChunk> GroupChunks = new Dictionary<Vector3Int, IMarchingCubeChunk>(Vector3EqualityComparer.instance);

      
        public void InsertChunkAt(Vector3Int coord, IMarchingCubeChunk chunk, int extraSize)
        {
            GroupChunks[coord] = chunk;
            Vector3Int v3 = new Vector3Int();
            for (int x = 1; x <= extraSize; x++)
            {
                v3.x = coord.x + x;
                for (int y = 1; y <= extraSize; y++)
                {
                    v3.y = coord.y + y;
                    for (int z = 1; z <= extraSize; z++)
                    {
                        v3.z = coord.z + z;
                        GroupChunks[v3] = chunk;
                    }
                }
            }
        }


    }
}
