using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkUpdateRoutine
    {

        protected static ChunkUpdateRoutine instance;

        /// <summary>
        /// check each array entry representing all nodes of the same groupsize for any node to merge its children becoming a leaf itself 
        /// </summary>
        public static HashSet<ChunkGroupTreeNode>[] chunkGroupNodes = new HashSet<ChunkGroupTreeNode>[MarchingCubeChunkHandler.MAX_CHUNK_LOD_POWER];

        /// <summary>
        /// check this hashset for leafs to split 
        /// </summary>
        public static HashSet<ChunkGroupTreeLeaf>[] chunkGroupTreeLeaves = new HashSet<ChunkGroupTreeLeaf>[MarchingCubeChunkHandler.MAX_CHUNK_LOD_POWER];
        
        /// <summary>
        /// Check this hashset for chunks to disable or delete altogether
        /// </summary>
        public static HashSet<ChunkGroupRoot> chunkGroupTreeRoots = new HashSet<ChunkGroupRoot>();

        /// <summary>
        /// Distance the player has to move from last checked position to trigger another routine check
        /// Sqr is calculated in static constructor
        /// </summary>
        public static int[] SQR_DISTANCE_LOD_UPDATES = new int[] { 5, 10 ,15, 25, 50};

        public static Vector3[] lastTimeCheckedPositions = new Vector3[SQR_DISTANCE_LOD_UPDATES.Length];

        public Vector3 playerPos;

        protected Stack<ChunkGroupRoot> destroySet;
        protected Stack<ChunkGroupRoot> deactivateSet;
        protected Stack<ChunkGroupTreeNode> mergeSet;
        protected Stack<ChunkSplitExchange> splitSet;

        protected ChunkUpdateValues updateValues;

        public bool update;
        public bool updateDone;

        protected int ChunkCheckIntervalInMs => updateValues.ChunkCheckIntervalInMs;
        protected float SqrDeactivateDistance => updateValues.SqrDeactivateDistance;
        protected float SqrDestroyDistance => updateValues.SqrDestroyDistance;

        public float[] SqrMergeDistanceRequirement => updateValues.sqrMergeDistanceRequirement;

        public float[] SqrSplitDistanceRequirement => updateValues.sqrSplitDistanceRequirement;

        public object mutexLockMerge = new object();
        public object mutexLockSplit = new object();
        public object mutexLockDestroy = new object();
        public object mutexLockDeactivate = new object(); 
        
        public object mutexLockLeafs = new object();
        public object mutexLockRoots = new object();
        public object mutexLockNodes = new object();

        static ChunkUpdateRoutine()
        {
            for (int i = 0; i < SQR_DISTANCE_LOD_UPDATES.Length; i++)
            {
                SQR_DISTANCE_LOD_UPDATES[i] *= SQR_DISTANCE_LOD_UPDATES[i];
            }
        }

        public ChunkUpdateRoutine(Stack<ChunkGroupRoot> destroySet, Stack<ChunkGroupRoot> deactivateSet, Stack<ChunkGroupTreeNode> mergeSet, Stack<ChunkSplitExchange> splitSet, ChunkUpdateValues updateValues)
        {
            if (instance != null)
                throw new System.Exception("There can be only one " + nameof(ChunkUpdateRoutine));
            instance = this;
            this.destroySet = destroySet;
            this.deactivateSet = deactivateSet;
            this.mergeSet = mergeSet;
            this.updateValues = updateValues;
            this.splitSet = splitSet;
            PrepareChunkNodeRegister();
        }

        public static int GetLodPowerForPosition(Vector3 pos)
        {
            return instance.updateValues.GetLodForSqrDistance(Vector3.SqrMagnitude(pos - instance.playerPos));
        }

        public void BeginAsynchrounLodCheck()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    RunRoutine();
                }
                catch (System.Exception x)
                {
                }
            });
        }

        protected void RunRoutine()
        {
            if(update)
            {
                updateDone = false; 
                update = false;
                //UpdateAll();
                CheckAllLodsForUpdate();
            }
            Thread.Sleep(ChunkCheckIntervalInMs);
            RunRoutine();
        }

        protected void UpdateAll()
        {
            CheckRootsForDestruction();
            CheckRootsForDeactivation();
            CheckAllLodsForUpdate();
            updateDone = true;
        }


        protected void PrepareChunkNodeRegister()
        {
            for (int i = 0; i < MarchingCubeChunkHandler.MAX_CHUNK_LOD_POWER; i++)
            {
                chunkGroupNodes[i] = new HashSet<ChunkGroupTreeNode>();
                chunkGroupTreeLeaves[i] = new HashSet<ChunkGroupTreeLeaf>();
            }
        }

        protected T[] GetCopiedHashset<T>(HashSet<T> set, object lockObject)
        {
            T[] result = new T[set.Count];
            lock (lockObject)
            {
                set.CopyTo(result);
            }
            return result;
        }

        protected void CheckRootsForDestruction()
        {
            foreach (var item in GetCopiedHashset(chunkGroupTreeRoots, mutexLockRoots))
            {
                if (CheckChunkForDestruction(item.Center))
                {
                    item.SetChannelChunkForDestruction();
                    destroySet.Push(item);
                }
            }
        }

        protected void CheckRootsForDeactivation()
        {
            foreach (var item in GetCopiedHashset(chunkGroupTreeRoots, mutexLockRoots))
            {
                if (!item.ChanneledForDestruction && !item.ChanneledForDeactivation && CheckChunkForDeactivation(item.Center))
                {
                    item.SetChannelChunkForDeactivation();
                    deactivateSet.Push(item);
                }
            }
        }

        protected void CheckAllLodsForUpdate(bool force = false)
        {
            CheckAllLodsForUpdate(0, force);
        }

        protected void CheckAllLodsForUpdate(int index, bool force = false)
        {
            if (index >= SQR_DISTANCE_LOD_UPDATES.Length || (!force && !CheckLod(index)))
                    return;

            ///only check next lod if current lod is going to be updated
            CheckAllLodsForUpdate(index + 1, force);
            CheckLodRoutineMerge(index);
            CheckLodRoutineSplit(index);
        }

        protected void CheckLodRoutineMerge(int index)
        {
            lastTimeCheckedPositions[index] = playerPos;
            ChunkGroupTreeNode[] currentSet = GetCopiedHashset(chunkGroupNodes[index], mutexLockNodes);
            foreach (ChunkGroupTreeNode chunk in currentSet)
            {
                CheckNodeGroupForMerge(index, chunk);
            }
        }

        protected void CheckLodRoutineSplit(int index)
        {
            lastTimeCheckedPositions[index] = playerPos;
            ChunkGroupTreeLeaf[] currentSet = GetCopiedHashset(chunkGroupTreeLeaves[index], mutexLockLeafs);
            foreach (ChunkGroupTreeLeaf chunk in currentSet)
            {
                CheckNodeGroupForSplit(index, chunk);
            }
        }

        protected void CheckNodeGroupForMerge(int index, ChunkGroupTreeNode node)
        {
            if (!CheckNodeForMerge(index, node.Center))
                return;

            lock (mutexLockMerge)
            {
                mergeSet.Push(node);
            }
        }

        protected void CheckNodeGroupForSplit(int index, ChunkGroupTreeLeaf leaf)
        {
            if (!CheckLeafForSplit(index, leaf.Center))
                return;
            leaf.parent.SplitChildAtIndex(leaf.ChildIndex, out List<ChunkGroupTreeNode> newNodes);
            ChunkSplitExchange exchange = new ChunkSplitExchange(leaf, newNodes);
            lock (mutexLockSplit)
            {
                splitSet.Push(exchange);
            }
        }

        protected bool CheckLod(int index) => 
            Vector3.SqrMagnitude(lastTimeCheckedPositions[index] - playerPos) >= SQR_DISTANCE_LOD_UPDATES[index];

        protected bool CheckChunkForDestruction(Vector3 center) =>
          Vector3.SqrMagnitude(center - playerPos) >= SqrDestroyDistance;

        protected bool CheckChunkForDeactivation(Vector3 center) =>
            Vector3.SqrMagnitude(center - playerPos) >= SqrDeactivateDistance;


        protected bool CheckNodeForMerge(int index, Vector3 center) =>
            Vector3.SqrMagnitude(center - playerPos) > SqrMergeDistanceRequirement[index];

        protected bool CheckLeafForSplit(int index, Vector3 center) =>
            Vector3.SqrMagnitude(center - playerPos) < SqrSplitDistanceRequirement[index];

    }

}
