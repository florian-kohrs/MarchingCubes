using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class WorldUpdater : MonoBehaviour
    {

        public MarchingCubeChunkHandler chunkHandler;

        public Vector3 lastUpdatePosition;

        public ChunkUpdateRoutine updateRoutine;

        protected Transform player;

        public void SetPlayer(Transform player)
        {
            if (player == null)
            {
                this.player = player;
            }
            else
                Debug.LogError("Cant set the player of world " +
                    "updater if it has been set before");
        }

        private void FixedUpdate()
        {
            updateRoutine.playerPos = player.position;
            updateRoutine.update = true;
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


        protected Stack<ChunkInitializeTask> chunksToInitialize = new Stack<ChunkInitializeTask>();

        protected bool isInIncreasingChunkIteration;
        protected bool isInDecreasingChunkIteration;

        protected HashSet<ChunkGroupRoot> destroyRoots;
        protected HashSet<ChunkGroupRoot> deactivateRoots;
        protected HashSet<ChunkGroupTreeNode> mergeSet;
        protected HashSet<ChunkGroupTreeLeaf> splitSet;

        private void Awake()
        {
            float distThreshold = 1.1f;
            float chunkDeactivateDist = chunkHandler.buildAroundDistance * distThreshold;
            float chunkDestroyDistance = chunkDeactivateDist + MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;
            ChunkUpdateValues updateValues = new ChunkUpdateValues(500, chunkDeactivateDist, chunkDestroyDistance,
                new float[] {}, new float[] {});

            destroyRoots = new HashSet<ChunkGroupRoot>();
            deactivateRoots = new HashSet<ChunkGroupRoot>();
            mergeSet = new HashSet<ChunkGroupTreeNode>();
            splitSet = new HashSet<ChunkGroupTreeLeaf>();
            updateRoutine = new ChunkUpdateRoutine(deactivateRoots, deactivateRoots, mergeSet, splitSet,);
        }

        public void AddChunkToInitialize(ChunkInitializeTask chunkTask)
        {
            chunksToInitialize.Push(chunkTask);
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


            isInIncreasingChunkIteration = true;
            

            isInIncreasingChunkIteration = false;
            
            //TODO: Potential race condition? maybe lock something
            isInDecreasingChunkIteration = true;
            
            isInDecreasingChunkIteration = false;
           

            FrameTimer.RestartWatch();
        }

    }
}
