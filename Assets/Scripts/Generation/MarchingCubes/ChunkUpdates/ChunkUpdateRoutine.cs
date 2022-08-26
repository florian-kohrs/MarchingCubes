using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkUpdateRoutine
    {

        /// <summary>
        /// check each array entry representing all nodes of the same groupsize for any node to merge its children becoming a leaf itself 
        /// </summary>
        public static HashSet<ChunkGroupTreeNode>[] chunkGroupNodes = new HashSet<ChunkGroupTreeNode>[MarchingCubeChunkHandler.MAX_CHUNK_LOD_POWER];

        /// <summary>
        /// check this hashset for leafs to split 
        /// </summary>
        public static HashSet<ChunkGroupTreeLeaf> chunkGroupTreeLeaves = new HashSet<ChunkGroupTreeLeaf>();
        
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

        protected HashSet<ChunkGroupRoot> destroySet;
        protected HashSet<ChunkGroupRoot> deactivateSet;
        protected HashSet<ChunkGroupTreeNode> mergeSet;
        protected HashSet<ChunkGroupTreeLeaf> splitSet;

        protected ChunkUpdateValues updateValues;

        public bool update;

        protected int ChunkCheckIntervalInMs => updateValues.ChunkCheckIntervalInMs;
        protected float SqrDeactivateDistance => updateValues.SqrDeactivateDistance;
        protected float SqrDestroyDistance => updateValues.SqrDestroyDistance;

        public float[] SqrMergeDistanceRequirement => updateValues.sqrMergeDistanceRequirement;

        public float[] SqrSplitDistanceRequirement => updateValues.sqrSplitDistanceRequirement;


        static ChunkUpdateRoutine()
        {
            for (int i = 0; i < SQR_DISTANCE_LOD_UPDATES.Length; i++)
            {
                SQR_DISTANCE_LOD_UPDATES[i] *= SQR_DISTANCE_LOD_UPDATES[i];
            }
        }

        public ChunkUpdateRoutine(HashSet<ChunkGroupRoot> destroySet, HashSet<ChunkGroupRoot> deactivateSet, HashSet<ChunkGroupTreeNode> mergeSet, HashSet<ChunkGroupTreeLeaf> splitSet, ChunkUpdateValues updateValues)
        {
            this.destroySet = destroySet;
            this.deactivateSet = deactivateSet;
            this.mergeSet = mergeSet;
            this.splitSet = splitSet;
            PrepareChunkNodeRegister();
        }

        public void BeginAsynchrounLodCheck()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                RunRoutine();
            });
        }

        protected void RunRoutine()
        {
            if(update)
            {
                update = false;
                UpdateAll();
            }
            Thread.Sleep(ChunkCheckIntervalInMs);
            RunRoutine();
        }

        protected void UpdateAll()
        {
            CheckRootsForDestruction();
            CheckRootsForDeactivation();
            CheckAllLodsForMerge();
            CheckAllLeafsForSplit();
        }


        protected void PrepareChunkNodeRegister()
        {
            for (int i = 0; i < MarchingCubeChunkHandler.MAX_CHUNK_LOD_POWER; i++)
            {
                chunkGroupNodes[i] = new HashSet<ChunkGroupTreeNode>();
            }
        }

        protected void CheckRootsForDestruction()
        {
            foreach (var item in chunkGroupTreeRoots)
            {
                if (CheckChunkForDestruction(item.Center))
                {
                    item.channeledForDestruction = true;
                    destroySet.Add(item);
                }
            }
        }

        protected void CheckRootsForDeactivation()
        {
            foreach (var item in chunkGroupTreeRoots)
            {
                if (CheckChunkForDeactivation(item.Center) && !item.channeledForDestruction)
                {
                    destroySet.Add(item);
                    item.channeledForDeactivation = true;
                }
            }
        }

        protected void CheckAllLodsForMerge(bool force = false)
        {
            CheckAllLodsForMerge(0, force);
        }

        protected void CheckAllLodsForMerge(int index, bool force = false)
        {
            if (index >= SQR_DISTANCE_LOD_UPDATES.Length || (!force && !CheckLod(index)))
                    return;

            ///only check next lod if current lod is going to be updated
            CheckAllLodsForMerge(index + 1, force);
            CheckLodRoutine(index);
        }

        protected void CheckAllLeafsForSplit()
        {
            ///when splitting a leaf check in new node hirachy recursively for recursive splits
        }

        protected void CheckLodRoutine(int index)
        {
            lastTimeCheckedPositions[index] = playerPos;
            HashSet<ChunkGroupTreeNode> currentSet = chunkGroupNodes[index];
            foreach (ChunkGroupTreeNode chunk in currentSet)
            {
                CheckNodeGroupForMerge(index, chunk);
            }
        }

        protected void CheckNodeGroupForMerge(int index, ChunkGroupTreeNode node)
        {
            if (!CheckChunkForMerge(index, node.Center))
                return;


        }

        protected bool CheckLod(int index) => 
            Vector3.SqrMagnitude(lastTimeCheckedPositions[index] - playerPos) >= SQR_DISTANCE_LOD_UPDATES[index];

        protected bool CheckChunkForDestruction(Vector3 center) =>
          Vector3.SqrMagnitude(center - playerPos) >= SqrDestroyDistance;

        protected bool CheckChunkForDeactivation(Vector3 center) =>
            Vector3.SqrMagnitude(center - playerPos) >= SqrDeactivateDistance;


        protected bool CheckChunkForMerge(int index, Vector3 center) =>
            Vector3.SqrMagnitude(center - playerPos) >= SqrMergeDistanceRequirement[index];

        protected bool CheckChunkForSplit(int index, Vector3 center) =>
            Vector3.SqrMagnitude(center - playerPos) >= SqrSplitDistanceRequirement[index];

    }

}
