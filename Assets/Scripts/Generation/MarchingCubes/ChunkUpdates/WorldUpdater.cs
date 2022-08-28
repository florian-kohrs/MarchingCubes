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

        [SerializeField]
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
            if(updateRoutine.updateDone)
            {
                HandleChunkChanges();
            }
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

        protected Stack<ChunkGroupRoot> destroyRoots;
        protected Stack<ChunkGroupRoot> deactivateRoots;
        protected Stack<ChunkGroupTreeNode> mergeSet;
        protected Stack<ChunkSplitExchange> splitSet;

        private void Awake()
        {
            float distThreshold = 1.1f;
            float chunkDeactivateDist = chunkHandler.buildAroundDistance * distThreshold;
            float chunkDestroyDistance = chunkDeactivateDist + MarchingCubeChunkHandler.CHUNK_GROUP_SIZE;
            ChunkUpdateValues updateValues = new ChunkUpdateValues(500, chunkDeactivateDist, chunkDestroyDistance,
                new float[] { 200, 350, 600, 1000, 2000 }, 1.1f);

            destroyRoots = new Stack<ChunkGroupRoot>();
            deactivateRoots = new Stack<ChunkGroupRoot>();
            mergeSet = new Stack<ChunkGroupTreeNode>();
            splitSet = new Stack<ChunkSplitExchange>();
            updateRoutine = new ChunkUpdateRoutine(deactivateRoots, deactivateRoots, mergeSet, splitSet, updateValues);
        
            chunkHandler.OnInitializationDoneCallback.Add(delegate { updateRoutine.BeginAsynchrounLodCheck(); });
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

        protected void HandleChunkChanges()
        {
            DestroyOutOfRangeChunks();
            DeactivateOutOfRangeChunks();
            MergeNode();
            SplitLeafs();
        }

        protected void DestroyOutOfRangeChunks()
        {
            lock (updateRoutine.mutexLockDestroy)
            {
                while (destroyRoots.Count > 0)
                    destroyRoots.Pop().DestroyBranch();
            }
        }

        protected void DeactivateOutOfRangeChunks()
        {
            lock (updateRoutine.mutexLockDeactivate)
            {
                while (deactivateRoots.Count > 0)
                    deactivateRoots.Pop().DeactivateBranch();
            }
        }

        protected void ActivateRoot()
        {

        }

        protected void MergeNode()
        {
            lock (updateRoutine.mutexLockMerge)
            {
                while (mergeSet.Count > 0)
                    HandleMerge(mergeSet.Pop());
            }
        }

        protected void SplitLeafs()
        {
            lock (updateRoutine.mutexLockSplit)
            {
                while (splitSet.Count > 0)
                    HandleSplit(splitSet.Pop());
            }
        }

        protected void HandleMerge(ChunkGroupTreeNode node)
        {
            chunkHandler.MergeAndReduceChunkNode(node);
        }

        protected void HandleSplit(ChunkSplitExchange split)
        {
            chunkHandler.SplitChunkLeaf(split);
        }

    }
}
