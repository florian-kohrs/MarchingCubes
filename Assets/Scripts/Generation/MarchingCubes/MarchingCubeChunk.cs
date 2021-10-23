using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MarchingCubes
{

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MarchingCubeChunk : CompressedMarchingCubeChunk, IMarchingCubeInteractableChunk, IHasInteractableMarchingCubeChunk, ICubeNeighbourFinder
    {

        protected override void WorkOnBuildedChunk()
        {
            //FindConnectedChunks();

            if (neighbourChunksGlue.Count > 0)
            {
                BuildMeshToConnectHigherLodChunks();
            }
        }

        //public override void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel)
        //{
        //    HasStarted = true;
        //    points = new float[PointsPerAxis * PointsPerAxis * PointsPerAxis];
        //    this.surfaceLevel = surfaceLevel;
        //    this.neighbourLODs = neighbourLODs;
        //    careAboutNeighbourLODS = neighbourLODs.HasNeighbourWithHigherLOD(lod);
        //    chunkHandler = handler;
        //    IsReady = true;
        //}

        public IMarchingCubeInteractableChunk GetChunk => this;

        public MarchingCubeEntity[,,] cubeEntities;

        public HashSet<MarchingCubeEntity> entities = new HashSet<MarchingCubeEntity>();

        //public Dictionary<int, MarchingCubeEntity> cubeEntities = new Dictionary<int, MarchingCubeEntity>();

        protected Dictionary<Vector3Int, MarchingCubeEntity> higherLodNeighbourCubes = new Dictionary<Vector3Int, MarchingCubeEntity>();

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




        protected NoiseFilter noiseFilter;



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


        //protected void FastFindConnectedChunks()
        //{
        //    int index = points.Length - 1;
        //    int sqrVertexSize = vertexSize * vertexSize;
        //    bool isBackTopRightCornerEarth = points[index] >= surfaceLevel;
        //    index -= vertexSize;
        //    bool isBackTopLeftCornerEarth = points[index] >= surfaceLevel;
        //    index -= sqrPointsPerAxis;
        //    index += pointsPerAxis;
        //    bool isBackBotLeftCornerEarth = points[index] >= surfaceLevel;
        //    index += vertexSize;
        //    bool isBackBotRightCornerEarth = points[index] >= surfaceLevel;

        //    index = 0;
        //    bool isFrontBotLeftCornerEarth = points[index] >= surfaceLevel;
        //    index += vertexSize;
        //    bool isFrontBotRightCornerEarth = points[index] >= surfaceLevel;
        //    index += sqrPointsPerAxis;
        //    index -= pointsPerAxis;
        //    bool isFrontTopRightCornerEarth = points[index] >= surfaceLevel;
        //    index -= vertexSize;
        //    bool isFrontTopLeftCornerEarth = points[index] >= surfaceLevel;

        //    if (neighbourLODs[0].ActiveLodPower > LODPower)
        //    {
        //        for (int y = 0; y < chunkSize; y++)
        //        {
        //            for (int z = 0; z < chunkSize; z++)
        //            {
        //                CheckForConnectedChunk(chunkSize - 1, y, z);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        HasNeighbourInDirection[0] =
        //        isBackBotRightCornerEarth != isFrontBotRightCornerEarth
        //        || isFrontBotRightCornerEarth != isFrontTopRightCornerEarth
        //        || isFrontTopRightCornerEarth != isBackTopRightCornerEarth;
        //    }

        //    if (neighbourLODs[1].ActiveLodPower > LODPower)
        //    {
        //        for (int y = 0; y < chunkSize; y++)
        //        {
        //            for (int z = 0; z < chunkSize; z++)
        //            {
        //                CheckForConnectedChunk(0, y, z);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        HasNeighbourInDirection[1] =
        //        isBackBotLeftCornerEarth != isFrontBotLeftCornerEarth
        //        || isFrontBotLeftCornerEarth != isFrontTopLeftCornerEarth
        //        || isFrontTopLeftCornerEarth != isBackTopRightCornerEarth;
        //    }

        //    if (neighbourLODs[2].ActiveLodPower > LODPower)
        //    {
        //        for (int x = 0; x < chunkSize; x++)
        //        {
        //            for (int z = 0; z < chunkSize; z++)
        //            {
        //                CheckForConnectedChunk(x, chunkSize - 1, z);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        HasNeighbourInDirection[2] =
        //        isBackTopLeftCornerEarth != isBackTopRightCornerEarth
        //        || isBackTopRightCornerEarth != isFrontTopLeftCornerEarth
        //        || isFrontTopLeftCornerEarth != isFrontTopRightCornerEarth;
        //    }

        //    if (neighbourLODs[3].ActiveLodPower > LODPower)
        //    {
        //        for (int x = 0; x < chunkSize; x++)
        //        {
        //            for (int z = 0; z < chunkSize; z++)
        //            {
        //                CheckForConnectedChunk(x, 0, z);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        HasNeighbourInDirection[3] =
        //        isBackBotLeftCornerEarth != isBackBotRightCornerEarth
        //        || isBackBotRightCornerEarth != isFrontBotLeftCornerEarth
        //        || isFrontBotLeftCornerEarth != isFrontBotRightCornerEarth;
        //    }

        //    if (neighbourLODs[4].ActiveLodPower > LODPower)
        //    {
        //        for (int x = 0; x < chunkSize; x++)
        //        {
        //            for (int y = 0; y < chunkSize; y++)
        //            {
        //                CheckForConnectedChunk(x, y, chunkSize - 1);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        HasNeighbourInDirection[4] =
        //        isBackBotLeftCornerEarth != isBackTopLeftCornerEarth
        //        || isBackTopLeftCornerEarth != isBackTopRightCornerEarth
        //        || isBackTopRightCornerEarth != isBackBotRightCornerEarth;
        //    }

        //    if (neighbourLODs[5].ActiveLodPower > LODPower)
        //    {
        //        for (int x = 0; x < chunkSize; x++)
        //        {
        //            for (int y = 0; y < chunkSize; y++)
        //            {
        //                CheckForConnectedChunk(x, y, 0);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        HasNeighbourInDirection[5] =
        //        isFrontBotLeftCornerEarth != isFrontTopLeftCornerEarth
        //        || isFrontTopLeftCornerEarth != isFrontTopRightCornerEarth
        //        || isFrontTopRightCornerEarth != isFrontBotRightCornerEarth;
        //    }

        //}

        protected void CheckForConnectedChunk(int x, int y, int z)
        {
            MarchingCubeEntity e;
            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            if (TryGetEntityAt(x, y, z, out e) && !e.FindMissingNeighbours(IsCubeInBounds, trisWithNeighboursOutOfBounds))
            {
                int count = trisWithNeighboursOutOfBounds.Count;
                for (int i = 0; i < count; ++i)
                {
                    MissingNeighbourData t = trisWithNeighboursOutOfBounds[i];
                    Vector3Int target = AnchorPos + e.origin + t.outsideNeighbour.offset;
                    AddNeighbourFromEntity(t.outsideNeighbour.offset);
                    if (careAboutNeighbourLODS)
                    {
                        IMarchingCubeChunk c;
                        //TODO: may also take non ready chunks!
                        if (chunkHandler.TryGetReadyChunkAt(target, out c))
                        {
                            if (c.LODPower > LODPower)
                            {
                                BuildMarchingCubeChunkTransitionInDirection(e.origin, t, c.LODPower);
                            }
                        }
                        else
                        {
                            int neighbourLodPower = neighbourLODs.GetLodPowerFromNeighbourInDirection(t.outsideNeighbour.offset);
                            if (neighbourLodPower > LODPower)
                            {
                                BuildMarchingCubeChunkTransitionInDirection(e.origin, t, neighbourLodPower);
                            }
                        }
                    }
                }
            }
        }


        //protected List<MissingNeighbourData> missingHigherLODNeighbour = new List<MissingNeighbourData>();

        //protected bool GetPointWithCorner(MarchingCubeEntity e, int a, int b, out Vector3 result)
        //{
        //    for (int i = 0; i < e.triangles.Count; ++i)
        //    {
        //        for (int x = 0; x < 3; x++)
        //        {
        //            Vector3 v = e.triangles[i].tri[x];
        //            int aIndex = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[e.triangulationIndex][i * 3 + x]];
        //            int bIndex = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[e.triangulationIndex][i * 3 + x]];
        //            if (aIndex == a && bIndex == b)
        //            {
        //                result = v;
        //                return true;
        //            }
        //        }
        //    }
        //    result = Vector3.zero;
        //    return false;
        //}



        protected virtual void ResetAll()
        {
            FreeAllMeshes();
            cubeEntities = null;
            triCount = 0;
        }



        protected void BuildAll(int localLod = 1)
        {
            triCount = 0;

            MarchingCubeEntity e;

            for (int x = 0; x < vertexSize / localLod; x++)
            {
                for (int y = 0; y < vertexSize / localLod; y++)
                {
                    for (int z = 0; z < vertexSize / localLod; z++)
                    {
                        //MarchingCubeEntity e = MarchAt(v, localLod);
                        //if (e.triangles.Count > 0)
                        //{
                        //    cubeEntities[IndexFromCoord(x, y, z)] = e;
                        //    triCount += e.triangles.Count * 3;
                        //}
                        e = MarchAt(x, y, z, this);
                        if (e != null)
                        {
                            AddEntityAt(x, y, z, e);
                        }
                    }
                }
            }
            // BuildMeshFromCurrentTriangles();
        }

        protected bool GetOrAddEntityAt(int x, int y, int z, int triangulationIndex, out MarchingCubeEntity e)
        {
            e = GetEntityAt(x, y, z);
            if (e == null)
            {
                e = new MarchingCubeEntity(this, triangulationIndex);
                e.origin = new Vector3Int(x, y, z);
                AddEntityAt(x, y, z, e);
                return false;
            }
            return true;
        }

        protected MarchingCubeEntity CreateAndAddEntityAt(int x, int y, int z, int triangulationIndex)
        {
            MarchingCubeEntity e = new MarchingCubeEntity(this, triangulationIndex);
            e.origin = new Vector3Int(x, y, z);
            AddEntityAt(x, y, z, e);
            return e;
        }


        protected override void BuildFromTriangleArray(TriangleBuilder[] ts, bool buildMeshAswell = true)
        {
            trisLeft = triCount;
            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            MarchingCubeEntity cube;
            cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            int x, y, z;
            int count = ts.Length;
            for (int i = 0; i < count; ++i)
            {
                x = ts[i].x;
                y = ts[i].y;
                z = ts[i].z;
                if (!TryGetEntityAt(x, y, z, out cube))
                {
                    cube = CreateAndAddEntityAt(x, y, z, ts[i].triIndex);
                    if (careAboutNeighbourLODS && IsBorderCube(x, y, z))
                    {
                        CheckForConnectedChunk(x, y, z);
                    }
                    SetNeighbourAt(x, y, z);
                }
                PathTriangle pathTri = new PathTriangle(cube, in ts[i].tri, ts[i].r, ts[i].g, ts[i].b, ts[i].steepness);
                cube.AddTriangle(pathTri);
                if (buildMeshAswell)
                {
                    AddTriangleToMeshData(in ts[i], ref usedTriCount, ref totalTreeCount);
                }
            }
        }

        private void SetNeighbourAt(int x, int y, int z)
        {
            if (x == 0)
            {
                HasNeighbourInDirection[1] = true;
            }
            else if (x == entitiesPerAxis)
            {
                HasNeighbourInDirection[0] = true;
            }

            if (y == 0)
            {
                HasNeighbourInDirection[3] = true;
            }
            else if (y == entitiesPerAxis)
            {
                HasNeighbourInDirection[2] = true;
            }

            if (z == 0)
            {
                HasNeighbourInDirection[5] = true;
            }
            else if (z == entitiesPerAxis)
            {
                HasNeighbourInDirection[4] = true;
            }
        }

        protected void BuildMeshFromCurrentTriangles()
        {
            if (IsEmpty)
                return;

            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            //int triangles = triCount / 3;
            //TriangleBuilder[] tris = chunkHandler.GenerateCubesFromNoise(this, triangles, Points);
            //TriangleBuilder t;
            //for (int i = 0; i < triangles; i++)
            //{
            //    t = tris[i];
            //    AddTriangleToMeshData(t.tri, t.GetColor(), ref usedTriCount, ref totalTreeCount, false);
            //}
            MarchingCubeEntity e;
            int count;
            IEnumerator<MarchingCubeEntity> enumerator = entities.GetEnumerator();
            while (enumerator.MoveNext())
            {
                e = enumerator.Current;
                count = e.triangles.Length;
                for (int i = 0; i < count; ++i)
                {
                    AddTriangleToMeshData(e.triangles[i], e.triangles[i].GetColor(), ref usedTriCount, ref totalTreeCount, false);
                }
            }

        }

        public void Rebuild()
        {
            ResetAll();
            BuildAll();
        }

        public bool IsInOtherThread { get; set; }

        protected static object reabuildListLock = new object();

      

        public void RebuildAround(float offsetX, float offsetY, float offsetZ, int radius, int posX, int posY, int posZ, float delta)
        {
            if (cubeEntities == null)
            {
                cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            }

            RequestPointsIfNotStored();
            ///at some point rather call gpu to compute this
            int ppMinus = pointsPerAxis - 1;
            float sqrEdit = radius * radius;


            ///define loop ranges
            int startX = Mathf.Max(0, posX - radius);
            int startY = Mathf.Max(0, posY - radius);
            int startZ = Mathf.Max(0, posZ - radius);
            int endX = Mathf.Min(ppMinus, posX + radius + 1);
            int endY = Mathf.Min(ppMinus, posY + radius + 1);
            int endZ = Mathf.Min(ppMinus, posZ + radius + 1);

            float factorMaxDistance = radius + 0;

            Func<float, bool> f;
            if (delta > 0)
                f = LargerThanSurface;
            else if (delta < 0)
                f = SmallerThanSurface;
            else
                return;

            float distanceX = startX - posX + offsetX;

            for (int x = startX; x <= endX; x++)
            {
                float distanceY = startY - posY + offsetY;
                for (int y = startY; y <= endY; y++)
                {
                    float distanceZ = startZ - posZ + offsetZ;
                    for (int z = startZ; z <= endZ; z++)
                    {
                        float sqrDistance = new Vector3(distanceX, distanceY, distanceZ).sqrMagnitude;

                        if (sqrDistance <= sqrEdit)
                        {
                            float dis = Mathf.Sqrt(sqrDistance);
                            float factor = 1 - (dis / factorMaxDistance);
                            float diff = factor * delta;
                            float value;
                            int index = PointIndexFromCoord(x, y, z);
                            value = points[index];

                            //if (f(value))
                            //    continue;

                            value += diff;
                            value = Mathf.Clamp(value, -2, 2);
                            points[index] = value;

                        }
                        distanceZ++;
                    }
                    distanceY++;
                }
                distanceX++;
            }

            distanceX = startX - posX;

            float marchDistance = Vector3.one.magnitude + radius + 1;
            int voxelMinus = chunkSize - 1;

            startX = Mathf.Max(0, startX - 1);
            startY = Mathf.Max(0, startY - 1);
            startZ = Mathf.Max(0, startZ - 5);
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
                        float dis = Mathf.Sqrt(xx + yy + zz);
                        //float sqrDistance = ((new Vector3(distanceX, distanceY, distanceZ) + globalOrigin) - point).sqrMagnitude;
                        if (dis <= marchDistance)
                        {
                            MarchingCubeEntity cube;
                            if (TryGetEntityAt(x, y, z, out cube))
                            {
                                triCount -= cube.triangles.Length * 3;
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
            RebuildMesh();
        }

        public void RebuildMesh()
        {
            if (!IsInOtherThread)
            {
                SoftResetMeshDisplayers();
            }
            BuildMeshFromCurrentTriangles();
        }

        protected Vector3Int[] GetIndicesAround(MarchingCubeEntity e)
        {
            Vector3Int v3 = e.origin;
            Vector3Int[] r = new Vector3Int[] {
                new Vector3Int(v3.x + 1, v3.y, v3.z),
                new Vector3Int(v3.x - 1, v3.y, v3.z),
                new Vector3Int(v3.x, v3.y + 1, v3.z),
                new Vector3Int(v3.x, v3.y - 1, v3.z),
                new Vector3Int(v3.x, v3.y, v3.z + 1),
                new Vector3Int(v3.x, v3.y, v3.z - 1) };
            return r;
        }

        protected List<Vector3Int> GetValidIndicesAround(MarchingCubeEntity e)
        {
            Vector3Int[] aroundVertices = GetIndicesAround(e);
            List<Vector3Int> r = new List<Vector3Int>(aroundVertices.Length);
            Vector3Int v3;
            for (int i = 0; i < aroundVertices.Length; ++i)
            {
                v3 = aroundVertices[i];
                if (IsCubeInBounds(v3))
                {
                    r.Add(v3);
                }
            }
            return r;
        }



        protected bool IsBorderCube(Vector3Int v)
        {
            return IsBorderCube(v.x, v.y, v.z);
        }


        protected bool IsBorderCube(int x, int y, int z)
        {
            return x == 0 || x == entitiesPerAxis
                || y == 0 || y == entitiesPerAxis
                || z == 0 || z == entitiesPerAxis;
        }


        //protected Direction GetBorderInfo(Vector3Int v)
        //{
        //    if (!(v.y == 0 || v.y % (vertexSize - 1) == 0
        //        || v.z == 0 || v.z % (vertexSize - 1) == 0))
        //    {
        //        if (v.x == 0)
        //            return Direction.xStart;
        //        else if (v.x % (vertexSize - 1) == 0)
        //            return Direction.xEnd;
        //    }

        //    if (!(v.x == 0 || v.x % (vertexSize - 1) == 0
        //        || v.z == 0 || v.z % (vertexSize - 1) == 0))
        //    {
        //        if (v.y == 0)
        //            return Direction.yStart;
        //        else if (v.y % (vertexSize - 1) == 0)
        //            return Direction.yEnd;
        //    }

        //    if (!(v.x == 0 || v.x % (vertexSize - 1) == 0
        //      || v.y == 0 || v.y % (vertexSize - 1) == 0))
        //    {
        //        if (v.z == 0)
        //            return Direction.yStart;
        //        else if (v.z % (vertexSize - 1) == 0)
        //            return Direction.yEnd;
        //    }
        //    return Direction.None;
        //}


        //protected enum Direction { None, xStart, xEnd, yStart, yEnd, zStart, zEnd };

        //protected Direction DirFromVector(Vector3Int dir)
        //{
        //    if (dir.x > 0)
        //        return Direction.xEnd;
        //    else if (dir.x < 0)
        //        return Direction.xStart;
        //    else if (dir.y > 0)
        //        return Direction.yEnd;
        //    else if (dir.y < 0)
        //        return Direction.yStart;
        //    else if (dir.z > 0)
        //        return Direction.zEnd;
        //    else
        //        return Direction.zStart;
        //}



        public PathTriangle GetTriangleFromRayHit(RaycastHit hit)
        {
            MarchingCubeEntity cube = GetClosestEntity(hit.point);
            return cube.GetTriangleWithNormal(hit.normal);
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
            Vector3 hitDiff = globalOrigin - hit.point;
            float hitOffsetX = hitDiff.x;
            float hitOffsetY = hitDiff.y;
            float hitOffsetZ = hitDiff.z;

            Queue<MarchingCubeChunkThreaded> readyChunks = new Queue<MarchingCubeChunkThreaded>();

            Vector3Int[] neighbourDirs = NeighbourDirections(originX, originY, originZ, editDistance + 1);

            int length = neighbourDirs.Length;

            int totalChunks = length + 1;
            int expectedFinishes = 0;

            List<Tuple<MarchingCubeChunkThreaded, Vector3Int>>[] chunks = new List<Tuple<MarchingCubeChunkThreaded, Vector3Int>>[2];
            chunks[0] = new List<Tuple<MarchingCubeChunkThreaded, Vector3Int>>();
            chunks[1] = new List<Tuple<MarchingCubeChunkThreaded, Vector3Int>>();

            chunks[points != null ? 1 : 0].Add(Tuple.Create((MarchingCubeChunkThreaded)this, origin));

            IMarchingCubeChunk chunk;
            for (int i = 0; i < length; i++)
            {
                Vector3Int offset = ChunkSize * neighbourDirs[i];
                Vector3Int newChunkPos = AnchorPos + offset;
                if (ChunkHandler.TryGetOrCreateChunkAt(newChunkPos, out chunk))
                {
                    if (chunk is MarchingCubeChunkThreaded threadedChunk)
                    {
                        Vector3Int v3 = origin - offset;
                        chunks[threadedChunk.points == null ? 1 : 0].Add(Tuple.Create(threadedChunk,v3));
                    }
                    else
                    {
                        Debug.LogWarning("Ediiting of non parallel chunks is not supported!");
                    }
                }
            }

            expectedFinishes = 0;

            int mainThreadChunks;
            if(chunks[1].Count >= 2)
            {
                mainThreadChunks = 8;
            }
            else
            {
                mainThreadChunks = 8;
            }
            

            int count = chunks[0].Count;

            for (int i = 0; i < count; i++)
            {
                Vector3Int v3 = chunks[0][i].Item2;
                if (totalChunks - i > mainThreadChunks)
                {
                    chunks[0][i].Item1.RebuildAroundParallel(hitOffsetX, hitOffsetY, hitOffsetZ, editDistance, v3.x, v3.y, v3.z, delta, readyChunks);
                    expectedFinishes++;
                }
                else
                {
                    chunks[0][i].Item1.RebuildAround(hitOffsetX, hitOffsetY, hitOffsetZ, editDistance, v3.x, v3.y, v3.z, delta);
                }
            }

            totalChunks -= count;

            count = chunks[1].Count;
            for (int i = 0; i < count; i++)
            {
                Vector3Int v3 = chunks[1][i].Item2;
                if (totalChunks - i > mainThreadChunks)
                {
                    chunks[1][i].Item1.RebuildAroundParallel(hitOffsetX, hitOffsetY, hitOffsetZ, editDistance, v3.x, v3.y, v3.z, delta, readyChunks);
                    expectedFinishes++;
                }
                else
                {
                    chunks[1][i].Item1.RebuildAround(hitOffsetX, hitOffsetY, hitOffsetZ, editDistance, v3.x, v3.y, v3.z, delta);
                }
            }

            int handlelChunks = 0;
            System.Diagnostics.Stopwatch watch2 = new System.Diagnostics.Stopwatch();
            watch2.Start();
            while (handlelChunks < expectedFinishes)
            {
                while(readyChunks.Count > 0)
                {
                    watch2.Stop();
                    MarchingCubeChunkThreaded c = readyChunks.Dequeue();
                    c.SoftResetMeshDisplayers();
                    c.BuildAllMeshes();
                    c.IsInOtherThread = false;
                    handlelChunks++;
                    watch2.Start();
                }
                if(watch.ElapsedMilliseconds > 1000)
                {
                    Debug.LogError("Watch exeeded a second. interupt!");
                    break;
                }
            }
            Debug.Log(watch2.Elapsed.TotalMilliseconds + "ms for wait of parallel chunks");


            //RebuildAround(selfEditedPoints);
            TimeSpan spam = watch.Elapsed;
            Debug.Log(spam.TotalMilliseconds + "ms for total rebuild of " + (length + 1) + " chunks");
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

    }
}