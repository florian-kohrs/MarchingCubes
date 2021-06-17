using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace MarchingCubes
{

    public class CompressedMarchingCubeChunkThreaded : CompressedMarchingCubeChunk
    {


        protected IEnumerator WaitForParallelDone()
        {
            while (!multiThreadDone)
            {
                yield return null;
            }
            isInOtherThread = false;
            BuildAllMeshes();
            IsReady = true;
            OnDone?.Invoke();
        }
         
        protected Action OnDone;

        public override void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel, Action OnDone = null)
        {
            HasStarted = true;
            chunkHandler = handler;
            this.OnDone = OnDone;
            chunkHandler.StartWaitForParralelChunkDoneCoroutine(WaitForParallelDone());
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(tris, handler, points, surfaceLevel, neighbourLODs, OnChunkDone));
        }
        protected bool isInOtherThread;

        protected void OnChunkDone()
        {
            multiThreadDone = true;
        }

        private bool multiThreadDone = false;

        protected void RequestChunk(TriangleBuilder[] tris, IMarchingCubeChunkHandler handler, float[] points, float surfaceLevel, MarchingCubeChunkNeighbourLODs neighbourLODs, Action OnChunkDone)
        {
            
            try
            {
                isInOtherThread = true;
                BuildChunkFromMeshData(tris, points, handler, neighbourLODs, surfaceLevel);
                OnChunkDone();
            }
            catch(Exception x)
            {
                Debug.LogException(x);
            }
        }

        protected override void SetCurrentMeshData(bool isBorderConnector)
        {
            if (isInOtherThread)
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

        protected void BuildAllMeshes()
        {
            for (int i = 0; i < data.Count; i++)
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