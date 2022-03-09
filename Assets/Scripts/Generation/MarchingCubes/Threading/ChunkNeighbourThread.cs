using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkNeighbourThread
    {

        public ChunkNeighbourThread(AsynchronNeighbourFinder threadHandler)
        {
            this.threadHandler = threadHandler;
        }

        protected AsynchronNeighbourFinder threadHandler;

        protected ChunkNeighbourTask currentTask;

        public void WaitForTasksAndExecute()
        {
            try
            {
                while (!threadHandler.InitializationDone)
                {
                    if (threadHandler.TryGetTask(out currentTask))
                    {
                        currentTask.FindNeighbours();
                        threadHandler.OnTaskDone(currentTask);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                currentTask = null;
            }
            catch (Exception x)
            {

            }
        }


    }
}