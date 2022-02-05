using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class WorldUpdater : MonoBehaviour
    {

        public MarchingCubeChunkHandler chunkHandler;

        public Vector3 lastUpdatePosition;

        public Transform player;

        public float updateClosestAfterDistance = 16;

        public float maxFrameTime = 15;

        public float estimatedTimeReduce = 1;
        public float estimatedTimeIncrease = 4;

        public Transform increaseTriggerParent;

        public Transform decreaseTriggerParent;

        public AnimationCurve lodPowerAtDistance;

        public float reduceChunkAtCorrectSizeDist = 0.25f;

        public float increaseChunkAtCorrectSizeDist = 0.25f;

        public Stack<ReadyChunkExchange> readyExchangeChunks = new Stack<ReadyChunkExchange>();

        public void RemoveLowerLodChunk(ICompressedMarchingCubeChunk c)
        {
            if (isInDecreasingChunkIteration)
            {
                removedLowerChunkLodsBuffer.Add(c);
            }
            else
            {
                lowerChunkLods.Remove(c);
            }
        }

        public HashSet<ICompressedMarchingCubeChunk> lowerChunkLods = new HashSet<ICompressedMarchingCubeChunk>();
        public HashSet<ICompressedMarchingCubeChunk> increaseChunkLods = new HashSet<ICompressedMarchingCubeChunk>();

        public HashSet<ICompressedMarchingCubeChunk> removedLowerChunkLodsBuffer = new HashSet<ICompressedMarchingCubeChunk>();
        public HashSet<ICompressedMarchingCubeChunk> increaseChunkLodsBuffer = new HashSet<ICompressedMarchingCubeChunk>();

        protected bool isInIncreasingChunkIteration;
        protected bool isInDecreasingChunkIteration;


        private void Awake()
        {
            lodPowerAtDistance = chunkHandler.lodPowerForDistances;
            GenerateTriggers();
            //StartCoroutine(UpdateChunkSizes());
        }

        protected void GenerateTriggers()
        {
            Keyframe last = lodPowerAtDistance[0];
            float distThreshold = 0.3f;
            for (int i = 1; i < lodPowerAtDistance.length; i++)
            {
                Keyframe f = lodPowerAtDistance.keys[i];

                float timeDiff = f.time - last.time;
                float extraTime = timeDiff * distThreshold;

                CreateTriggerOfTypeForLod<ChunkLodIncreaseTrigger>(i - 1, f.time, increaseTriggerParent);
                CreateTriggerOfTypeForLod<ChunkLodDecreaseTrigger>(i, f.time + extraTime, decreaseTriggerParent);

                last = f;
            }
            float chunkDeactivateDist = chunkHandler.buildAroundDistance * (1 + distThreshold);
            CreateTriggerOfTypeForLod<ChunkLodIncreaseTrigger>(MarchingCubeChunkHandler.MAX_CHUNK_LOD_POWER, chunkHandler.buildAroundDistance, increaseTriggerParent);
            CreateTriggerOfTypeForLod<ChunkLodDecreaseTrigger>(MarchingCubeChunkHandler.MAX_CHUNK_LOD_POWER + 1, chunkDeactivateDist, decreaseTriggerParent);
            CreateTriggerOfTypeForLod<ChunkLodDecreaseTrigger>(MarchingCubeChunkHandler.DESTROY_CHUNK_LOD, chunkDeactivateDist + MarchingCubeChunkHandler.CHUNK_GROUP_SIZE, decreaseTriggerParent);
        }

        protected void CreateTriggerOfTypeForLod<T>(int lod, float radius, Transform parent) where T : BaseChunkLodTrigger
        {
            GameObject g = new GameObject();
            g.layer = 7;
            g.transform.SetParent(parent, false);
            T trigger = g.AddComponent<T>();
            trigger.lod = lod;
            SphereCollider c = g.AddComponent<SphereCollider>();
            c.isTrigger = true;
            c.radius = radius;
        }


        public int maxMillisecondsPerFrame = 30;

        public int stopReducingChunksAtMillisecond = 28;

        public int stopGeneratingChunksAtMillisecond = 12;

        public int stopIncreasingChunkLodsAtMillisecond = 16;


        //TODO: Use Request Async maybe for new chunks
        private void LateUpdate()
        {
            while (readyExchangeChunks.Count > 0 && FrameTimer.HasTimeLeftInFrame)
            {
                ReadyChunkExchange change;
                lock (MarchingCubeChunkHandler.exchangeLocker)
                {
                    change = readyExchangeChunks.Pop();
                }
                List<ICompressedMarchingCubeChunk> chunk = change.chunks;
                List<ICompressedMarchingCubeChunk> olds = change.old;
                
                if (olds[0].IsSpawner)
                {
                    chunkHandler.SpawnEmptyChunksAround(chunk[0]);
                }
                for (int i = 0; i < olds.Count; i++)
                {
                    olds[i].DestroyChunk();
                }
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (chunk[i].IsEmpty && !chunk[i].IsSpawner)
                    {
                        chunk[i].DestroyChunk();
                    }
                    else
                    {
                        chunk[i].SetChunkOnMainThread();
                    }
                }
               
            }


            List<ICompressedMarchingCubeChunk> chunks = new List<ICompressedMarchingCubeChunk>();
            isInIncreasingChunkIteration = true;
            foreach (var item in increaseChunkLods)
            {
                if (FrameTimer.HasTimeLeftInFrame)
                {
                    if (item.IsReady || item.IsSpawner)
                    {
                        chunkHandler.IncreaseChunkLod(item, item.TargetLODPower);
                        chunks.Add(item);
                    }
                }
                else 
                {
                    break;
                }
            }
            isInIncreasingChunkIteration = false;
            foreach (var c in chunks)
            {
                increaseChunkLods.Remove(c);
            }
            chunks.Clear();
            isInDecreasingChunkIteration = true;
            foreach (var item in lowerChunkLods)
            {
                if (FrameTimer.HasTimeLeftInFrame)
                {
                    if (item.IsReady)
                    {
                        bool contains = removedLowerChunkLodsBuffer.Contains(item);
                        if (!item.IsReady || contains)
                            continue;

                        chunkHandler.DecreaseChunkLod(item, item.TargetLODPower);
                    }
                }
                else
                {
                    break;
                }
            }
            isInDecreasingChunkIteration = false;
            foreach (var c in removedLowerChunkLodsBuffer)
            {
                lowerChunkLods.Remove(c);
            }
            removedLowerChunkLodsBuffer.Clear();
        }

    }
}
