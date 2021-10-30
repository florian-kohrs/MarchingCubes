using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class WorldUpdater : MonoBehaviour
    {

        public MarchingCubeChunkHandler chunkHandler;

        public Vector3 lastUpdatePosition;

        public float updateClosestAfterDistance = 16;

        void Update()
        {
            
        }

        private void Awake()
        {
            for (int i = 0; i < MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER; i++)
            {
                chunksAtSizes[i] = new HashSet<IMarchingCubeChunk>();
            }
        }


        /// <summary>
        /// chunks with small sizes have to be checked more frequently if they need to be changed
        /// </summary>
        public HashSet<IMarchingCubeChunk>[] chunksAtSizes = new HashSet<IMarchingCubeChunk>[MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER];

        public void AddChunk(IMarchingCubeChunk chunk)
        {
            chunksAtSizes[chunk.ChunkSizePower].Add(chunk);
        }

        public void RemoveChunkUnsafe(IMarchingCubeChunk chunk)
        {
            chunksAtSizes[chunk.ChunkSizePower].Remove(chunk);
        }

        public int maxMillisecondsPerFrame = 30;

        public int stopReducingChunksAtMillisecond = 28;

        public int stopGeneratingChunksAtMillisecond = 12;

        public int stopIncreasingChunkLodsAtMillisecond = 16;

        private void LateUpdate()
        {
            
        }

    }
}
