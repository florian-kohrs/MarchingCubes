using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class WorldUpdater : MonoBehaviour
    {

        public MarchingCubeChunkHandler chunkHandler;

        public ChunkUpdateRoutine updateRoutine;

        [HideInInspector]
        public List<ChunkGroupTreeLeaf> leafsWithEmptyChunks = new List<ChunkGroupTreeLeaf>();

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

        public float reduceChunkAtCorrectSizeDist = 0.25f;

        public float increaseChunkAtCorrectSizeDist = 0.25f;

        public Stack<ReadyChunkExchange> readyExchangeChunks = new Stack<ReadyChunkExchange>();


        protected Stack<ChunkInitializeTask> chunksToInitialize = new Stack<ChunkInitializeTask>();

        protected Stack<ChunkGroupTreeNode> destroyRoots;
        protected Stack<ChunkGroupTreeNode> deactivateRoots;
        protected Stack<ChunkGroupTreeNode> reactivateRoots;
        protected Stack<ChunkGroupTreeNode> mergeSet;
        protected Stack<ChunkSplitExchange> splitSet;

        private void Awake()
        {
            chunkHandler.OnInitializationDoneCallback.Add(BeginChunkUpdates);
        }


        protected void BeginChunkUpdates()
        {
            updateRoutine.BeginAsynchrounLodCheck();
        }

        public void ClearLeafesWithoutValue()
        {
            for (int i = 0; i < leafsWithEmptyChunks.Count; i++)
            {
                leafsWithEmptyChunks[i].DestroyLeaf();
            }
        }

        public void InitializeUpdateRoutine(ChunkUpdateValues updateValues)
        {
            destroyRoots = new Stack<ChunkGroupTreeNode>();
            deactivateRoots = new Stack<ChunkGroupTreeNode>();
            reactivateRoots = new Stack<ChunkGroupTreeNode>();
            mergeSet = new Stack<ChunkGroupTreeNode>();
            splitSet = new Stack<ChunkSplitExchange>();
            updateRoutine = new ChunkUpdateRoutine(deactivateRoots, deactivateRoots, reactivateRoots, mergeSet, splitSet, updateValues);
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
                task.chunk.InitializeWithMeshDataAsync(task.chunk.MeshData, task.onChunkDone);
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

                if (olds.Count == 0)
                {
                    Debug.LogError("old is 0!");
                    continue;
                }

                for (int i = 0; i < olds.Count; i++)
                {
                    olds[i].DestroyChunk();
                }
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (chunk[i].IsEmpty)
                    {
                        chunk[i].DestroyChunk();
                    }
                    else
                    {
                        chunk[i].ApplyMeshCollider();
                        chunk[i].Leaf.Register();
                        chunk[i].BuildEnvironmentForChunk();
                    }
                    
                }
                foreach (var item in change.nodes)
                {
                    item.Register();
                }
               
            }

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
