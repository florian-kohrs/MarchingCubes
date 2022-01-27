using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MarchingCubes
{

    //TODO: Dont use as Interactablechunk when destruction begun
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MarchingCubeChunk : CompressedMarchingCubeChunk, 
        IMarchingCubeInteractableChunk, IHasInteractableMarchingCubeChunk, ICubeNeighbourFinder, IStoreableMarchingCube
    {

        public const float REBUILD_SHADER_THREAD_GROUP_SIZE = 4;

        public const int MAX_NOISE_VALUE  = 100;

        public IMarchingCubeInteractableChunk GetChunk => this;

        public MarchingCubeEntity[,,] cubeEntities;

        public HashSet<MarchingCubeEntity> entities = new HashSet<MarchingCubeEntity>();

        public int kernelId;
        public ComputeShader rebuildShader;
        public ComputeBuffer rebuildNoiseBuffer;
        public ComputeBuffer rebuildTriResult;
        public ComputeBuffer rebuildTriCounter;

        public Maybe<Bounds> meshBounds = new Maybe<Bounds>();



        //TODO: Add for each cube entitiy index in mesh and increase next index by entitiy cube count and try to use this when rebuilding mesh
        //TODO: Build from consume buffer

        public override bool UseCollider => true;

        protected override void BuildDetailEnvironment()
        {
            BuildGrassOnChunk();
        }

        protected void BuildGrassOnChunk()
        {
            SetBoundsOfChunk();
            chunkHandler.ComputeGrassFor(meshBounds, triangleHeap);
        }

        protected void StoreChunkState()
        {
            chunkHandler.Store(AnchorPos, this);
        }

        public MarchingCubeEntity GetEntityAt(Vector3Int v3)
        {
            return GetEntityAt(v3.x, v3.y, v3.z);
        }
        public MarchingCubeEntity GetEntityAt(int x, int y, int z)
        {
            return cubeEntities[x, y, z];
        }

        public bool TryGetEntityAt(int x, int y, int z, out MarchingCubeEntity e)
        {
            e = GetEntityAt(x, y, z);
            return e != null;
        }

        public bool HasEntityAt(Vector3Int v)
        {
            return GetEntityAt(v.x, v.y, v.z) != null;
        }

        public bool HasEntityAt(int x, int y, int z)
        {
            return GetEntityAt(x, y, z) != null;
        }

        public void RemoveEntityAt(MarchingCubeEntity e)
        {
            RemoveEntityAt(e.origin.x, e.origin.y, e.origin.z, e);
        }

        public void RemoveEntityAt(int x, int y, int z, MarchingCubeEntity e)
        {
            SetEntityAt(x, y, z, null);
            entities.Remove(e);
        }

        public void SetEntityAt(Vector3Int v3, MarchingCubeEntity e)
        {
            SetEntityAt(v3.x, v3.y, v3.z, e);
        }

        public void AddEntityAt(Vector3Int v, MarchingCubeEntity e)
        {
            AddEntityAt(v.x, v.y, v.z, e);
        }

        public void AddEntityAt(int x, int y, int z, MarchingCubeEntity e)
        {
            entities.Add(e);
            cubeEntities[x, y, z] = e;
        }

        public void SetEntityAt(int x, int y, int z, MarchingCubeEntity e)
        {
            cubeEntities[x, y, z] = e;
        }

        public MarchingCubeEntity GetEntityInNeighbourAt(Vector3Int outsidePos)
        {
            IMarchingCubeChunk chunk;
            if (chunkHandler.TryGetReadyChunkAt(AnchorPos + outsidePos, out chunk))
            {
                if (chunk is IMarchingCubeInteractableChunk c)
                {
                    Vector3Int pos = outsidePos + AnchorPos - chunk.AnchorPos;
                    return c.GetEntityAt(pos);
                }
            }
            return null;
        }


        protected virtual void ResetAll()
        {
            FreeAllMeshes();
            cubeEntities = null;
            NumTris = 0;
        }

        public override void PrepareDestruction()
        {
            base.PrepareDestruction();
            meshBounds = default;
        }

        protected MarchingCubeEntity CreateAndAddEntityAt(int x, int y, int z, int triangulationIndex)
        {
            MarchingCubeEntity e = new MarchingCubeEntity(this, triangulationIndex);
            e.origin = new Vector3Int(x, y, z);
            AddEntityAt(x, y, z, e);
            return e;
        }


        protected override void RebuildFromTriangleArray(TriangleChunkHeap heap)
        {
            trisLeft = TriCount;
            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            TriangleBuilder[] ts = heap.tris;
            MarchingCubeEntity cube;
            cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            int x, y, z;
            int count = heap.triCount;
            int endIndex = heap.startIndex + heap.triCount;
            for (int i = heap.startIndex; i < endIndex; ++i)
            {
                x = ts[i].x;
                y = ts[i].y;
                z = ts[i].z;
                if (!TryGetEntityAt(x, y, z, out cube))
                {
                    cube = CreateAndAddEntityAt(x, y, z, ts[i].triIndex);
                    SetNeighbourAt(x, y, z);
                }
                PathTriangle pathTri = new PathTriangle(cube, in ts[i].tri, ts[i].color32);
                cube.AddTriangle(pathTri);
                AddTriangleToMeshData(in ts[i], ref usedTriCount, ref totalTreeCount);
            }
        }

        protected void SetBoundsOfChunk()
        {
            meshBounds.Value = activeDisplayers[0].mesh.bounds;
            for (int i = 1; i < activeDisplayers.Count; i++)
            {
                meshBounds.Value.Encapsulate(activeDisplayers[i].mesh.bounds);
            }
        }

        protected void AddFromTriangleArray(TriangleBuilder[] ts, Vector3 origin, float distance)
        {
            int count = ts.Length;
            int x, y, z;
            MarchingCubeEntity cube;

            for (int i = 0; i < count; ++i)
            {
                x = ts[i].x;
                y = ts[i].y;
                z = ts[i].z;
                if (!TryGetEntityAt(x, y, z, out cube))
                {
                    cube = CreateAndAddEntityAt(x, y, z, ts[i].triIndex);
                    SetNeighbourAt(x, y, z);
                }
                //else
                //{
                //    Debug.Log($"didint remove {x} {y} {z} with distance {(new Vector3(x,y,z)-origin).magnitude} with allowed distance {distance}." +
                //        $"triangle has {TriangulationTable.triangulationSizes[ts[i].triIndex]}");
                //}
                PathTriangle pathTri = new PathTriangle(cube, in ts[i].tri, ts[i].color32);
                cube.AddTriangle(pathTri);
            }
        }

        protected void BuildMeshFromCurrentTriangles()
        {
            if (IsEmpty)
                return;

            trisLeft = TriCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            MarchingCubeEntity e;
            int count;
            IEnumerator<MarchingCubeEntity> enumerator = entities.GetEnumerator();
            while (enumerator.MoveNext())
            {
                e = enumerator.Current;
                count = e.triangles.Length;
                for (int i = 0; i < count; ++i)
                {
                    AddTriangleToMeshData(e.triangles[i], ref usedTriCount, ref totalTreeCount);
                }
            }

        }

        protected static object rebuildListLock = new object();


        protected void GetNoiseEditData(Vector3 offset, int radius, Vector3Int clickedIndex, out Vector3Int start, out Vector3Int end)
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
            GetNoiseEditData(offset, radius, VectorExtension.ToVector3Int(clickedIndex - offset), out start, out end);

            bool rebuildChunk;

            if (HasPoints)
            {
                rebuildChunk = EditPointsOnCPU(start, end, clickedIndex + offset, radius, delta);
            }
            else
            {
                rebuildChunk = true;
                ChunkHandler.SetEditedNoiseAtPosition(this, clickedIndex + offset, start,end,delta,radius);
            }

            if (rebuildChunk)
            {
                if (cubeEntities == null)
                {
                    cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
                }
                //System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                //w.Start();
                StoreChunkState();
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

            Vector3Int threadsPerAxis = new Vector3Int(
               Mathf.CeilToInt((1 + (endVec.x - startVec.x)) / REBUILD_SHADER_THREAD_GROUP_SIZE),
               Mathf.CeilToInt((1 + (endVec.y - startVec.y)) / REBUILD_SHADER_THREAD_GROUP_SIZE),
               Mathf.CeilToInt((1 + (endVec.z - startVec.z)) / REBUILD_SHADER_THREAD_GROUP_SIZE)
               );

            rebuildShader.SetVector("editPoint", new Vector4(clickedIndex.x, clickedIndex.y, clickedIndex.z,0));
            rebuildShader.SetVector("start", startVec);
            rebuildShader.SetVector("end", endVec);
            rebuildShader.SetVector("anchor", new Vector4(AnchorPos.x, AnchorPos.y, AnchorPos.z, 0));
            rebuildShader.SetInt("numPointsPerAxis", pointsPerAxis);
            rebuildShader.SetFloat("spacing", 1);
            rebuildNoiseBuffer.SetData(Points);
            rebuildShader.SetFloat("sqrRebuildRadius", marchSquare);
            rebuildTriResult.SetCounterValue(0);

            rebuildShader.Dispatch(0, threadsPerAxis.x, threadsPerAxis.y, threadsPerAxis.z);

            int startX = (int)startVec.x;
            int startY = (int)startVec.y;
            int startZ = (int)startVec.z;

            int editPointY = clickedIndex.y;
            int editPointZ = clickedIndex.z;

            int endX = (int)endVec.x;
            int endY = (int)endVec.y;
            int endZ = (int)endVec.z;

            float distanceX = startX - clickedIndex.x;
            float xx = distanceX * distanceX;
            for (int x = startX; x <= endX; x++)
            {
                float distanceY = startY - editPointY;
                float yy = distanceY * distanceY;
                for (int y = startY; y <= endY; y++)
                {
                    float distanceZ = startZ - editPointZ;
                    float zz = distanceZ * distanceZ;
                    for (int z = startZ; z <= endZ; z++)
                    {
                        float sqrDis = xx + yy + zz;
                        if (sqrDis <= marchSquare)
                        {
                            MarchingCubeEntity cube;
                            if (TryGetEntityAt(x, y, z, out cube))
                            {
                                NumTris -= cube.triangles.Length;
                                RemoveEntityAt(x, y, z, cube);
                            }
                        }
                        distanceZ++;
                        zz = distanceZ * distanceZ;
                    }
                    distanceY++;
                    yy = distanceY * distanceY;
                }
                distanceX++;
                xx = distanceX * distanceX;
            }

            TriangleBuilder[] ts;
            NumTris += ChunkHandler.ReadCurrentTriangleData(out ts);

            AddFromTriangleArray(ts, clickedIndex, marchDistance);

            if (!IsEmpty)
            {
                SetSimpleCollider();
            }

            RebuildMesh();
        }

        protected void RebuildFromNoiseAround(Vector3Int start, Vector3Int end, Vector3Int clickedIndex, float radius)
        {
            float marchDistance = Vector3.one.magnitude + radius + 1;
            float marchSquare = marchDistance * marchDistance;
            int voxelMinus = chunkSize - 1;

            int startX = Mathf.Max(0, start.x - 1);
            int startY = Mathf.Max(0, start.y - 1);
            int startZ = Mathf.Max(0, start.z - 1);

            int posY = clickedIndex.y;
            int posZ = clickedIndex.z;

            int endX = Mathf.Min(voxelMinus, end.x + 1);
            int endY = Mathf.Min(voxelMinus, end.y + 1);
            int endZ = Mathf.Min(voxelMinus, end.z + 1);

            float distanceX = start.x - clickedIndex.x;

            for (int x = startX; x <= endX; x++)
            {
                int distanceY = startY - posY;
                float xx = distanceX * distanceX;
                for (int y = startY; y <= endY; y++)
                {
                    int distanceZ = startZ - posZ;
                    int yy = distanceY * distanceY;
                    for (int z = startZ; z <= endZ; z++)
                    {
                        int zz = distanceZ * distanceZ;
                        float sqrDis = xx + yy + zz;
                        if (sqrDis <= marchSquare)
                        {
                            MarchingCubeEntity cube;
                            if (TryGetEntityAt(x, y, z, out cube))
                            {
                                NumTris -= cube.triangles.Length;
                                RemoveEntityAt(x, y, z, cube);
                            }
                            cube = MarchAt(x, y, z, this);
                            if (cube != null)
                            {
                                AddEntityAt(x, y, z, cube);
                            }
                        }

                        distanceZ++;
                    }
                    distanceY++;
                }
                distanceX++;
            }

            if (!IsEmpty)
            {
                SetSimpleCollider();
            }

            RebuildMesh();
        }

        public void RebuildMesh()
        {
            if (!IsInOtherThread)
            {
                FreeAllMeshes();
            }
            BuildMeshFromCurrentTriangles();
        }


        public PathTriangle GetTriangleFromRayHit(RaycastHit hit)
        {
            MarchingCubeEntity cube = GetClosestEntity(hit.point);
            return cube.GetTriangleWithNormalOrClosest(hit.normal, hit.point);
        }

        protected int LocalCornerIndexToGlobalDelta(int local)
        {
            if (local == 0)
                return 0;
            else if (local == 1)
                return 1;
            else if (local == 2)
                return 1 + sqrPointsPerAxis;
            else if (local == 3)
                return sqrPointsPerAxis;
            else if (local == 4)
                return pointsPerAxis;
            else if (local == 5)
                return pointsPerAxis + 1;
            else if (local == 6)
                return pointsPerAxis + sqrPointsPerAxis + 1;
            else if (local == 7)
                return pointsPerAxis + sqrPointsPerAxis;
            else
                throw new Exception("Invalid value");
        }

        protected Vector3Int LocalCornerIndexToOffset(int local)
        {
            Vector3Int r = new Vector3Int();

            if (local == 1)
            {
                r.x = 1;
            }
            else if (local == 2)
            {
                r.x = 1;
                r.z = 1;
            }
            else if (local == 3)
            {
                r.z = 1;
            }
            else if (local == 4)
            {
                r.y = 1;
            }
            else if (local == 5)
            {
                r.x = 1;
                r.y = 1;
            }
            else if (local == 6)
            {
                r.x = 1;
                r.y = 1;
                r.z = 1;
            }
            else if (local == 7)
            {
                r.y = 1;
                r.z = 1;
            }

            return r;
        }

        protected int FlipIndexInDirection(int pointIndex, Vector3Int dir)
        {
            if (dir.x > 0)
            {
                pointIndex -= vertexSize;
            }
            else if (dir.x < 0)
            {
                pointIndex += vertexSize;
            }
            if (dir.y > 0)
            {
                pointIndex -= pointsPerAxis * vertexSize;
            }
            else if (dir.y < 0)
            {
                pointIndex += pointsPerAxis * vertexSize;
            }
            if (dir.y > 0)
            {
                pointIndex -= sqrPointsPerAxis * vertexSize;
            }
            else if (dir.y < 0)
            {
                pointIndex += sqrPointsPerAxis * vertexSize;
            }
            return pointIndex;
        }

        protected bool SmallerThanSurface(float f) => f < surfaceLevel;
        protected bool LargerThanSurface(float f) => f >= surfaceLevel;

        public void EditPointsAroundRayHit(float delta, RaycastHit hit, int editDistance)
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            MarchingCubeEntity e = GetEntityFromRayHit(hit);
            Vector3Int origin = e.origin;
            int originX = origin.x;
            int originY = origin.y;
            int originZ = origin.z;

            Vector3 globalOrigin = origin + AnchorPos;
            Vector3 hitDiff = hit.point - globalOrigin;

            Vector3Int[] neighbourDirs = NeighbourDirections(originX, originY, originZ, editDistance + 1);

            int length = neighbourDirs.Length;

            List<Tuple<IMarchingCubeInteractableChunk, Vector3Int>> chunks = new List<Tuple<IMarchingCubeInteractableChunk, Vector3Int>>();
            chunks.Add(Tuple.Create<IMarchingCubeInteractableChunk, Vector3Int>(this, origin));

            IMarchingCubeChunk chunk;
            for (int i = 0; i < length; i++)
            {
                Vector3Int offset = ChunkSize * neighbourDirs[i];
                Vector3Int newChunkPos = AnchorPos + offset;
                //TODO: Get empty chunk first, only request actual noise when noise values change
                //!TODO: When requesting a nonexisting chunk instead of create -> edit request modified noise and only build that
                if (ChunkHandler.TryGetReadyChunkAt(newChunkPos, out chunk))
                {
                    if (chunk is IMarchingCubeInteractableChunk threadedChunk)
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
                    GetNoiseEditData(offset, editDistance, origin - offset, out start, out end);
                    chunkHandler.CreateChunkWithNoiseEdit(newChunkPos, hit.point - newChunkPos, start,end, delta, editDistance, out IMarchingCubeChunk _);
                }
            }

            int count = chunks.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3Int v3 = chunks[i].Item2;
                IMarchingCubeInteractableChunk currentChunk = chunks[i].Item1;
                currentChunk.RebuildAround(hitDiff, editDistance, v3, delta);
            }

            watch.Stop();
            Debug.Log($"Time to rebuild {count} chunks: {watch.Elapsed.TotalMilliseconds} ms.");
        }


        public MarchingCubeEntity GetClosestEntity(Vector3 v3)
        {
            Vector3 rest = v3 - AnchorPos;
            rest /= lod;
            return GetEntityAt((int)rest.x, (int)rest.y, (int)rest.z);
        }

        public MarchingCubeEntity GetEntityFromRayHit(RaycastHit hit)
        {
            return GetClosestEntity(hit.point);
        }

        public Vector3 NormalFromRay(RaycastHit hit)
        {
            return GetTriangleFromRayHit(hit).Normal;
        }

        public void StoreChunk(StoredChunkEdits storage)
        {
            storage.noise = Points;
            storage.originalCubePositions = GetCurrentCubePositions();
        }

        protected Vector3Int[] GetCurrentCubePositions()
        {
            int length = entities.Count;
            Vector3Int[] result = new Vector3Int[length];
            int index = 0;
            IEnumerator<MarchingCubeEntity> enumerator = entities.GetEnumerator();
            while (enumerator.MoveNext())
            {
                result[index] = enumerator.Current.origin;
                index++;
            }
            return result;
        }

    }
}
