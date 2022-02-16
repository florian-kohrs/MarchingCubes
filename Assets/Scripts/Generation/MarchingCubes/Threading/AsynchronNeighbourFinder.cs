using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class AsynchronNeighbourFinder
    {

        protected static object locker;

        protected static Queue<ChunkNeighourTask> tasks;

        //protected List<WorkGroups> workGroups;

        /// <summary>
        /// this is an estimate value since it could be wrong due to raceconditions as
        /// this value will be used in other threads
        /// </summary>
        public int EstimatedTaskRemaining => tasks.Count;

        public static void AddTask(ChunkNeighourTask task)
        {
            lock(locker)
                tasks.Enqueue(task);
        }


        protected static bool TryGetTask(out ChunkNeighourTask task)
        {
            task = null;
            lock (locker)
            {
                if(tasks.Count > 0)
                    task = tasks.Dequeue();
            }
            return task != null;
        }

    }
}