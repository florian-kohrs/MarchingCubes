using System;
using System.Collections;
using System.Collections.Generic;
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



        public MarchingCubeEntity GetEntityInNeighbourAt(Vector3Int outsidePos, Vector3Int offset)
        {
            IMarchingCubeChunk chunk;
            if (chunkHandler.TryGetReadyChunkAt(AnchorPos + outsidePos, out chunk))
            {
                if (chunk is IMarchingCubeInteractableChunk c)
                {
                    Vector3Int pos = TransformBorderCubePointToChunk(outsidePos, offset, chunk);
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
            TriangleBuilder t;
            Vector3Int origin;
            int x, y, z;
            int count = ts.Length;
            for (int i = 0; i < count; ++i)
            {
                t = ts[i];
                origin = t.Origin;
                x = origin.x;
                y = origin.y;
                z = origin.z;
                if (!TryGetEntityAt(x, y, z, out cube))
                {
                    cube = CreateAndAddEntityAt(x, y, z, t.TriIndex);
                    if (careAboutNeighbourLODS && IsBorderCube(x, y, z))
                    {
                        CheckForConnectedChunk(x, y, z);
                    }
                    SetNeighbourAt(x, y, z);
                }
                PathTriangle pathTri = new PathTriangle(cube, t.tri, t.steepnessAndColorData);
                cube.AddTriangle(pathTri);
                if (buildMeshAswell)
                {
                    AddTriangleToMeshData(pathTri, t.GetColor(), ref usedTriCount, ref totalTreeCount);
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


            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

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


            TimeSpan spam = watch.Elapsed;
            UnityEngine.Debug.Log(spam.TotalMilliseconds + "ms; " + spam.Ticks + " ticks");
        }

        public void Rebuild()
        {
            ResetAll();
            BuildAll();
        }

        public void RebuildAround(List<Vector3Int> changedPoints)
        {
            if(cubeEntities == null)
            {
                cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            }

            HashSet<Vector3Int> set = new HashSet<Vector3Int>();

            int count = changedPoints.Count;
            for (int i = 0; i < count; i++)
            {
                set.UnionWith(changedPoints[i].GetAllSurroundingFields());
            }

            int x, y, z;

            IEnumerator<Vector3Int> enu = set.GetEnumerator();
            while (enu.MoveNext())
            {
                Vector3Int c = enu.Current;
                x = c.x;
                y = c.y;
                z = c.z;

                if (IsCubeInBounds(x, y, z))
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
            }
            RebuildMesh();
        }


        public void RebuildMesh()
        {
            SoftResetMeshDisplayers();
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
            return x == 0 || x % (entitiesPerAxis) == 0
                || y == 0 || y % (entitiesPerAxis) == 0
                || z == 0 || z % (entitiesPerAxis) == 0;
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

        public void EditPointsAroundRayHit(float delta, RaycastHit hit, int editDistance)
        {
            float sign = Mathf.Sign(delta);
            float signedSurface = surfaceLevel * sign;
            float[] points = Points;
            MarchingCubeEntity e = GetEntityFromRayHit(hit);
            PathTriangle tri = e.GetTriangleWithNormalOrClosest(hit.normal, hit.point);
            Vector3Int origin = e.origin;
            Vector3Int globalOrigin = origin + AnchorPos;

            Dictionary<Vector3Int, Tuple<IMarchingCubeInteractableChunk, List<Vector3Int>>> editedNeighbourChunks
                = new Dictionary<Vector3Int, Tuple<IMarchingCubeInteractableChunk, List<Vector3Int>>>();

            List<Vector3Int> selfEditedPoints = new List<Vector3Int>();

            HashSet<Vector3Int> editedPoints = new HashSet<Vector3Int>();

            HashSet<PathTriangle> tris = new HashSet<PathTriangle>() { tri };
            HashSet<PathTriangle> next = new HashSet<PathTriangle>();

            for (int xx = -editDistance; xx < editDistance; xx++)
            {
                for (int yy = -editDistance; yy < editDistance; yy++)
                {
                    for (int zz = -editDistance; zz < editDistance; zz++)
                    {
                        int x = origin.x + xx;
                        int y = origin.y + yy;
                        int z = origin.z + zz;
                        float distance = ((new Vector3(xx,yy,zz) + globalOrigin) - hit.point).magnitude;

                        if (distance > editDistance)
                            continue;

                        float factor = 1 - (distance / editDistance);
                        float diff = factor * delta;
                        float value = int.MinValue;
                        if (IsPointInBounds(x, y, z))
                        {
                            int index = PointIndexFromCoord(x, y, z);
                            value = Points[index];
                            ///if the value is already air stop checking this point (maybe multiply surface level with sign of delta (and swap se to ge))
                            if (value < signedSurface)
                                continue;

                            value += diff;
                            Points[index] = value;
                            selfEditedPoints.Add(new Vector3Int(x, y, z));
                        }

                        Vector3Int[] neighbourDirs = NeighbourDirections(x, y, z);
                        int length = neighbourDirs.Length;
                        for (int i = 0; i < length; i++)
                        {
                            Vector3Int dir = neighbourDirs[i];
                            List<Vector3Int> l;
                            Tuple<IMarchingCubeInteractableChunk, List<Vector3Int>> tuple;
                            if (!editedNeighbourChunks.TryGetValue(dir, out tuple))
                            {
                                IMarchingCubeChunk chunk;
                                Vector3Int newChunkPos = AnchorPos + ChunkSize * dir;
                                if (ChunkHandler.TryGetOrCreateChunkAt(newChunkPos, out chunk) && chunk is IMarchingCubeInteractableChunk changeableChunk)
                                {
                                    l = new List<Vector3Int>();
                                    tuple = Tuple.Create(changeableChunk, l);
                                    editedNeighbourChunks[dir] = tuple;
                                }
                            }
                            if (tuple != null)
                            {
                                IMarchingCubeInteractableChunk chunk = tuple.Item1;
                                l = tuple.Item2;
                                Vector3Int pos = TransformBorderNoisePointToChunk(new Vector3Int(x, y, z), dir, chunk);
                                if (chunk.IsPointInBounds(pos))
                                {
                                    int index = chunk.PointIndexFromCoord(pos);

                                    value = chunk.Points[index];

                                    if (value < signedSurface)
                                        continue;

                                    value += diff;
                                    Points[index] = value;

                                    l.Add(pos);
                                }
                            }
                        }
                    }
                }
            }


            /////bfs with editDistance interations
            //for (int i = 0; i <= editDistance; i++)
            //{
            //    float distanceScaling = 1 - (i / Mathf.Max(1f, editDistance));
            //    IEnumerator<PathTriangle> enu = tris.GetEnumerator();
            //    PathTriangle t;
            //    ///iterate over current pathtriangles
            //    while (enu.MoveNext())
            //    {
            //        t = enu.Current;
            //        ///iterate over all neighbours of triangle
            //        foreach (var item in t.Neighbours)
            //        {
            //            if (next.Add(item))
            //            {
            //                int[] corners = item.CornerIndices;
            //                ///iterate over all corner indices of triangle
            //                for (int c = 0; c < 3; c++)
            //                {
            //                    Vector3Int v3 = origin + LocalCornerIndexToOffset(corners[c]);
            //                    ///only work on point if it wasnt worked on before
            //                    if (!editedPoints.Contains(v3))
            //                    {
            //                        float diff = delta * distanceScaling;
            //                        editedPoints.Add(v3);
            //                        ///if points is in chunk edit surface value
            //                        if (IsPointInBounds(v3))
            //                        {
            //                            selfEditedPoints.Add(v3);
            //                            points[PointIndexFromCoord(v3)] -= diff;
            //                        }
            //                        ///iterate over all directions where chunks share this noise point
            //                        Vector3Int[] neighbourDirs = NeighbourDirections(v3);
            //                        int length = neighbourDirs.Length;
            //                        for (int z = 0; z < length; z++)
            //                        {
            //                            Vector3Int dir = neighbourDirs[z];
            //                            IMarchingCubeChunk chunk;
            //                            List<Vector3Int> l;
            //                            Tuple<IMarchingCubeInteractableChunk, List<Vector3Int>> tuple;
            //                            if (!editedNeighbourChunks.TryGetValue(dir, out tuple))
            //                            {
            //                                Vector3Int newChunkPos = AnchorPos + ChunkSize * dir;
            //                                if (ChunkHandler.TryGetReadyChunkAt(newChunkPos, out chunk) && chunk is IMarchingCubeInteractableChunk changeableChunk)
            //                                {

            //                                    l = new List<Vector3Int>();
            //                                    tuple = Tuple.Create(changeableChunk, l);
            //                                    editedNeighbourChunks[newChunkPos] = tuple;
            //                                }
            //                            }
            //                            if (tuple != null)
            //                            {
            //                                chunk = tuple.Item1;
            //                                l = tuple.Item2;
            //                                Vector3Int pos = TransformBorderNoisePointToChunk(v3, dir, chunk);
            //                                l.Add(pos);
            //                                chunk.Points[PointIndexFromCoord(pos)] -= diff;
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //    ///prepare next iteration
            //    tris = next;
            //    next.Clear();
            //}

            ///if another chunk is affected call chunkhandler
            if (editedNeighbourChunks.Count > 0)
            {
                foreach (var item in editedNeighbourChunks)
                {
                    item.Value.Item1.RebuildAround(item.Value.Item2);
                }
            }

            RebuildAround(selfEditedPoints);
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