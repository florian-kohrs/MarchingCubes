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

        public override void InitializeWithMeshDataParallel(TriangleBuilder[] tris, Queue<IThreadedMarchingCubeChunk> readyChunks, bool keepPoints = false)
        {
            HasStarted = true;
            this.readyChunks = readyChunks;
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(tris, keepPoints));
        }

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
                lock (reabuildListLock)
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