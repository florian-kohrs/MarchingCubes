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

        public bool TryGetEntitiyAt(int x, int y, int z, out MarchingCubeEntity e)
        {
            e = GetEntityAt(x, y, z);
            return e != null;
        }

        public void RemoveEntityAt(MarchingCubeEntity e)
        {
            RemoveEntityAt(e.origin.x, e.origin.y, e.origin.z, e);
        }

        public void RemoveEntityAt(int x, int  y, int z, MarchingCubeEntity e)
        {
            SetEntityAt(x, y, z, null);
            entities.Remove(e);
        }

        public void SetEntityAt(Vector3Int v3, MarchingCubeEntity e)
        {
            SetEntityAt(v3.x, v3.y, v3.z, e);
        }

        public void AddEntityAt(Vector3Int  v, MarchingCubeEntity e)
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
                    Vector3Int pos = TransformBorderPointToChunk(outsidePos, offset, chunk);
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
            if (TryGetEntitiyAt(x, y, z, out e) && !e.FindMissingNeighbours(IsCubeInBounds, trisWithNeighboursOutOfBounds))
            {
                int count = trisWithNeighboursOutOfBounds.Count;
                for (int i = 0; i < count; ++i)
                {
                    MissingNeighbourData t = trisWithNeighboursOutOfBounds[i];
                    Vector3Int target = AnchorPos + e.origin + t.outsideNeighbour.offset;
                    AddNeighbourFromEntity(t.outsideNeighbour.offset);
                    if (careAboutNeighbourLODS) {
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
            SoftResetMeshDisplayers();
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
                        e = MarchAt(x,y,z, this);
                        if(e!= null)
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
            e = GetEntityAt(x,y,z);
            if (e == null)
            {
                e = new MarchingCubeEntity(this, triangulationIndex);
                e.origin = new Vector3Int(x,y,z);
                AddEntityAt(x,y,z, e);
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
            for (int i = 0; i< count; ++i) 
            {
                t = ts[i];
                origin = t.Origin;
                x = origin.x;
                y = origin.y;
                z = origin.z;
                if (!TryGetEntitiyAt(x,y,z, out cube))
                {
                    cube = CreateAndAddEntityAt(x, y, z, t.TriIndex);
                    if (careAboutNeighbourLODS && IsBorderCube(x,y,z))
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
            else if(x == entitiesPerAxis)
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
            //    AddTriangleToMeshData(t.tri,t.GetColor(), ref usedTriCount, ref totalTreeCount, false);
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


        public void RebuildAround(MarchingCubeEntity e, Vector3Int origin)
        {
            //ResetAll();
            //BuildAll();
            //BuildChunkEdges();
            //BuildMeshFromCurrentTriangles();
            if (e != null)
            {
                triCount -= e.triangles.Length * 3;
                RemoveEntityAt(e);
            }

            bool isBorder = IsBorderCube(e.origin);
            //List<MarchingCubeEntity> buildNeighbours = new List<MarchingCubeEntity>();

            for (int x = origin.x - 1; x <= origin.x + 1; x++)
            {
                for (int y = origin.y - 1; y <= origin.y + 1; y++)
                {
                    for (int z = origin.z - 1; z <= origin.z + 1; z++)
                    {
                        if (!isBorder || IsCubeInBounds(x, y, z))
                        {
                            MarchingCubeEntity cube;
                            if (TryGetEntitiyAt(x, y, z, out cube))
                            {
                                triCount -= cube.triangles.Length * 3;
                                RemoveEntityAt(x, y, z, cube);
                            }
                            cube = MarchAt(x, y, z, this);
                            if(cube != null)
                            {
                                AddEntityAt(x, y, z, cube);
                            }
                        }
                    }
                }
            }
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

        protected bool IsPointTouchingBorderInDirection(Vector3 p, Vector3Int borderDir)
        {
            p -= AnchorPos;
            return (borderDir.x < 0 && p.x == 0
                || borderDir.x > 0 && p.x == vertexSize
                || borderDir.y < 0 && p.y == 0
                || borderDir.y > 0 && p.y == vertexSize
                || borderDir.z < 0 && p.z == 0
                || borderDir.z > 0 && p.z == vertexSize);
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
            float[] points = Points;
            MarchingCubeEntity e = GetEntityFromRayHit(hit);
            int triIndex = e.GetTriangleIndexWithNormalOrClosest(hit.normal, hit.point) * 3;
            int startPointIndex = PointIndexFromCoord(e.origin);
            float[] cornerIndices = new float[8];
            for (int i = 0; i < cornerIndices.Length; ++i)
            {
                cornerIndices[i] = 0.4f;
            }
            for (int i = 0; i < 3; ++i)
            {
                int cornerA = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[e.triangulationIndex][triIndex + i]];
                int cornerB = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[e.triangulationIndex][triIndex + i]];
                cornerIndices[cornerA] += 0.2f;
                cornerIndices[cornerB] += 0.2f;
            }

            for (int i = 0; i < cornerIndices.Length; ++i)
            {
                points[startPointIndex + LocalCornerIndexToGlobalDelta(i)] += /*cornerIndices[i] **/ delta;
            }

            //foreach (int i in cornerIndices)
            //{
            //    points[i] += delta;
            //}

            //for (int i = 0; i < points.Length; ++i)
            //{
            //    points[i] += delta;
            //}

            if (IsBorderCube(e.origin))
            {
                chunkHandler.EditNeighbourChunksAt(AnchorPos, e.origin, delta);
            }

            RebuildAround(e, e.origin);
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


        public void EditPointsNextToChunk(IMarchingCubeChunk chunk, Vector3Int entityOrigin, Vector3Int offset, float delta)
        {
            int[] cornerIndices = GetCubeCornerIndicesForPoint(entityOrigin);
            int length = cornerIndices.Length;
            int index;
            for (int i = 0; i < length; ++i)
            {
                index = cornerIndices[i];
                Vector3Int indexPoint = CoordFromPointIndex(index);
                Vector3Int pointOffset = new Vector3Int();
                for (int x = 0; x < 3; x++)
                {
                    if (offset[x] == 0)
                    {
                        pointOffset[x] = 0;
                    }
                    else
                    {
                        int indexOffset = Mathf.CeilToInt((indexPoint[x] / (vertexSize - 2f)) - 1);
                        pointOffset[x] = -indexOffset;
                    }
                }

                if (pointOffset == offset)
                {
                    points[index] += delta;
                }
            }
            RebuildAround(GetEntityAt(entityOrigin), entityOrigin);
        }

      
    }
}