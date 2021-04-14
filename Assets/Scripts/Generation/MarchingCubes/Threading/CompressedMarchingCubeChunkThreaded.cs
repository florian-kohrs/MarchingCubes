using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace MarchingCubes
{

    public class CompressedMarchingCubeChunkThreaded : CompressedMarchingCubeChunk
    {


        private void Update()
        {
            if (multiThreadDone)
            {
                isInOtherThread = false;
                BuildAllMeshes();
                enabled = false;
                IsReady = true;
                OnDone();
            }
        }

        protected Action OnDone;

        public override void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel, Action OnDone)
        {
            HasStarted = true;
            chunkHandler = handler;
            this.OnDone = OnDone;
            children.Add(new BaseMeshChild(GetComponent<MeshFilter>(), GetComponent<MeshRenderer>(), GetComponent<MeshCollider>(), new Mesh()));
         
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
                BuildMeshData(tris, points, handler, neighbourLODs, surfaceLevel);
                OnChunkDone();
            }
            catch(Exception x)
            {

            }
        }

        protected override void SetCurrentMeshData(bool useCollider)
        {
            if (isInOtherThread)
            {
                data.Add(new MeshData(meshTriangles, vertices, colorData, useCollider));
            }
            else
            {
                base.SetCurrentMeshData(useCollider);
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
            BaseMeshChild displayer = GetNextMeshDisplayer();
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, Material, d.useCollider);
        }


    }
}