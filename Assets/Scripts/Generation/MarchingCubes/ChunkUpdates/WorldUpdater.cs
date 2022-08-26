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

        public void SetPlayer(Transform player)
        {
            if (player == null)
            {
                this.player = player;
                GenerateTriggers();
            }
            else
                Debug.LogError("Cant set the player of world " +
                    "updater if it has been set before");
        }

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

        public void RemoveLowerLodChunk(CompressedMarchingCubeChunk c)
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

        public HashSet<CompressedMarchingCubeChunk> lowerChunkLods = new HashSet<CompressedMarchingCubeChunk>();
        public HashSet<CompressedMarchingCubeChunk> increaseChunkLods = new HashSet<CompressedMarchingCubeChunk>();

        public HashSet<CompressedMarchingCubeChunk> removedLowerChunkLodsBuffer = new HashSet<CompressedMarchingCubeChunk>();
        public HashSet<CompressedMarchingCubeChunk> increaseChunkLodsBuffer = new HashSet<CompressedMarchingCubeChunk>();

        protected Stack<ChunkInitializeTask> chunksToInitialize = new Stack<ChunkInitializeTask>();

        protected bool isInIncreasingChunkIteration;
        protected bool isInDecreasingChunkIteration;


        private void Start()
        {
            if(player != null)
            {
                GenerateTriggers();
            }
        }

        public void AddChunkToInitialize(ChunkInitializeTask chunkTask)
        {
            chunksToInitialize.Push(chunkTask);
        }

        protected void GenerateTriggers()
        {
            lodPowerAtDistance = chunkHandler.lodPowerForDistances;

            Keyframe last = lodPowerAtDistance[0];
            float distThreshold = 1.1f;
            for (int i = 1; i < lodPowerAtDistance.length; i++)
            {
                Keyframe f = lodPowerAtDistance.keys[i];

                //float timeDiff = f.time - last.time;
                //float extraTime = timeDiff * distThreshold;

                CreateTriggerOfTypeForLod<ChunkLodIncreaseTrigger>(i - 1, f.time / distThreshold, increaseTriggerParent);
                CreateTriggerOfTypeForLod<ChunkLodDecreaseTrigger>(i, f.time * distThreshold, decreaseTriggerParent);

                //last = f;
            }
            float chunkDeactivateDist = chunkHandler.buildAroundDistance * distThreshold;
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
            while(chunksToInitialize.Count > 0 && FrameTimer.HasTimeLeftInFrame)
            {
                ChunkInitializeTask task = chunksToInitialize.Pop();
                task.chunk.InitializeWithMeshData(task.chunk.MeshData);
                task.onChunkDone(task.chunk);
            }

            while (readyExchangeChunks.Count > 0 && FrameTimer.HasTimeLeftInFrame)
            {
                ReadyChunkExchange change;
                lock (MarchingCubeChunkHandler.exchangeLocker)
                {
                    change = readyExchangeChunks.Pop();
                }
                
                List<CompressedMarchingCubeChunk> chunk = change.chunks;
                 List<CompressedMarchingCubeChunk> olds = change.old;
                
                if(olds.Count == 0)
                {
                    Debug.LogError("old is 0!");
                    continue;
                }

                if (olds[0].IsSpawner)
                {
                    chunkHandler.FindNeighbourOfChunk(chunk[0]);
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
                        chunk[i].BuildEnvironmentForChunk();
                    }
                }
               
            }


            List<CompressedMarchingCubeChunk> chunks = new List<CompressedMarchingCubeChunk>();
            isInIncreasingChunkIteration = true;
            foreach (CompressedMarchingCubeChunk chunk in increaseChunkLods)
            {
                if (FrameTimer.HasTimeLeftInFrame)
                {
                    if (chunk.IsReady || chunk.IsSpawner)
                    {
                        chunkHandler.IncreaseChunkLod(chunk, chunk.TargetLODPower);
                        chunks.Add(chunk);
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

            //TODO: Potential race condition? maybe lock something
            isInDecreasingChunkIteration = true;
            foreach (var item in lowerChunkLods)
            {
                if (FrameTimer.HasTimeLeftInFrame)
                {
                    if (item.IsReady && !removedLowerChunkLodsBuffer.Contains(item))
                    {
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

            FrameTimer.RestartWatch();
        }

    }
}
