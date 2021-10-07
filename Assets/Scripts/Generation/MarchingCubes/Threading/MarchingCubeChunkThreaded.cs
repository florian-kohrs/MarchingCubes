using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace MarchingCubes
{

    public class MarchingCubeChunkThreaded : MarchingCubeChunk, IThreadedMarchingCubeChunk
    {

        protected Queue<IThreadedMarchingCubeChunk> readyChunks;

        public override void InitializeWithMeshDataParallel(TriangleBuilder[] tris, Queue<IThreadedMarchingCubeChunk> readyChunks, bool keepPoints = false)
        {
            HasStarted = true;
            this.readyChunks = readyChunks;
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(tris, keepPoints));
        }

        public bool IsInOtherThread { get; set; }

        public static object listLock = new object();

        protected void OnChunkDone()
        {
            lock (listLock)
            {
                readyChunks.Enqueue(this);
            }
        }

        protected void RequestChunk(TriangleBuilder[] tris, bool keepPoints)
        {
            try
            {
                IsInOtherThread = true;
                InitializeWithMeshData(tris, keepPoints);
                OnChunkDone();
            }
            catch(Exception x)
            {
                Debug.LogException(x);
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
            BaseMeshDisplayer displayer = GetMeshInteractableDisplayer(this);
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, Material, d.useCollider);
        }


    }
}