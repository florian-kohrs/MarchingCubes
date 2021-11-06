using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace MarchingCubes
{

    public class CompressedMarchingCubeChunkThreaded : CompressedMarchingCubeChunk, IThreadedMarchingCubeChunk
    {


        protected Queue<IThreadedMarchingCubeChunk> readyChunks;

        public override void InitializeWithMeshDataParallel(TriangleChunkHeap heap, Queue<IThreadedMarchingCubeChunk> readyChunks)
        {
            HasStarted = true;
            this.readyChunks = readyChunks;
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(heap, OnChunkDone));
        }

        public bool IsInOtherThread { get; set; }

        protected void OnChunkDone()
        {
            lock (MarchingCubeChunkThreaded.listLock)
            {
                readyChunks.Enqueue(this);
            }
        }

        protected void RequestChunk(TriangleChunkHeap heap, Action OnChunkDone)
        {
            try
            {
                IsInOtherThread = true;
                InitializeWithMeshData(heap);
                OnChunkDone();
            }
            catch(Exception x)
            {
                MarchingCubeChunkThreaded.xs.Add(x);
                //TODO: Tell chunk handler it didnt work, or will stay stuck
                Console.WriteLine(x);
                //Debug.LogException(x);
            }
        }

        protected override void SetCurrentMeshData(bool isBorderConnector)
        {
            if (IsInOtherThread)
            {
                data.Add(new MeshData(meshTriangles, vertices, colorData, false, isBorderConnector));
            }
            else
            {
                base.SetCurrentMeshData(isBorderConnector);
            }
        }

        //protected override void ResetAll()
        //{
        //    if (isInOtherThread)
        //    {
        //        data.Clear();
        //    }
        //    else
        //    {
        //        base.ResetAll();
        //    }
        //}


        protected List<MeshData> data = new List<MeshData>();

        public void BuildAllMeshes()
        {
            for (int i = 0; i < data.Count; ++i)
            {
                ApplyChangesToMesh(data[i]);
            }
        }

        protected void ApplyChangesToMesh(in MeshData d)
        {
            BaseMeshDisplayer displayer = GetMeshDisplayer();
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, Material, d.useCollider);
        }


    }
}