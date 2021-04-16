using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MarchingCubeChunk : CompressedMarchingCubeChunk, IMarchingCubeInteractableChunk, IHasInteractableMarchingCubeChunk
    {

        public override void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel, Action OnDone)
        {
            Debug.LogWarning("This class does not support concurrency! Will use base class instead. Use " + nameof(MarchingCubeChunkThreaded) + "instead!");
            base.InitializeWithMeshDataParallel(tris, points, handler,neighbourLODs, surfaceLevel, OnDone);
        }

        public override void InitializeWithMeshData(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel)
        {
            children.Add(new BaseMeshChild(GetComponent<MeshFilter>(), GetComponent<MeshRenderer>(), GetComponent<MeshCollider>(), new Mesh()));
            BuildMeshData(tris, points, handler, neighbourLODs, surfaceLevel);
        }

        protected override void BuildMeshData(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel)
        {
            HasStarted = true;
            this.surfaceLevel = surfaceLevel;
            this.neighbourLODs = neighbourLODs;
            triCount = tris.Length * 3;
            careAboutNeighbourLODS = neighbourLODs.HasNeighbourWithHigherLOD(lod);
            chunkHandler = handler;
            this.points = points;

            BuildFromTriangleArray(tris);
            //ResetAll();
            //BuildAll();

            if (lod <= MarchingCubeChunkHandler.DEFAULT_MIN_CHUNK_LOD_SIZE)
            {
                BuildChunkEdges();
            }
            else
            {
                FindConnectedChunks();
            }

            //  BuildMeshFromCurrentTriangles();

            //if (careAboutNeighbourLODS)
            //{
            //    BuildMeshFromCurrentTriangles();
            //}

            BuildMeshToConnectHigherLodChunks();

            IsReady = true;

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

        protected void AddCurrentMeshDataChild()
        {
            GameObject g = new GameObject();
            g.transform.parent = transform;
            g.AddComponent<MeshFilter>();
        }

        public IMarchingCubeInteractableChunk GetChunk => this;


        public Dictionary<int, MarchingCubeEntity> cubeEntities = new Dictionary<int, MarchingCubeEntity>();

        protected Dictionary<Vector3Int, MarchingCubeEntity> higherLodNeighbourCubes = new Dictionary<Vector3Int, MarchingCubeEntity>(new Vector3EqualityComparer());

        public MarchingCubeEntity GetEntityAt(Vector3Int v3)
        {
            MarchingCubeEntity e;
            cubeEntities.TryGetValue(PointIndexFromCoord(v3), out e);
            return e;
        }

        public MarchingCubeEntity GetEntityAt(int x, int y, int z)
        {
            MarchingCubeEntity e;
            cubeEntities.TryGetValue(PointIndexFromCoord(x, y, z), out e);
            return e;
        }


        protected NoiseFilter noiseFilter;


        protected Vector4 BuildVector4(Vector3 v3, float w)
        {
            return new Vector4(v3.x, v3.y, v3.z, w);
        }

        public virtual MarchingCubeEntity March(Vector3Int p)
        {
            MarchingCubeEntity e = new MarchingCubeEntity();
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
                e.triangles.Add(new PathTriangle(tri));
                triCount += 3;

            }

            if (e.triangles.Count > 0)
            {
                cubeEntities[PointIndexFromCoord(p)] = e;
            }
            return e;
        }



        protected void BuildChunkEdges()
        {
            if (IsEmpty)
                return;

            lodNeighbourPointCorrectionLookUp = new Dictionary<Vector3, Vector3>(new Vector3EqualityComparer());

            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            MissingNeighbourData t;

            var @enum = cubeEntities.Values.GetEnumerator();
            MarchingCubeEntity e;
            while (@enum.MoveNext())
            {
                e = @enum.Current;
                e.BuildInternNeighbours();
                if ((e.origin.x + e.origin.y + e.origin.z) % 2 == 0 || IsBorderCube(e.origin))
                {
                    if (!e.BuildNeighbours(GetEntityAt, IsCubeInBounds, trisWithNeighboursOutOfBounds))
                    {
                        for (int i = 0; i < trisWithNeighboursOutOfBounds.Count; i++)
                        {
                            t = trisWithNeighboursOutOfBounds[i];
                            //Vector3Int offset = t.neighbour.offset.Map(Math.Sign);
                            Vector3Int target = chunkOffset + t.neighbour.offset;
                            IMarchingCubeChunk chunk;
                            AddNeighbourFromEntity(target, e);
                            if (chunkHandler.TryGetReadyChunkAt(target, out chunk))
                            {
                                if (chunk is IMarchingCubeInteractableChunk c)
                                {
                                    if (c.LOD == lod)
                                    {
                                        Vector3Int pos = (e.origin + t.neighbour.offset).Map(ClampInChunk);
                                        MarchingCubeEntity cube = c.GetEntityAt(pos);
                                        e.BuildSpecificNeighbourInNeighbour(cube, e.triangles[t.neighbour.triangleIndex], t.neighbour.relevantVertexIndices, t.neighbour.rotatedEdgePair);
                                    }
                                    else if (c.LOD > lod)
                                    {
                                        Vector3Int pos = (e.origin + t.neighbour.offset).Map(ClampInChunk);

                                        float lodDiff = c.LOD / lod;
                                        ///pos needed to be divided by lodDiff or something
                                        MarchingCubeEntity cube = c.GetEntityAt(pos);
                                        CorrectMarchingCubeInDirection(e.origin, t, c.LOD, t.neighbour.offset);
                                    }
                                }
                            }
                            else if (careAboutNeighbourLODS)
                            {
                                int neighbourLod = neighbourLODs.GetLodFromNeighbourInDirection(t.neighbour.offset);
                                if (neighbourLod > lod)
                                {
                                    CorrectMarchingCubeInDirection(e.origin, t, neighbourLod, t.neighbour.offset);
                                }
                            }
                        }
                        trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
                    }
                }
            }
        }

        protected void FindConnectedChunks()
        {
            if (IsEmpty)
                return;

            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            MissingNeighbourData t;
            IMarchingCubeChunk c;

            var @enum = cubeEntities.Values.GetEnumerator();
            MarchingCubeEntity e;
            while (@enum.MoveNext())
            {
                e = @enum.Current;
                if (IsBorderCube(e.origin) && !e.FindMissingNeighbours(IsCubeInBounds, trisWithNeighboursOutOfBounds))
                {
                    for (int i = 0; i < trisWithNeighboursOutOfBounds.Count; i++)
                    {
                        t = trisWithNeighboursOutOfBounds[i];
                        Vector3Int target = chunkOffset + t.neighbour.offset;
                        AddNeighbourFromEntity(target, e);

                        if (chunkHandler.TryGetReadyChunkAt(target, out c))
                        {
                            if (c.LOD > lod)
                            {
                                Vector3Int pos = (e.origin + t.neighbour.offset).Map(ClampInChunk);

                                float lodDiff = c.LOD / lod;
                                ///pos needed to be divided by lodDiff or something
                                CorrectMarchingCubeInDirection(e.origin, t, c.LOD, t.neighbour.offset);
                            }
                        }
                        else if (careAboutNeighbourLODS)
                        {
                            int neighbourLod = neighbourLODs.GetLodFromNeighbourInDirection(t.neighbour.offset);
                            if (neighbourLod > lod)
                            {
                                CorrectMarchingCubeInDirection(e.origin, t, neighbourLod, t.neighbour.offset);
                            }
                        }
                    }
                }
            }
        }

        protected void BuildEdgesFor(MarchingCubeEntity e, List<MissingNeighbourData> addMissingNeighboursHere, bool overrideEdges)
        {
            int currentCount = addMissingNeighboursHere.Count;

            e.BuildInternNeighbours();
            e.BuildNeighbours(GetEntityAt, IsCubeInBounds, addMissingNeighboursHere, overrideEdges);
        }


        //protected List<MissingNeighbourData> missingHigherLODNeighbour = new List<MissingNeighbourData>();

        protected Dictionary<Vector3, Vector3> lodNeighbourPointCorrectionLookUp;

        protected bool GetPointWithCorner(MarchingCubeEntity e, int a, int b, out Vector3 result)
        {
            for (int i = 0; i < e.triangles.Count; i++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Vector3 v = e.triangles[i].tri[x];
                    int aIndex = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[e.triangulationIndex][i * 3 + x]];
                    int bIndex = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[e.triangulationIndex][i * 3 + x]];
                    if (aIndex == a && bIndex == b)
                    {
                        result = v;
                        return true;
                    }
                }
            }
            result = Vector3.zero;
            return false;
        }





        protected virtual Vector3 InterpolatePositions(Vector3 v1, Vector3 v2, float p)
        {
            return v1 + p * (v2 - v1);
        }


        protected virtual void ResetAll()
        {
            ResetMeshDisplayers();
            cubeEntities = new Dictionary<int, MarchingCubeEntity>();
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

        protected bool GetOrAddEntityAt(int x, int y, int z, out MarchingCubeEntity e)
        {
            int key = PointIndexFromCoord(x, y, z);
            if (!cubeEntities.TryGetValue(key, out e))
            {
                e = new MarchingCubeEntity();
                e.origin = new Vector3Int(x, y, z);
                cubeEntities[key] = e;
                return false;
            }
            return true;
        }

        protected bool GetOrAddEntityAt(Vector3Int v3, out MarchingCubeEntity e)
        {
            return GetOrAddEntityAt(v3.x, v3.y, v3.z, out e);
        }

        protected override void BuildFromTriangleArray(TriangleBuilder[] ts, bool buildMeshAswell = true)
        {
            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            MarchingCubeEntity cube;
            cubeEntities = new Dictionary<int, MarchingCubeEntity>(vertexSize * vertexSize * vertexSize / 15);
            TriangleBuilder t;
            for(int i = 0; i< ts.Length; i++) 
            {
                t = ts[i];
                if (!GetOrAddEntityAt(t.Origin, out cube))
                {
                    cube.triangulationIndex = t.TriIndex;
                }
                PathTriangle pathTri = new PathTriangle(t.tri);
                cube.triangles.Add(pathTri);
                if (buildMeshAswell)
                {
                    AddTriangleToMeshData(pathTri, ref usedTriCount, ref totalTreeCount);
                }
            }
        }

        protected void BuildMeshFromCurrentTriangles()
        {
            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            var @enum = cubeEntities.Values.GetEnumerator();
            MarchingCubeEntity e;
            while (@enum.MoveNext())
            {
                e = @enum.Current;
                for (int i = 0; i < e.triangles.Count; i++)
                {
                    AddTriangleToMeshData(e.triangles[i], ref usedTriCount, ref totalTreeCount, true);
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
                cubeEntities.Remove(PointIndexFromCoord(e.origin));
            }
            Vector3Int v = new Vector3Int();

            List<MarchingCubeEntity> buildNeighbours = new List<MarchingCubeEntity>();

            for (int x = origin.x - 1; x <= origin.x + 1; x++)
            {
                v.x = x;
                for (int y = origin.y - 1; y <= origin.y + 1; y++)
                {
                    v.y = y;
                    for (int z = origin.z - 1; z <= origin.z + 1; z++)
                    {
                        v.z = z;
                        if (IsCubeInBounds(v))
                        {
                            MarchingCubeEntity neighbourEntity;
                            if (cubeEntities.TryGetValue(PointIndexFromCoord(v), out neighbourEntity))
                            {
                                triCount -= neighbourEntity.triangles.Count * 3;
                                cubeEntities.Remove(PointIndexFromCoord(v));
                            }
                            MarchingCubeEntity newCube = March(v);
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
            for (int i = 0; i < buildNeighbours.Count; i++)
            {
                BuildEdgesFor(buildNeighbours[i], missingNeighbours, true);
            }

            ResetMeshDisplayers();
            BuildMeshFromCurrentTriangles();
        }

        protected Vector3Int[] GetIndicesAround(MarchingCubeEntity e)
        {
            Vector3Int v3 = e.origin;
            Vector3Int[] r = new Vector3Int[6];
            r[0] = new Vector3Int(v3.x + 1, v3.y, v3.z);
            r[1] = new Vector3Int(v3.x - 1, v3.y, v3.z);
            r[2] = new Vector3Int(v3.x, v3.y + 1, v3.z);
            r[3] = new Vector3Int(v3.x, v3.y - 1, v3.z);
            r[4] = new Vector3Int(v3.x, v3.y, v3.z + 1);
            r[5] = new Vector3Int(v3.x, v3.y, v3.z - 1);
            return r;
        }

        protected List<Vector3Int> GetValidIndicesAround(MarchingCubeEntity e)
        {
            Vector3Int[] aroundVertices = GetIndicesAround(e);
            List<Vector3Int> r = new List<Vector3Int>(aroundVertices.Length);
            Vector3Int v3;
            for (int i = 0; i < aroundVertices.Length; i++)
            {
                v3 = aroundVertices[i];
                if (IsCubeInBounds(v3))
                {
                    r.Add(v3);
                }
            }
            return r;
        }



        protected bool IsBorderCube(Vector3 p)
        {
            return p.x == 0 || p.x % (vertexSize - 1) == 0
                || p.y == 0 || p.y % (vertexSize - 1) == 0
                || p.z == 0 || p.z % (vertexSize - 1) == 0;
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
                return 1 + PointsPerAxis * PointsPerAxis;
            else if (local == 3)
                return PointsPerAxis * PointsPerAxis;
            else if (local == 4)
                return PointsPerAxis;
            else if (local == 5)
                return PointsPerAxis + 1;
            else if (local == 6)
                return PointsPerAxis + PointsPerAxis * PointsPerAxis + 1;
            else if (local == 7)
                return PointsPerAxis + PointsPerAxis * PointsPerAxis;
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
                pointIndex -= PointsPerAxis * vertexSize;
            }
            else if (dir.y < 0)
            {
                pointIndex += PointsPerAxis * vertexSize;
            }
            if (dir.y > 0)
            {
                pointIndex -= PointsPerAxis * PointsPerAxis * vertexSize;
            }
            else if (dir.y < 0)
            {
                pointIndex += PointsPerAxis * PointsPerAxis * vertexSize;
            }
            return pointIndex;
        }

        public void EditPointsAroundRayHit(float delta, RaycastHit hit, int editDistance)
        {
            MarchingCubeEntity e = GetEntityFromRayHit(hit);
            int triIndex = e.GetTriangleIndexWithNormalOrClosest(hit.normal, hit.point) * 3;
            int startPointIndex = PointIndexFromCoord(e.origin);
            float[] cornerIndices = new float[8];
            for (int i = 0; i < cornerIndices.Length; i++)
            {
                cornerIndices[i] = 0.4f;
            }
            for (int i = 0; i < 3; i++)
            {
                int cornerA = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[e.triangulationIndex][triIndex + i]];
                int cornerB = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[e.triangulationIndex][triIndex + i]];
                cornerIndices[cornerA] += 0.2f;
                cornerIndices[cornerB] += 0.2f;
            }

            for (int i = 0; i < cornerIndices.Length; i++)
            {
                points[startPointIndex + LocalCornerIndexToGlobalDelta(i)] += cornerIndices[i] * delta;
            }

            //foreach (int i in cornerIndices)
            //{
            //    points[i] += delta;
            //}

            //for (int i = 0; i < points.Length; i++)
            //{
            //    points[i] += delta;
            //}

            if (IsBorderCube(e.origin))
            {
                chunkHandler.EditNeighbourChunksAt(chunkOffset, e.origin, delta);
            }

            RebuildAround(e, e.origin);
        }


        public MarchingCubeEntity GetClosestEntity(Vector3 v3)
        {
            Vector3 rest = v3 - GetAnchorPosition();
            rest /= lod;
            return GetEntityAt((int)rest.x, (int)rest.y, (int)rest.z);
        }

        public MarchingCubeEntity GetEntityFromRayHit(RaycastHit hit)
        {
            return GetClosestEntity(hit.point);
        }

        //protected float RelativeSpacing 0>

        public Vector3 GetAnchorPosition()
        {
            return transform.position + (chunkOffset * MarchingCubeChunkHandler.ChunkSize);
        }

        public void EditPointsNextToChunk(IMarchingCubeChunk chunk, Vector3Int entityOrigin, Vector3Int offset, float delta)
        {
            int[] cornerIndices = GetCubeCornerIndicesForPoint(entityOrigin);
            int length = cornerIndices.Length;
            int index;
            for (int i = 0; i < length; i++)
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