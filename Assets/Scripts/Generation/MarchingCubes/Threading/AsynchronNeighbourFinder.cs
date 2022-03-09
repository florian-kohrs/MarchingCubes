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

        protected List<ChunkNeighbourThread> threads = new List<ChunkNeighbourThread>();

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

        public bool InitializationDone =>
            !HasWaitingTasks &&
            !HasActiveTasks && 
            handler.NoWorkOnMainThread;

        public void MaxOutRunningThreads()
        {
            int startThreadsAmount = System.Environment.ProcessorCount * 2;
            for (int i = 0; i < startThreadsAmount; i++)
            {
                StartThread();
            }
        }

        protected ChunkNeighbourThread GetNewThread()
        {
            ChunkNeighbourThread newNeighbourThread = new ChunkNeighbourThread(this);
            threads.Add(newNeighbourThread);
            return newNeighbourThread;
        }

        protected void StartThread()
        {
            ChunkNeighbourThread thread = GetNewThread();
            ThreadPool.QueueUserWorkItem((o) => thread.WaitForTasksAndExecute());
        }

        public void OnTaskDone(ChunkNeighbourTask task)
        {
            activeTasks--;
            activeTask.Remove(task);
            handler.AddFinishedTask(task);
        }

        public void AddTask(ChunkNeighbourTask task)
        {
        
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    activeTask.Add(task);
                    activeTasks++; task.FindNeighbours(); OnTaskDone(task);
                }catch(Exception x)
                {

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