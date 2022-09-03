using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ReducedMarchingCubesChunk : CompressedMarchingCubeChunk,
        IHasInteractableMarchingCubeChunk
    {


        public const int MAX_NOISE_VALUE = 100;


        protected static object rebuildListLock = new object();

        public ReducedMarchingCubesChunk GetChunk => this;

        public StorageTreeLeaf storageLeaf;

        protected float[] points;

        public bool HasPoints => points != null;

        public float[] Points
        {
            get
            {
                if (points == null)
                {
                    points = chunkHandler.RequestNoiseForChunk(this);
                }
                return points;
            }
            set
            {
                points = value;
            }
        }

        protected override void OnChunkReset()
        {
            points = null;
        }


        public override bool UseCollider => true;

        protected void StoreChunkState()
        {
            chunkHandler.Store(AnchorPos, this);
        }

        protected virtual void ResetAll()
        {
            FreeAllMeshes();
            NumTris = 0;
        }


        protected void GetNoiseEditData(int radius, Vector3Int clickedIndex, out Vector3Int start, out Vector3Int end)
        {
            int ppMinus = pointsPerAxis - 1;

            start = new Vector3Int(
                Mathf.Max(0, clickedIndex.x - radius),
                Mathf.Max(0, clickedIndex.y - radius),
                Mathf.Max(0, clickedIndex.z - radius));

            end = new Vector3Int(
                Mathf.Min(ppMinus, clickedIndex.x + radius + 1),
                Mathf.Min(ppMinus, clickedIndex.y + radius + 1),
                Mathf.Min(ppMinus, clickedIndex.z + radius + 1));
        }

        //TODO: When editing chunk that spawns new chunk build neighbours of new chunk if existing
        public void RebuildAround(Vector3 offset, int radius, Vector3Int clickedIndex, float delta)
        {
            ///define loop ranges
            Vector3Int start;
            Vector3Int end;
            GetNoiseEditData(radius, VectorExtension.ToVector3Int(clickedIndex - offset), out start, out end);

            bool rebuildChunk;
            if (!HasPoints)
            {
                ChunkHandler.RequestNoiseForChunk(this);
            }
            rebuildChunk = EditPointsOnCPU(start, end, clickedIndex + offset, radius, delta);

            //else
            //{
            //    rebuildChunk = true;
            //    ChunkHandler.SetEditedNoiseAtPosition(this, clickedIndex + offset, start,end,delta,radius);
            //}

            if (rebuildChunk)
            {
                //System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                //w.Start();
                StoreChunkState();
                storageLeaf.RemoveMipMapInHirachy();
                RebuildFromNoiseAroundOnGPU(start, end, clickedIndex, radius);
                //RebuildFromNoiseAround(start, end, clickedIndex, radius);
                //w.Stop();
                //Debug.Log("Time for rebuild only: " + w.Elapsed.TotalMilliseconds);
            }
        }

        protected bool EditPointsOnCPU(Vector3Int start, Vector3Int end, Vector3 clickPosition, float editDistance, float delta)
        {
            bool result = false;

            int startX = start.x;
            int startY = start.y;
            int startZ = start.z;

            int endX = end.x;
            int endY = end.y;
            int endZ = end.z;

            float clickPosX = clickPosition.x;
            float clickPosY = clickPosition.y;
            float clickPosZ = clickPosition.z;

            float sqrEdit = editDistance * editDistance;

            float distanceX = startX - clickPosX;

            for (int x = startX; x <= endX; x++)
            {
                float distanceY = startY - clickPosY;
                for (int y = startY; y <= endY; y++)
                {
                    float distanceZ = startZ - clickPosZ;
                    for (int z = startZ; z <= endZ; z++)
                    {
                        float sqrDistance = distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ;

                        if (sqrDistance < sqrEdit)
                        {
                            float dis = Mathf.Sqrt(sqrDistance);
                            float factor = 1 - (dis / editDistance);
                            float diff = factor * delta;
                            int index = PointIndexFromCoord(x, y, z);
                            float point = Points[index];
                            float value = point;

                            if (factor > 0 && ((value != -MAX_NOISE_VALUE || diff >= 0)
                                && (value != MAX_NOISE_VALUE || diff < 0)))
                            {
                                result = true;
                                value += diff;
                                value = Mathf.Clamp(value, -MAX_NOISE_VALUE, MAX_NOISE_VALUE);
                                point = value;
                                points[index] = point;
                            }
                        }
                        distanceZ++;
                    }
                    distanceY++;
                }
                distanceX++;
            }
            return result;
        }


        protected void RebuildFromNoiseAroundOnGPU(Vector3Int start, Vector3Int end, Vector3Int clickedIndex, float radius)
        {
            float marchDistance = Vector3.one.magnitude + radius + 1;
            float marchSquare = marchDistance * marchDistance;
            int voxelMinus = chunkSize - 1;

            Vector3 startVec = new Vector3(
                Mathf.Max(0, start.x - 1),
                Mathf.Max(0, start.y - 1),
                Mathf.Max(0, start.z - 1));

            Vector3 endVec = new Vector3(
                Mathf.Min(voxelMinus, end.x + 1),
                Mathf.Min(voxelMinus, end.y + 1),
                Mathf.Min(voxelMinus, end.z + 1));

            //TODO: Remove to array from native array
           
            MeshData result = ChunkHandler.DispatchRebuildAround(this, clickedIndex, startVec, endVec, marchSquare);

            FreeAllMeshes();
            InitializeWithMeshData(result);
        }

        protected Vector3Int GlobalPosToCubeIndex(Vector3 pos)
        {
            pos = GlobalToLocalPosition(pos);
            return new Vector3Int((int)pos.x, (int)pos.y, (int)pos.z);
        }

        public Vector3 GlobalToLocalPosition(Vector3 v3) => v3 - AnchorPos;

        public void EditPointsAroundRayHit(float delta, RaycastHit hit, int editDistance)
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            Vector3Int origin = GlobalPosToCubeIndex(hit.point);
            //var e = GetEntityFromRayHit(hit);
            int originX = origin.x;
            int originY = origin.y;
            int originZ = origin.z;

            Vector3 globalOrigin = origin + AnchorPos;
            Vector3 hitDiff = hit.point - globalOrigin;

            Direction[] neighbourDirs = NeighbourDirections(originX, originY, originZ, editDistance + 1);

            int length = neighbourDirs.Length;

            List<Tuple<ReducedMarchingCubesChunk, Vector3Int>> chunks = new List<Tuple<ReducedMarchingCubesChunk, Vector3Int>>();
            chunks.Add(Tuple.Create(this, origin));

            CompressedMarchingCubeChunk lastChunk = this;
            CompressedMarchingCubeChunk chunk;

            Vector3Int offset = Vector3Int.zero;

            for (int i = 0; i < length; i++)
            {
                Direction dir = neighbourDirs[i];
                offset = DirectionHelper.OffsetVector3Int(dir, offset, ChunkSize);
                //TODO: Get empty chunk first, only request actual noise when noise values change
                //!TODO: When requesting a nonexisting chunk instead of create -> edit request modified noise and only build that
                if (lastChunk.Leaf.TryGetChunkInDirection(dir, out ChunkReadyState readyState) && readyState.HasLeaf(out chunk))
                {
                    if (chunk is ReducedMarchingCubesChunk threadedChunk)
                    {
                        Vector3Int v3 = origin - offset;
                        chunks.Add(Tuple.Create(threadedChunk, v3));
                    }
                    else
                    {
                        Debug.LogWarning("Editing of compressed marchingcube chunks is not supported!");
                    }
                }
                else
                {
                    Vector3Int start;
                    Vector3Int end;
                    Vector3Int newChunkPos = AnchorPos + offset;
                    GetNoiseEditData(editDistance, origin - offset, out start, out end);
                    chunkHandler.CreateChunkWithNoiseEdit(readyState, hit.point - newChunkPos, start, end, delta, editDistance, out _);
                }
            }

            int count = chunks.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3Int v3 = chunks[i].Item2;
                ReducedMarchingCubesChunk currentChunk = chunks[i].Item1;
                currentChunk.RebuildAround(hitDiff, editDistance, v3, delta);
            }

            watch.Stop();
            Debug.Log($"Time to rebuild {count} chunks: {watch.Elapsed.TotalMilliseconds} ms.");
        }


        public Vector3 NormalFromRay(RaycastHit hit)
        {
            return hit.normal;
        }

        public void StoreChunk(StoredChunkEdits storage)
        {
            storage.noise = Points;
            //storage.originalCubePositions = GetCurrentCubePositions();
        }

    }
}