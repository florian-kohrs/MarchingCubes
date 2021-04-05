using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace MarchingCubes
{

    public class MarchingCubeChunkThreadWrapper : MonoBehaviour, IMarchingCubeInteractableChunk
    {

        protected IEnumerator WaitForData()
        {
            yield return new WaitForSeconds(0.075f);
            if(chunk != null)
            {

                StopAllCoroutines();
            }
        }

        public void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel)
        {
            StartCoroutine(WaitForData());
            this.mat = mat;
            this.chunkHandler = handler;
            ThreadStart threadStart = delegate
            {
                RequestChunk(tris, activeTris, points, surfaceLevel, OnChunkDone);
            };

            new Thread(threadStart).Start();
        }

        protected void OnChunkDone(MarchingCubeThreadedChunk chunk)
        {
            this.chunk = chunk;
            Debug.Log("Recieved map data :)");
        }

        protected void RequestChunk(TriangleBuilder[] tris, int activeTris, float[] points, float surfaceLevel, Action<MarchingCubeThreadedChunk> OnChunkDone)
        {
            chunk = new MarchingCubeThreadedChunk();
            chunk.BuildMeshTrigger = StoreMeshData;
            chunk.InitializeWithMeshData(tris, activeTris, points, surfaceLevel);
            OnChunkDone(chunk);
        }

        protected void StoreMeshData(MeshData d)
        {
            data.Add(d);
        }

        protected List<MeshData> data = new List<MeshData>();

        protected List<BaseMeshChild> children = new List<BaseMeshChild>();

        protected void BuildAllMeshed()
        {
            for (int i = 0; i < data.Count; i++)
            {
                ApplyChangesToMesh(data[i]);
            }
        }

        protected void ApplyChangesToMesh(in MeshData d)
        {
            BaseMeshChild displayer = GetNextMeshDisplayer();
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, mat);
        }

        protected void BuildAllMissingEdges()
        {
            foreach (var kv in chunk.missingNeighbours)
            {

            }       
        }

        public BaseMeshChild GetNextMeshDisplayer()
        {
            if (children[children.Count - 1].IsAppliedMesh)
            {
                BaseMeshChild result = new BaseMeshChild(this, transform);
                children.Add(result);
                return result;
            }
            else
            {
                return children[children.Count - 1];
            }
        }

        public PathTriangle GetTriangleFromRayHit(RaycastHit hit)
        {
            return chunk.GetTriangleFromRayHit(hit);
        }

        public MarchingCubeEntity GetClosestEntity(Vector3 v3)
        {
            return chunk.GetClosestEntity(v3);
        }

        public void EditPointsAroundRayHit(int sign, RaycastHit hit, int editDistance)
        {
            chunk.EditPointsAroundRayHit(sign, hit, editDistance);
        }

        public MarchingCubeThreadedChunk chunk;

        protected Material mat;

        public Vector3Int chunkOffset;

        public IMarchingCubeChunkHandler chunkHandler;

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