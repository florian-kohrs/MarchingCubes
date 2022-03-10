using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MarchingCubes
{
    public class AsynchronNeighbourFinder
    {
        
        protected static object locker = new object();

        public AsynchronNeighbourFinder(MarchingCubeChunkHandler handler)
        {
            this.handler = handler;
        }

        protected Queue<ChunkNeighbourTask> waitingTasks = new Queue<ChunkNeighbourTask>();

        public MarchingCubeChunkHandler handler;

        //protected List<WorkGroups> workGroups;

        /// <summary>
        /// this is an estimate value since it could be wrong due to raceconditions as
        /// this value will be used in other threads
        /// </summary>
        public int EstimatedTaskRemaining => waitingTasks.Count;

        public bool HasWaitingTasks => waitingTasks.Count > 0;

        protected int activeTasks;

        protected HashSet<ChunkNeighbourTask> activeTask = new HashSet<ChunkNeighbourTask>();
        public int ActiveTasks => activeTasks;

        public bool HasActiveTasks => activeTasks > 0;

        protected object mutex = new object();

        public bool InitializationDone =>
            !HasWaitingTasks &&
            !HasActiveTasks && 
            handler.NoWorkOnMainThread;

        public void OnTaskDone(ChunkNeighbourTask task)
        {
            lock (mutex)
            {
                activeTasks--;
                //activeTask.Remove(task);
            }
            handler.AddFinishedTask(task);
        }

        public void AddTask(ChunkNeighbourTask task)
        {
        
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    lock (mutex)
                    {
                        //activeTask.Add(task);
                        activeTasks++;
                    }
                    task.FindNeighbours(); 
                    OnTaskDone(task);
                }
                catch(Exception x)
                {
                    CompressedMarchingCubeChunk.xs.Add(x);
                }
            });
            //lock(locker)
            //    waitingTasks.Enqueue(task);
        }


        public bool TryGetTask(out ChunkNeighbourTask task)
        {
            lock (locker)
            {
                if (waitingTasks.Count > 0)
                {
                    task = waitingTasks.Dequeue();
                    activeTasks++;
                }
                else
                    task = null;
            }
            return task != null;
        }

    }
}