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

        public HashSet<IMarchingCubeChunk> lowerChunkLods = new HashSet<IMarchingCubeChunk>();
        public HashSet<IMarchingCubeChunk> increaseChunkLods = new HashSet<IMarchingCubeChunk>();

        Stack<IEnumerator<IMarchingCubeChunk>> checkTheeseChunks = new Stack<IEnumerator<IMarchingCubeChunk>>();

        protected IEnumerator UpdateChunkSizes()
        {
            IEnumerator<IMarchingCubeChunk> chunks;
            IMarchingCubeChunk chunk;
            while (true)
            {
                if (checkTheeseChunks.Count == 0)
                {
                    int count = chunksAtSizes.Length;
                    for (int i = 0; i < count; i++)
                    {
                        checkTheeseChunks.Push(chunksAtSizes[i].GetEnumerator());
                    }
                }
                while (checkTheeseChunks.Count > 0)
                {
                    chunks = checkTheeseChunks.Pop();
                    while (chunks.MoveNext())
                    {
                        if (FrameTimer.MillisecondsSinceFrame >= maxFrameTime)
                        {
                            yield return null;
                        }
                        chunk = chunks.Current;
                        if(CheckSizeOfChunk(chunk))
                        {
                            break;
                        }
                    }
                }
                yield return null;
            }
        }


        protected bool CheckSizeOfChunk(IMarchingCubeChunk c)
        {
            float dist = (c.CenterPos - player.position).magnitude;
            float sizePower = lodPowerAtDistance.Evaluate(dist);
            float distanceToNext = sizePower - (int)sizePower;
            if (distanceToNext < -reduceChunkAtCorrectSizeDist)
            {
                //chunkHandler.GetSplittedNoiseArray(c);
                return true;
            }
            else if (distanceToNext > increaseChunkAtCorrectSizeDist)
            {
                chunkHandler.DecreaseChunkLod(c, c.LODPower + 1);
                return true;
            }
            return false;
        }


        private void Awake()
        {
            for (int i = 0; i <= MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER; i++)
            {
                chunksAtSizes[i] = new HashSet<IMarchingCubeChunk>();
            }
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

                CreateTriggerOfTypeForLod<ChunkLodIncreaseTrigger>(i - 1, f.time - extraTime, increaseTriggerParent);
                CreateTriggerOfTypeForLod<ChunkLodDecreaseTrigger>(i, f.time + extraTime, decreaseTriggerParent);
                
                last = f;
            }
            
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

        /// <summary>
        /// chunks with small sizes have to be checked more frequently if they need to be changed
        /// </summary>
        public HashSet<IMarchingCubeChunk>[] chunksAtSizes = new HashSet<IMarchingCubeChunk>[MarchingCubeChunkHandler.CHUNK_GROUP_SIZE_POWER + 1];

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
