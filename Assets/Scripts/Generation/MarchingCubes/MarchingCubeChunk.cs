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
    public class MarchingCubeChunk : CompressedMarchingCubeChunk, IMarchingCubeInteractableChunk, IHasInteractableMarchingCubeChunk, ICubeNeighbourFinder
    {
         

        public const int MAX_NOISE_VALUE  = 100;

        public IMarchingCubeInteractableChunk GetChunk => this;

        public MarchingCubeEntity[,,] cubeEntities;

        public HashSet<MarchingCubeEntity> entities = new HashSet<MarchingCubeEntity>();

        public int kernelId;
        public ComputeShader rebuildShader;
        public ComputeBuffer rebuildNoiseBuffer;
        public ComputeBuffer rebuildTriResult;
        public ComputeBuffer rebuildTriCounter;


        public override bool UseCollider => true;

        protected void StoreNoiseArray()
        {
            chunkHandler.Store(AnchorPos, Points);
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

        protected void AddFromTriangleArray(TriangleBuilder[] ts)
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


        //TODO: When editing chunk that spawns new chunk build neighbours of new chunk if existing
        public void RebuildAround(float offsetX, float offsetY, float offsetZ, int radius, int posX, int posY, int posZ, float delta)
        {
            if (cubeEntities == null)
            {
                cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            }

            ///at some point rather call gpu to compute this
            int ppMinus = pointsPerAxis - 1;
            float sqrEdit = radius * radius;

            int editedNoiseCount = 0;

            ///define loop ranges
            int startX = Mathf.Max(0, posX - radius);
            int startY = Mathf.Max(0, posY - radius);
            int startZ = Mathf.Max(0, posZ - radius);
            int endX = Mathf.Min(ppMinus, posX + radius + 1);
            int endY = Mathf.Min(ppMinus, posY + radius + 1);
            int endZ = Mathf.Min(ppMinus, posZ + radius + 1);

            float factorMaxDistance = radius + 0;

            float distanceX = startX - posX + offsetX;

            for (int x = startX; x <= endX; x++)
            {
                float distanceY = startY - posY + offsetY;
                for (int y = startY; y <= endY; y++)
                {
                    float distanceZ = startZ - posZ + offsetZ;
                    for (int z = startZ; z <= endZ; z++)
                    {
                        float sqrDistance = distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ;

                        if (sqrDistance <= sqrEdit)
                        {
                            float dis = Mathf.Sqrt(sqrDistance);
                            float factor = 1 - (dis / factorMaxDistance);
                            float diff = factor * delta;
                            int index = PointIndexFromCoord(x, y, z);
                            PointData point = Points[index];
                            float value = point.surfaceValue;

                            if (factor > 0 && ((value != -MAX_NOISE_VALUE || diff >= 0)
                                && (value != MAX_NOISE_VALUE || diff < 0)))
                            {
                                editedNoiseCount++;
                                value += diff;
                                value = Mathf.Clamp(value, -MAX_NOISE_VALUE, MAX_NOISE_VALUE);
                                point.surfaceValue = value;
                                points[index] = point;
                            }
                        }
                        distanceZ++;
                    }
                    distanceY++;
                }
                distanceX++;
            }

            if (editedNoiseCount > 0)
            {
                StoreNoiseArray();
                RebuildFromNoiseAround(radius, posX, posY, posZ, startX, startY,startZ, endX, endY, endZ);
            }
        }

        protected void RebuildFromNoiseAroundOnGPU(int radius, int posX, int posY, int posZ, int startX, int startY, int startZ, int endX, int endY, int endZ)
        {
            float marchDistance = Vector3.one.magnitude + radius + 1;
            int marchDistCeil = Mathf.CeilToInt(marchDistance + radius);
            float marchSquare = marchDistance * marchDistance;

            radius += 1;
            Vector3Int start = new Vector3Int(posX - radius, posY - radius, posZ - radius);
            int voxelMinus = chunkSize - 1;
            endX = Mathf.Min(voxelMinus, endX + 1);
            endY = Mathf.Min(voxelMinus, endY + 1);
            endZ = Mathf.Min(voxelMinus, endZ + 1);

            rebuildShader.SetVector("pos", new Vector4(posX, posY, posZ, 0));
            rebuildShader.SetVector("start", new Vector4(start.x,start.y,start.z,0));
            rebuildShader.SetVector("end", new Vector4(endX, endY, endZ, 0));
            rebuildShader.SetVector("anchor", new Vector4(AnchorPos.x, AnchorPos.y, AnchorPos.z, 0));
            rebuildShader.SetInt("numPointsPerAxis", pointsPerAxis);
            rebuildShader.SetInt("spacing", 1);
            rebuildNoiseBuffer.SetData(Points);
            rebuildShader.SetFloat("sqrRadius", marchSquare);
            rebuildTriResult.SetCounterValue(0);

            rebuildShader.Dispatch(0, pointsPerAxis, pointsPerAxis, pointsPerAxis);

            startX = Mathf.Max(0, startX - 4);
            startY = Mathf.Max(0, startY - 4);
            startZ = Mathf.Max(0, startZ - 4);


            //float distanceX = startX - posX;
            //for (int x = startX; x <= endX; x++)
            //{
            //    int distanceY = startY - posY;
            //    float xx = distanceX * distanceX;
            //    for (int y = startY; y <= endY; y++)
            //    {
            //        int distanceZ = startZ - posZ;
            //        int yy = distanceY * distanceY;
            //        for (int z = startZ; z <= endZ; z++)
            //        {
            //            int zz = distanceZ * distanceZ;
            //            float sqrDis = xx + yy + zz;
            //            if (sqrDis <= marchSquare)
            //            {
            //                MarchingCubeEntity cube;
            //                if (TryGetEntityAt(x, y, z, out cube))
            //                {
            //                    triCount -= cube.triangles.Length * 3;
            //                    RemoveEntityAt(x, y, z, cube);
            //                }
            //            }

            //            distanceZ++;
            //        }
            //        distanceY++;
            //    }
            //    distanceX++;
            //}
            entities = new HashSet<MarchingCubeEntity>();
            cubeEntities = new MarchingCubeEntity[chunkSize, chunkSize, chunkSize];

            TriangleBuilder[] ts;
            NumTris = ChunkHandler.ReadCurrentTriangleData(out ts);

            AddFromTriangleArray(ts);

            if (!IsEmpty)
            {
                SetSimpleCollider();
            }

            RebuildMesh();
        }

        protected void RebuildFromNoiseAround(int radius, int posX, int posY, int posZ,int startX, int startY, int startZ, int endX, int endY, int endZ)
        {
            float distanceX = startX - posX;

            float marchDistance = Vector3.one.magnitude + radius + 1;
            float marchSquare = marchDistance * marchDistance;
            int voxelMinus = chunkSize - 1;

            startX = Mathf.Max(0, startX - 1);
            startY = Mathf.Max(0, startY - 1);
            startZ = Mathf.Max(0, startZ - 1);
            endX = Mathf.Min(voxelMinus, endX + 1);
            endY = Mathf.Min(voxelMinus, endY + 1);
            endZ = Mathf.Min(voxelMinus, endZ + 1);

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

        protected void EditPointsAroundRayHitOnGPU()
        {

        }

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
            Vector3 hitDiff = globalOrigin - hit.point;
            float hitOffsetX = hitDiff.x;
            float hitOffsetY = hitDiff.y;
            float hitOffsetZ = hitDiff.z;

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
                if (ChunkHandler.TryGetOrCreateChunkAt(newChunkPos, out chunk))
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
            }

            int count = chunks.Count;
            for (int i = 0; i < count; i++)
            {
                Vector3Int v3 = chunks[i].Item2;
                chunks[i].Item1.RebuildAround(hitOffsetX, hitOffsetY, hitOffsetZ, editDistance, v3.x, v3.y, v3.z, delta);
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
    }
}