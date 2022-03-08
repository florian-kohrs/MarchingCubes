using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class AsynchronNeighbourFinder
    {

        protected static object locker;

        protected Queue<ChunkNeighbourTask> tasks;

        protected List<ChunkNeighbourThread> threads;

        public MarchingCubeChunkHandler handler;

        //protected List<WorkGroups> workGroups;

        /// <summary>
        /// this is an estimate value since it could be wrong due to raceconditions as
        /// this value will be used in other threads
        /// </summary>
        public int EstimatedTaskRemaining => tasks.Count;

        public bool HasTasks => tasks.Count > 0;    

        public void OnTaskDone(ChunkNeighbourTask task)
        {
            handler.BuildNeighbourChunks(task.HasNeighbourInDirection, task.chunk.ChunkSize, task.chunk.CenterPos);
        }

        public void AddTask(ChunkNeighbourTask task)
        {
            lock(locker)
                tasks.Enqueue(task);
        }


        public bool TryGetTask(out ChunkNeighbourTask task)
        {
            lock (locker)
            {
                if(tasks.Count > 0)
                    task = tasks.Dequeue();
                else
                    task= null;
            }
            return task != null;
        }

    }
}