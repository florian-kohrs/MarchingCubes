using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace MarchingCubes
{

    public class MarchingCubeChunkThreaded : MarchingCubeChunk
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

        public override void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, MarchingCubeChunkNeighbourLODs neighbourLODs, Action OnDone = null)
        {
            HasStarted = true;
            this.OnDone = OnDone;
            chunkHandler.StartWaitForParralelChunkDoneCoroutine(WaitForParallelDone());
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(tris, points, neighbourLODs, OnChunkDone));
        }

        protected bool isInOtherThread;

        protected void OnChunkDone()
        {
            multiThreadDone = true;
        }

        private bool multiThreadDone = false;

        protected void RequestChunk(TriangleBuilder[] tris, float[] points, MarchingCubeChunkNeighbourLODs neighbourLODs, Action OnChunkDone)
        {
            try
            {
                isInOtherThread = true;
                BuildChunkFromMeshData(tris, points, neighbourLODs);
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
                data.Add(new MeshData(meshTriangles, vertices, colorData, !isBorderConnector, isBorderConnector));
            }
            else
            {
                base.SetCurrentMeshData(isBorderConnector);
            }
        }

        protected override void ResetAll()
        {
            if (isInOtherThread)
            {
                data.Clear();
            }
            else
            {
                base.ResetAll();
            }
        }

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
            BaseMeshDisplayer displayer = GetMeshInteractableDisplayer(this);
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, Material, d.useCollider);
        }


    }
}