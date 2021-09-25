using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MarchingCubeChunk : CompressedMarchingCubeChunk, IMarchingCubeInteractableChunk, IHasInteractableMarchingCubeChunk
    {

        public override void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, MarchingCubeChunkNeighbourLODs neighbourLODs, Action OnDone = null)
        {
            Debug.LogWarning("This class does not support concurrency! Will use base class instead. Use " + nameof(MarchingCubeChunkThreaded) + "instead!");
            base.InitializeWithMeshDataParallel(tris, points, neighbourLODs, OnDone);
        }

        public override void InitializeWithMeshData(TriangleBuilder[] tris, float[] points, MarchingCubeChunkNeighbourLODs neighbourLODs)
        {
            BuildChunkFromMeshData(tris, points, neighbourLODs);
        }

        protected override void WorkOnBuildedChunk()
        {
            //if (LODPower <= MarchingCubeChunkHandler.DEFAULT_MIN_CHUNK_LOD_POWER)
            //{
            //    BuildChunkEdges();
            //}
            //else
            {
                FindConnectedChunks();
            }

            BuildMeshToConnectHigherLodChunks();

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

        protected List<MarchingCubeEntity> entities;

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

        public void RemoveEntityAt(Vector3Int v3)
        {
            SetEntityAt(v3.x, v3.y, v3.z, null);
        }

        public void RemoveEntityAt(int  x,int  y,int z)
        {
            SetEntityAt(x, y, z, null);
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


        protected Vector4 BuildVector4(Vector3 v3, float w)
        {
            return new Vector4(v3.x, v3.y, v3.z, w);
        }

        public virtual MarchingCubeEntity March(Vector3Int p)
        {
            MarchingCubeEntity e = new MarchingCubeEntity(this);
            e.origin = p;
            Vector4[] cubeCorners = GetCubeCornersForPoint(p);

            int cubeIndex = 0;
            if (cubeCorners[0].w < surfaceLevel) cubeIndex |= 1;
            if (cubeCorners[1].w < surfaceLevel) cubeIndex |= 2;
            if (cubeCorners[2].w < surfaceLevel) cubeIndex |= 4;
            if (cubeCorners[3].w < surfaceLevel) cubeIndex |= 8;
            if (cubeCorners[4].w < surfaceLevel) cubeIndex |= 16;
            if (cubeCorners[5].w < surfaceLevel) cubeIndex |= 32;
            if (cubeCorners[6].w < surfaceLevel) cubeIndex |= 64;
            if (cubeCorners[7].w < surfaceLevel) cubeIndex |= 128;

            e.triangulationIndex = cubeIndex;

            for (int i = 0; TriangulationTable.triangulation[cubeIndex][i] != -1; i += 3)
            {
                // Get indices of corner points A and B for each of the three edges
                // of the cube that need to be joined to form the triangle.
                int a0 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i]];
                int b0 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i]];

                int a1 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i + 1]];
                int b1 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i + 1]];

                int a2 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i + 2]];
                int b2 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i + 2]];

                Triangle tri = new Triangle();

                tri.c = InterpolateVerts(cubeCorners[a0], cubeCorners[b0]);
                tri.b = InterpolateVerts(cubeCorners[a1], cubeCorners[b1]);
                tri.a = InterpolateVerts(cubeCorners[a2], cubeCorners[b2]);
                e.triangles.Add(new PathTriangle(e, tri, GetColor));
                triCount += 3;

            }

            if (e.triangles.Count > 0)
            {
                AddEntityAt(p, e);
            }
            return e;
        }




        protected void BuildChunkEdges()
        {
            if (IsEmpty)
                return;

            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            MissingNeighbourData t;
            MarchingCubeEntity e;

            
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        if(TryGetEntitiyAt(x,y,z, out e))
                        {
                            e.BuildInternNeighbours();
                            if ((e.origin.x + e.origin.y + e.origin.z) % 2 == 0 || IsBorderCube(x,y,z))
                            {
                                if (!e.BuildNeighbours(IsBorderCube(e.origin), GetEntityAt, IsCubeInBounds, trisWithNeighboursOutOfBounds))
                                {
                                    int count = trisWithNeighboursOutOfBounds.Count;
                                    for (int i = 0; i < count; ++i)
                                    {
                                        t = trisWithNeighboursOutOfBounds[i];
                                        //Vector3Int offset = t.neighbour.offset.Map(Math.Sign);
                                        Vector3Int target = GetGlobalEstimatedNeighbourPositionFromOffset(t.outsideNeighbour.offset);
                                        Vector3Int border = t.originCubeEntity + t.outsideNeighbour.offset;
                                        IMarchingCubeChunk chunk;
                                        AddNeighbourFromEntity(t.outsideNeighbour.offset);
                                        if (chunkHandler.TryGetReadyChunkAt(AnchorPos + border, out chunk))
                                        {
                                            if (chunk is IMarchingCubeInteractableChunk c)
                                            {
                                                if (c.LODPower == LODPower)
                                                {
                                                    Vector3Int pos = TransformBorderPointToChunk(border, t.outsideNeighbour.offset, chunk);
                                                    MarchingCubeEntity cube = c.GetEntityAt(pos);
                                                    if (cube == null)
                                                    {
                                                        cube = c.GetEntityAt(pos);
                                                    }
                                                    e.BuildSpecificNeighbourInNeighbour(cube, t.outsideNeighbour.triangleIndex, t.outsideNeighbour.originalEdgePair, t.outsideNeighbour.relevantVertexIndices, t.outsideNeighbour.rotatedEdgePair);
                                                }
                                                else if (c.LODPower > LODPower)
                                                {
                                                    BuildMarchingCubeChunkTransitionInDirection(e.origin, t, c.LODPower);
                                                }
                                            }
                                        }
                                        else if (careAboutNeighbourLODS)
                                        {
                                            int neighbourLodPower = neighbourLODs.GetLodPowerFromNeighbourInDirection(t.outsideNeighbour.offset);
                                            if (neighbourLodPower > LODPower)
                                            {
                                                BuildMarchingCubeChunkTransitionInDirection(e.origin, t, neighbourLodPower);
                                            }
                                        }
                                    }
                                    trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void FindConnectedChunks()
        {
            if (IsEmpty)
                return;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if(CheckForConnectedChunk(x, y, 0) && HasNeighbourInDirection[5] && !careAboutNeighbourLODS)
                    {
                        y = chunkSize;
                        x = chunkSize;
                    }
                }
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (CheckForConnectedChunk(x, y, chunkSize - 1) && HasNeighbourInDirection[4] && !careAboutNeighbourLODS)
                    {
                        y = chunkSize;
                        x = chunkSize;
                    }
                }
            }

            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (CheckForConnectedChunk(0, y, z) && HasNeighbourInDirection[1] && !careAboutNeighbourLODS)
                    {
                        y = chunkSize;
                        z = chunkSize;
                    }
                    
                }
            }
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (CheckForConnectedChunk(chunkSize - 1, y, z) && HasNeighbourInDirection[0] && !careAboutNeighbourLODS)
                    {
                        y = chunkSize;
                        z = chunkSize;
                    }
                }
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (CheckForConnectedChunk(x, 0, z) && HasNeighbourInDirection[3] && !careAboutNeighbourLODS)
                    {
                        x = chunkSize;
                        z = chunkSize;
                    }
                }
            }

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    if (CheckForConnectedChunk(x, chunkSize - 1, z) && HasNeighbourInDirection[2] && !careAboutNeighbourLODS)
                    {
                        x = chunkSize;
                        z = chunkSize;
                    }
                }
            }
        }

        protected bool CheckForConnectedChunk(int x, int y, int z)
        {
            bool result = false;
            MarchingCubeEntity e;
            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            if (TryGetEntitiyAt(x, y, z, out e) && !e.FindMissingNeighbours(IsCubeInBounds, trisWithNeighboursOutOfBounds))
            {
                int count = trisWithNeighboursOutOfBounds.Count;
                result = true;
                for (int i = 0; i < count; ++i)
                {
                    MissingNeighbourData t = trisWithNeighboursOutOfBounds[i];
                    Vector3Int target = GetGlobalEstimatedNeighbourPositionFromOffset(t.outsideNeighbour.offset);
                    AddNeighbourFromEntity(t.outsideNeighbour.offset);
                    IMarchingCubeChunk c;
                    if (chunkHandler.TryGetReadyChunkAt(target, out c))
                    {
                        if (c.LODPower > LODPower)
                        {
                            //might need to use Transform position
                            Vector3Int pos = (e.origin + t.outsideNeighbour.offset).Map(ClampInChunk);
                            BuildMarchingCubeChunkTransitionInDirection(e.origin, t, c.LODPower);
                        }
                    }
                    else if (careAboutNeighbourLODS)
                    {
                        int neighbourLodPower = neighbourLODs.GetLodPowerFromNeighbourInDirection(t.outsideNeighbour.offset);
                        if (neighbourLodPower > LODPower)
                        {
                            BuildMarchingCubeChunkTransitionInDirection(e.origin, t, neighbourLodPower);
                        }
                    }
                }
            }
            return result;
        }

        protected void BuildEdgesFor(MarchingCubeEntity e, List<MissingNeighbourData> addMissingNeighboursHere, bool overrideEdges)
        {
            int currentCount = addMissingNeighboursHere.Count;

            e.BuildInternNeighbours();
            e.BuildNeighbours(IsBorderCube(e.origin), GetEntityAt, IsCubeInBounds, addMissingNeighboursHere, overrideEdges);
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





        protected virtual Vector3 InterpolatePositions(Vector3 v1, Vector3 v2, float p)
        {
            return v1 + p * (v2 - v1);
        }


        protected virtual void ResetAll()
        {
            SoftResetMeshDisplayers();
            cubeEntities = null;
            triCount = 0;
        }



        protected void BuildAll(int localLod = 1)
        {
            triCount = 0;
            Vector3Int v = new Vector3Int();

            for (int x = 0; x < vertexSize / localLod; x++)
            {
                v.x = x;
                for (int y = 0; y < vertexSize / localLod; y++)
                {
                    v.y = y;
                    for (int z = 0; z < vertexSize / localLod; z++)
                    {
                        v.z = z;
                        //MarchingCubeEntity e = MarchAt(v, localLod);
                        //if (e.triangles.Count > 0)
                        //{
                        //    cubeEntities[IndexFromCoord(x, y, z)] = e;
                        //    triCount += e.triangles.Count * 3;
                        //}
                        March(v);
                    }
                }
            }
            // BuildMeshFromCurrentTriangles();
        }

        protected bool GetOrAddEntityAt(Vector3Int v3, out MarchingCubeEntity e)
        {
            e = GetEntityAt(v3);
            if (e == null)
            {
                e = new MarchingCubeEntity(this);
                e.origin = v3;
                AddEntityAt(v3, e);
                return false;
            }
            return true;
        }


        protected override void BuildFromTriangleArray(TriangleBuilder[] ts, bool buildMeshAswell = true)
        {
            trisLeft = triCount;
            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            MarchingCubeEntity cube;
            cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            entities = new List<MarchingCubeEntity>();
            TriangleBuilder t;
            for(int i = 0; i< ts.Length; ++i) 
            {
                t = ts[i];
                if (!GetOrAddEntityAt(t.Origin, out cube))
                {
                    cube.triangulationIndex = t.TriIndex;
                }
                PathTriangle pathTri = new PathTriangle(cube, t.tri, t.steepnessAndColorData);
                cube.triangles.Add(pathTri);
                if (buildMeshAswell)
                {
                    AddTriangleToMeshData(pathTri, t.GetColor(), ref usedTriCount, ref totalTreeCount);
                }
            }
        }

        protected void BuildMeshFromCurrentTriangles()
        {
            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            MarchingCubeEntity e;
            int count;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        if(TryGetEntitiyAt(x,y,z,out e))
                        {
                            count = e.triangles.Count;
                            for (int i = 0; i < count; ++i)
                            {
                                AddTriangleToMeshData(e.triangles[i], e.triangles[i].GetColor(), ref usedTriCount, ref totalTreeCount, false);
                            }
                        }
                    }
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
                triCount -= e.triangles.Count * 3;
                RemoveEntityAt(e.origin);
            }
            int[] a = new int[3];

            List<MarchingCubeEntity> buildNeighbours = new List<MarchingCubeEntity>();

            for (int x = origin.x - 1; x <= origin.x + 1; x++)
            {
                a[0] = x;
                for (int y = origin.y - 1; y <= origin.y + 1; y++)
                {
                    a[1] = y;
                    for (int z = origin.z - 1; z <= origin.z + 1; z++)
                    {
                        a[2] = z;
                        if (IsCubeInBounds(a))
                        {
                            MarchingCubeEntity neighbourEntity;
                            if (TryGetEntitiyAt(a[0],a[1],a[2], out neighbourEntity))
                            {
                                triCount -= neighbourEntity.triangles.Count * 3;
                                RemoveEntityAt(a[0], a[1], a[2]);
                            }
                            MarchingCubeEntity newCube = March(new Vector3Int(a[0], a[1], a[2]));
                            if (newCube.triangles.Count > 0)
                            {
                                buildNeighbours.Add(newCube);
                            }
                            ///inform neighbours about eventuell change!
                        }
                    }
                }
            }
            List<MissingNeighbourData> missingNeighbours = new List<MissingNeighbourData>();
            int buildCount = buildNeighbours.Count;
            for (int i = 0; i < buildCount; ++i)
            {
                BuildEdgesFor(buildNeighbours[i], missingNeighbours, true);
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
            return x == 0 || x % (vertexSize - 1) == 0
                || y == 0 || y % (vertexSize - 1) == 0
                || z == 0 || z % (vertexSize - 1) == 0;
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