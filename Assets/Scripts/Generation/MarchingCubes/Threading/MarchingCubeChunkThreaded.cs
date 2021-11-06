using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MarchingCubes
{

    public class MarchingCubeChunkThreaded : MarchingCubeChunk, IThreadedMarchingCubeChunk
    {

  
        protected Queue<IThreadedMarchingCubeChunk> readyChunks;


        protected Action<IThreadedMarchingCubeChunk> OnChunkFinished;

        public override void InitializeWithMeshDataParallel(TriangleChunkHeap heap, Queue<IThreadedMarchingCubeChunk> readyChunks)
        {
            this.readyChunks = readyChunks;
            StartParallel(heap);
        }

        public override void InitializeWithMeshDataParallel(TriangleChunkHeap heap, Action<IThreadedMarchingCubeChunk> OnChunkFinished)
        {
            this.OnChunkFinished = OnChunkFinished;
            StartParallel(heap);
        }

        protected void StartParallel(TriangleChunkHeap heap)
        {
            HasStarted = true;
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(heap));
        }

        public static object listLock = new object();

        protected void OnChunkDone()
        {
            if (readyChunks != null)
            {
                lock (MarchingCubeChunkThreaded.listLock)
                {
                    readyChunks.Enqueue(this);
                }
            }
            OnChunkFinished?.Invoke(this);
        }

        public static List<Exception> xs = new List<Exception>();

        protected void RequestChunk(TriangleChunkHeap heap)
        {
            try
            {
                IsInOtherThread = true;
                InitializeWithMeshData(heap);
                OnChunkDone();
            }
            catch(Exception x)
            {
                xs.Add(x);
                Console.WriteLine(x);
                //Debug.LogException(x);
            }
        }

        protected override void SetCurrentMeshData(bool isBorderConnector)
        {
            if (IsInOtherThread)
            {
                data.Add(new MeshData(meshTriangles, vertices, colorData, !isBorderConnector, isBorderConnector));
            }
            else
            {
                base.SetCurrentMeshData(isBorderConnector);
            }
        }

        protected override void ResetAll()
        {
            if (IsInOtherThread)
            {
                data.Clear();
            }
            else
            {
                base.ResetAll();
            }
        }

        public void RebuildAroundParallel(float offsetX, float offsetY, float offsetZ, int radius, int posX, int posY, int posZ, float delta, Queue<MarchingCubeChunkThreaded> readyChunks)
        {
            IsInOtherThread = true;

            RequestPointsIfNotStored();
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    RebuildAround(offsetX, offsetY, offsetZ, radius, posX, posY, posZ, delta);
                }
                catch(Exception x)
                {
                    Debug.LogError(x);
                    //TODO: Reduce expected chunk finishes
                }
                lock (rebuildListLock)
                {
                    readyChunks.Enqueue(this);
                }
            });
        }

        protected List<MeshData> data = new List<MeshData>();

        public void BuildAllMeshes()
        {
            for (int i = 0; i < data.Count; ++i)
            {
                ApplyChangesToMesh(data[i]);
            }
            data.Clear();
        }

        protected void ApplyChangesToMesh(MeshData d)
        {
            BaseMeshDisplayer displayer = GetMeshInteractableDisplayer(this);
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, Material, d.useCollider);
        }


    }
}