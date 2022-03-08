using System.Collections;
using System.Collections.Generic;
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

        public void Loop()
        {
            while(threadHandler.HasTasks)
            {
                if(threadHandler.TryGetTask(out currentTask))
                {
                    currentTask.FindNeighbours();
                    threadHandler.OnTaskDone(currentTask);
                }
            }
        }


    }
}