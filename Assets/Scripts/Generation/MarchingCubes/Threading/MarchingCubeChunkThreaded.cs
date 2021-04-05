using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace MarchingCubes
{

    public class MarchingCubeChunkThreaded : MarchingCubeChunk
    {


        private void Update()
        {
            if (multiThreadDone)
            {
                BuildAllMeshes();
                enabled = false;
                IsReady = true;
                OnDone();
            }
        }

        protected Action OnDone;

        public override void InitializeWithMeshDataParallel(TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel, Action OnDone)
        {
            HasStarted = true;
            chunkHandler = handler;
            this.OnDone = OnDone;
            children.Add(new BaseMeshChild(GetComponent<MeshFilter>(), GetComponent<MeshRenderer>(), GetComponent<MeshCollider>(), new Mesh()));
         
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(tris, handler, activeTris, points, surfaceLevel, OnChunkDone));
            
        }

        protected void OnChunkDone()
        {
            //BuildAllMissingEdges();
            multiThreadDone = true;
        }

        private bool multiThreadDone = false;

        protected void RequestChunk(TriangleBuilder[] tris, IMarchingCubeChunkHandler handler, int activeTris, float[] points, float surfaceLevel, Action OnChunkDone)
        {
            BuildMeshData(tris, activeTris, points, handler, surfaceLevel);
            OnChunkDone();
        }

        protected override void SetCurrentMeshData()
        {
           data.Add(new MeshData(meshTriangles, vertices, colorData));
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
            BaseMeshChild displayer = GetNextMeshDisplayer();
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, Material);
        }

        //protected void BuildAllMissingEdges()
        //{
        //    foreach (var kv in chunk.missingNeighbours)
        //    {
        //        for (int i = 0; i < kv.Value.Count; i++)
        //        {
        //            MissingNeighbourData t = kv.Value[i];
        //            Vector3Int target = ChunkOffset + t.neighbour.offset;
        //            IMarchingCubeChunk c;
        //            if (chunkHandler.TryGetReadyChunkAt(target, out c))
        //            {
        //                Vector3Int pos = (kv.Key.origin + t.neighbour.offset).Map(ClampInChunk);
        //                MarchingCubeEntity cube = c.GetEntityAt(pos);
        //                kv.Key.BuildSpecificNeighbourInNeighbour(cube, kv.Key.triangles[t.neighbour.triangleIndex], t.neighbour.relevantVertexIndices, t.neighbour.rotatedEdgePair);
        //            }
        //        }
        //    }       
        //}

        private struct ChunkThreadInfo
        {

            public Action<ChunkThreadInfo> t;
            public MarchingCubeThreadedChunk param;

            public ChunkThreadInfo(Action<ChunkThreadInfo> t, MarchingCubeThreadedChunk param)
            {
                this.t = t;
                this.param = param;
            }
        }

    }
}