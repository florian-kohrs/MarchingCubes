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
            //BuildAllMissingEdges();
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
            BaseMeshChild displayer = GetNextMeshDisplayer();
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, Material, d.useCollider);
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

    }
}