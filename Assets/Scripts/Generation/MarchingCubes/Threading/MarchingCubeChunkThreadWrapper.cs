using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

namespace MarchingCubes
{

    public class MarchingCubeChunkThreadWrapper : MonoBehaviour, IMarchingCubeInteractableChunk, IMarchingCubeChunk, IHasMarchingCubeChunk
    {


        //protected IEnumerator WaitForData()
        //{
        //    yield return null;
        //    if(IsReady)
        //    {
        //        chunk.wrapper = this;
        //        BuildAllMeshes();
        //        BuildAllMissingEdges();
        //        StopAllCoroutines();
        //        OnDone();
        //    }
        //}

        private void Update()
        {
            if (multiThreadDone)
            {
                BuildAllMeshes();
                chunk.wrapper = this;
                enabled = false;
                IsReady = true;
                OnDone();
            }
        }

        public bool IsReady { get; private set; }

        protected Action OnDone;

        public void InitializeWithMeshDataParallel(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel, Action OnDone)
        {
            HasStarted = true;
            this.mat = mat;
            this.chunkHandler = handler;
            this.OnDone = OnDone;
            children.Add(new BaseMeshChild(GetComponent<MeshFilter>(), GetComponent<MeshRenderer>(), GetComponent<MeshCollider>(), new Mesh()));
            //StartCoroutine(WaitForData());
            //Action<object> f = (s) =>
            //{
            //    RequestChunk(tris, activeTris, points, surfaceLevel, OnChunkDone);
            //};

            //ThreadStart threadStart = delegate
            //{
            //    RequestChunk(tris, activeTris, points, surfaceLevel, OnChunkDone);
            //};
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(tris, activeTris, points, surfaceLevel, OnChunkDone));
            //new Thread(threadStart).Start();
        }


        public void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, float surfaceLevel)
        {
            throw new NotImplementedException();
        }


        protected void OnChunkDone(MarchingCubeThreadedChunk chunk)
        {
            BuildAllMissingEdges();
            multiThreadDone = true;
        }

        private bool multiThreadDone = false;

        protected void RequestChunk(TriangleBuilder[] tris, int activeTris, float[] points, float surfaceLevel, Action<MarchingCubeThreadedChunk> OnChunkDone)
        {
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
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, mat);
        }

        protected int ClampInChunk(int i)
        {
            return i.FloorMod(MarchingCubeChunkHandler.ChunkSize);
        }

        protected void BuildAllMissingEdges()
        {
            foreach (var kv in chunk.missingNeighbours)
            {
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    MissingNeighbourData t = kv.Value[i];
                    Vector3Int target = ChunkOffset + t.neighbour.offset;
                    IMarchingCubeChunk c;
                    if (chunkHandler.TryGetReadyChunkAt(target, out c))
                    {
                        Vector3Int pos = (kv.Key.origin + t.neighbour.offset).Map(ClampInChunk);
                        MarchingCubeEntity cube = c.GetEntityAt(pos);
                        kv.Key.BuildSpecificNeighbourInNeighbour(cube, kv.Key.triangles[t.neighbour.triangleIndex], t.neighbour.relevantVertexIndices, t.neighbour.rotatedEdgePair);
                    }
                }
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

        public void EditPointsNextToChunk(IMarchingCubeChunk chunk, MarchingCubeEntity e, Vector3Int offset, float delta)
        {
            this.chunk.EditPointsNextToChunk(chunk, e, offset, delta);
        }

        public MarchingCubeEntity GetEntityAt(Vector3Int v3)
        {
            return chunk.GetEntityAt(v3.x, v3.y, v3.z);
        }

        public MarchingCubeEntity GetEntityAt(int x, int y, int z)
        {
            return chunk.GetEntityAt(x, y, z);
        }

        public void SetActive(bool b)
        {
            gameObject.SetActive(b);
        }

        public MarchingCubeThreadedChunk chunk = new MarchingCubeThreadedChunk();

        protected Material mat;

        public IMarchingCubeChunkHandler chunkHandler;

        public Vector3Int ChunkOffset { get => chunk.ChunkOffset; set => chunk.ChunkOffset = value; }

        public IEnumerable<Vector3Int> NeighbourIndices => chunk.NeighbourIndices;

        public int NeighbourCount => chunk.NeighbourCount;

        public bool IsEmpty => chunk.IsEmpty;

        public bool IsCompletlyAir => chunk.IsCompletlyAir;

        public bool IsCompletlySolid => chunk.IsCompletlySolid;

        public IMarchingCubeInteractableChunk GetChunk => this;

        public bool HasStarted { get; private set; }

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