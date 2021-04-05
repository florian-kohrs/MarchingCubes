using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeThreadedChunk
    {

        public void InitializeWithMeshData(TriangleBuilder[] tris, int activeTris, float[] points, float surfaceLevel)
        {
            this.surfaceLevel = surfaceLevel;
            //chunkHandler = handler;
            this.points = points;
            BuildFromTriangleArray(tris, activeTris);
            BuildChunkEdges();
        }

        public int LOD;

        public Action<MeshData> BuildMeshTrigger;

        public MarchingCubeChunkThreadWrapper wrapper;

        // protected Vector4[] firstPoint;

        protected const int MAX_TRIANGLES_PER_MESH = 65000;

        public bool IsEmpty => triCount == 0;

        /// <summary>
        /// chunk is completly underground
        /// </summary>
        public bool IsCompletlySolid => IsEmpty && points[0] >= surfaceLevel;

        /// <summary>
        /// chunk is completly air
        /// </summary>
        public bool IsCompletlyAir => IsEmpty && points[0] < surfaceLevel;

        //public IMarchingCubeChunkHandler chunkHandler;

        public Dictionary<Vector3Int, HashSet<MarchingCubeEntity>> NeighboursReachableFrom = new Dictionary<Vector3Int, HashSet<MarchingCubeEntity>>();

        public IEnumerable<Vector3Int> NeighbourIndices => NeighboursReachableFrom.Keys;

        public void AddNeighbourFromEntity(Vector3Int v3, MarchingCubeEntity from)
        {
            HashSet<MarchingCubeEntity> r;
            if (!NeighboursReachableFrom.TryGetValue(v3, out r))
            {
                r = new HashSet<MarchingCubeEntity>();
                NeighboursReachableFrom.Add(v3, r);
            }
            r.Add(from);
        }

        public void RemoveNeighbourFromEntity(Vector3Int v3, MarchingCubeEntity from)
        {
            HashSet<MarchingCubeEntity> r;
            if (NeighboursReachableFrom.TryGetValue(v3, out r))
            {
                r.Remove(from);
                if (r.Count == 0)
                {
                    NeighboursReachableFrom.Remove(v3);
                }
            }
        }

        protected float[] points;

        public Vector3Int chunkOffset;

        public int VertexSize => MarchingCubeChunkHandler.ChunkSize + 1;

        public Vector3Int ChunkOffset { get => chunkOffset; set => chunkOffset = value; }

        public int NeighbourCount => NeighboursReachableFrom.Count;

        protected float surfaceLevel;

        public Color[] colorData;
        public Vector3[] vertices;
        public int[] meshTriangles;


        /// <summary>
        /// maybe reference to pathtriangles instead?
        /// </summary>
        //protected List<Triangle> allTriangles;

        //protected List<Triangle> AllTriangles
        //{
        //    get
        //    {
        //        if (allTriangles == null)
        //        {
        //            BuildAllTriangles();
        //        }
        //        return allTriangles;
        //    }
        //}

        //protected void BuildAllTriangles()
        //{
        //    allTriangles = new List<Triangle>();
        //    for (int x = 0; x < MarchingCubeChunkHandler.ChunkSize; x++)
        //    {
        //        for (int y = 0; y < MarchingCubeChunkHandler.ChunkSize; y++)
        //        {
        //            for (int z = 0; z < MarchingCubeChunkHandler.ChunkSize; z++)
        //            {
        //                MarchingCubeEntity e = cubeEntities[x, y, z];
        //                if (e != null)
        //                {
        //                    foreach (PathTriangle t in e.triangles)
        //                    {
        //                        allTriangles.Add(t.tri);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}


        protected int triCount;

        protected int trisLeft;

        //protected MarchingCubeEntity[,,] cubeEntities;

        //public MarchingCubeEntity[,,] CubeEntities => cubeEntities;

        public Dictionary<int, MarchingCubeEntity> cubeEntities;


        public MarchingCubeEntity GetEntityAt(Vector3Int v3)
        {
            MarchingCubeEntity e;
            cubeEntities.TryGetValue(IndexFromCoord(v3), out e);
            return e;
        }

        public MarchingCubeEntity GetEntityAt(int x, int y, int z)
        {
            MarchingCubeEntity e;
            cubeEntities.TryGetValue(IndexFromCoord(x, y, z), out e);
            return e;
        }


        protected Vector4 GetHeightDataFrom(int x, int y, int z)
        {
            Vector3 v3 = MarchingCubeChunkHandler.AnchorFromChunkIndex(chunkOffset);
            return BuildVector4(v3, points[IndexFromCoord(x, y, z)]);
        }

        protected Vector4 BuildVector4(Vector3 v3, float w)
        {
            return new Vector4(v3.x, v3.y, v3.z, w);
        }

        protected Vector4 BuildVector4FromCoord(Vector3 v3, int x, int y, int z)
        {
            return new Vector4(v3.x, v3.y, v3.z, points[IndexFromCoord(x, y, z)]);
        }

        protected Vector4[] GetCubeCornersForPoint(Vector3Int p)
        {
            Vector3 v3 = MarchingCubeChunkHandler.AnchorFromChunkIndex(chunkOffset);
            return new Vector4[]
            {
                BuildVector4FromCoord(v3,p.x, p.y, p.z),
                BuildVector4FromCoord(v3,p.x + 1, p.y, p.z),
                BuildVector4FromCoord(v3,p.x + 1, p.y, p.z + 1),
                BuildVector4FromCoord(v3,p.x, p.y, p.z + 1),
                BuildVector4FromCoord(v3,p.x, p.y + 1, p.z),
                BuildVector4FromCoord(v3,p.x + 1, p.y + 1, p.z),
                BuildVector4FromCoord(v3,p.x + 1, p.y + 1, p.z + 1),
                BuildVector4FromCoord(v3,p.x, p.y + 1, p.z + 1)
            };
        }

        protected int[] GetCubeCornerIndicesForPoint(Vector3Int p)
        {
            return new int[]
            {
            IndexFromCoord(p.x, p.y, p.z),
            IndexFromCoord(p.x + 1, p.y, p.z),
            IndexFromCoord(p.x + 1, p.y, p.z + 1),
            IndexFromCoord(p.x, p.y, p.z + 1),
            IndexFromCoord(p.x, p.y + 1, p.z),
            IndexFromCoord(p.x + 1, p.y + 1, p.z),
            IndexFromCoord(p.x + 1, p.y + 1, p.z + 1),
            IndexFromCoord(p.x, p.y + 1, p.z + 1)
            };
        }

        public virtual void March(Vector3Int p, float[] points)
        {
            MarchingCubeEntity e = new MarchingCubeEntity();
            e.origin = p;
            Vector4[] cubeCorners = GetCubeCornersForPoint(p);

            short cubeIndex = 0;
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
                triCount++;

            }
            //e.BuildInternNeighbours();
            cubeEntities[IndexFromCoord(p)] = e;
        }

        protected void BuildChunkEdges()
        {
            if (IsEmpty)
            {
                return;
            }
            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            foreach (MarchingCubeEntity e in cubeEntities.Values)
            {
                e.BuildInternNeighbours();
                if ((e.origin.x + e.origin.y + e.origin.z) % 2 == 0 || IsBorderPoint(e.origin))
                {
                    if(!e.BuildNeighbours(GetEntityAt, IsInBounds, trisWithNeighboursOutOfBounds))
                    {
                        missingNeighbours.Add(e, trisWithNeighboursOutOfBounds);

                        for (int i = 0; i < trisWithNeighboursOutOfBounds.Count; i++)
                        {
                            Vector3Int target = chunkOffset + trisWithNeighboursOutOfBounds[i].neighbour.offset;
                            AddNeighbourFromEntity(target, e);
                        }
                        trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
                    }
                }
            }
        }

        //have to be resolved after joined main thread;
        public Dictionary<MarchingCubeEntity, List<MissingNeighbourData>> missingNeighbours = new Dictionary<MarchingCubeEntity, List<MissingNeighbourData>>();

        protected virtual Vector3 InterpolateVerts(Vector4 v1, Vector4 v2)
        {
            Vector3 v = v1.GetXYZ();
            float t = (surfaceLevel - v1.w) / (v2.w - v1.w);
            return v + t * (v2.GetXYZ() - v);
        }


        protected Vector3Int CoordFromIndex(int i)
        {
            return new Vector3Int
               ((i % (VertexSize * VertexSize) % VertexSize)
               , (i % (VertexSize * VertexSize) / VertexSize)
               , (i / (VertexSize * VertexSize))
               );
        }

        protected int IndexFromCoord(int x, int y, int z)
        {
            return z * VertexSize * VertexSize + y * VertexSize + x;
        }

        protected int IndexFromCoord(Vector3Int v)
        {
            return IndexFromCoord(v.x, v.y, v.z);
        }


        protected void ResetMesh()
        {
            triCount = 0;
        }


        protected void Build()
        {
            Vector3Int v = new Vector3Int();

            for (int x = 0; x < MarchingCubeChunkHandler.ChunkSize; x++)
            {
                v.x = x;
                for (int y = 0; y < MarchingCubeChunkHandler.ChunkSize; y++)
                {
                    v.y = y;
                    for (int z = 0; z < MarchingCubeChunkHandler.ChunkSize; z++)
                    {
                        v.z = z;
                        March(v, points);
                    }
                }
            }
            PrepareModifiedMesh();
        }

        protected bool GetOrAddEntityAt(int x, int y, int z, out MarchingCubeEntity e)
        {
            int key = IndexFromCoord(x, y, z);
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


        public void BuildFromTriangleArray(TriangleBuilder[] ts, int activeTris)
        {
            triCount = activeTris * 3;
            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            MarchingCubeEntity cube;
            int chunksize = MarchingCubeChunkHandler.ChunkSize;
            cubeEntities = new Dictionary<int, MarchingCubeEntity>(chunksize * chunksize * chunksize / 15);
            foreach (TriangleBuilder t in ts)
            {
                if (totalTreeCount >= activeTris)
                {
                    if (usedTriCount > 0)
                    {
                        BuildMeshTrigger(StoreCurrentMeshData(false));
                    }
                    break;
                }
                if (!GetOrAddEntityAt(t.Origin, out cube))
                {
                    cube.triangulationIndex = t.TriIndex;
                }
                cube.triangles.Add(new PathTriangle(t.tri));
                for (int i = 0; i < 3; i++)
                {
                    meshTriangles[usedTriCount + i] = usedTriCount + i;
                    vertices[usedTriCount + i] = t.tri[i];
                    colorData[usedTriCount + i] = Color.yellow;
                }
                usedTriCount += 3;
                totalTreeCount++;
                if (usedTriCount >= MAX_TRIANGLES_PER_MESH || usedTriCount >= trisLeft)
                {
                    trisLeft -= meshTriangles.Length;
                    BuildMeshTrigger(StoreCurrentMeshData(trisLeft > 0));
                    usedTriCount = 0;
                }
            }
        }

        protected void PrepareModifiedMesh()
        {
            //Vector3[] vertices = new Vector3[triCount];
            //int[] meshTriangles = new int[triCount];
            //colorData = new Color[triCount];

            //int count = 0;

            //foreach (MarchingCubeEntity e in cubeEntities.Values)
            //{
            //    foreach (PathTriangle t in e.triangles)
            //    {
            //        for (int i = 0; i < 3; i++)
            //        {
            //            meshTriangles[count + i] = count + i;
            //            vertices[count + i] = t.tri[i];
            //            colorData[count + i] = Color.yellow;
            //        }
            //        count += 3;
            //    }
            //}
            //BuildMeshTrigger(CurrentMeshData);
        }

        public void ResetArrayData()
        {
            int size = Mathf.Min(trisLeft, MAX_TRIANGLES_PER_MESH + 1);
            meshTriangles = new int[size];
            vertices = new Vector3[size];
            colorData = new Color[size];
        }

        public void Rebuild()
        {
            ResetMesh();
            Build();
        }

        protected MeshData StoreCurrentMeshData(bool rebuild)
        {
            MeshData data = new MeshData(meshTriangles, vertices, colorData);
            if (rebuild)
            {
                ResetArrayData();
            }

            return data;
        }

        public void RebuildAround(MarchingCubeEntity e)
        {
            triCount -= e.triangles.Count * 3;

            Vector3Int v = new Vector3Int();
            for (int x = e.origin.x - 1; x <= e.origin.x + 1; x++)
            {
                v.x = x;
                for (int y = e.origin.y - 1; y <= e.origin.y + 1; y++)
                {
                    v.y = y;
                    for (int z = e.origin.z - 1; z <= e.origin.z + 1; z++)
                    {
                        v.z = z;
                        if (IsInBounds(v))
                        {
                            March(v, points);
                            ///inform neighbours about eventuell change!
                        }
                    }
                }
            }
            PrepareModifiedMesh();
        }

        protected IEnumerable<Vector3Int> GetIndicesAround(MarchingCubeEntity e)
        {
            Vector3Int r = e.origin;
            yield return new Vector3Int(r.x - 1, r.y, r.z);
            yield return new Vector3Int(r.x + 1, r.y, r.z);
            yield return new Vector3Int(r.x, r.y - 1, r.z);
            yield return new Vector3Int(r.x, r.y + 1, r.z);
            yield return new Vector3Int(r.x, r.y, r.z - 1);
            yield return new Vector3Int(r.x, r.y, r.z + 1);
        }

        protected IEnumerable<Vector3Int> GetValidIndicesAround(MarchingCubeEntity e)
        {
            foreach (Vector3Int v3 in GetIndicesAround(e))
            {
                if (IsInBounds(v3))
                {
                    yield return v3;
                }
            }
        }

        protected bool IsInBounds(Vector3Int v)
        {
            return
                v.x >= 0 && v.x < MarchingCubeChunkHandler.ChunkSize
                && v.y >= 0 && v.y < MarchingCubeChunkHandler.ChunkSize
                && v.z >= 0 && v.z < MarchingCubeChunkHandler.ChunkSize;
        }

        protected bool IsBorderPoint(Vector3 p)
        {
            return p.x % (MarchingCubeChunkHandler.ChunkSize - 1) == 0
                || p.y % (MarchingCubeChunkHandler.ChunkSize - 1) == 0
                || p.z % (MarchingCubeChunkHandler.ChunkSize - 1) == 0;
        }


        public PathTriangle GetTriangleFromRayHit(RaycastHit hit)
        {
            MarchingCubeEntity cube = GetClosestEntity(hit.point);
            return cube.GetTriangleWithNormal(hit.normal);
        }

        public void EditPointsAroundRayHit(int sign, RaycastHit hit, int editDistance)
        {
            MarchingCubeEntity e = GetEntityFromRayHit(hit);
            //Triangle t = e.GetTriangleWithNormal(hit.normal).tri;

            int[] cornerIndices = GetCubeCornerIndicesForPoint(e.origin);
            float delta = sign * 1f /** Time.deltaTime*/;

            foreach (int i in cornerIndices)
            {
                points[i] += delta;
            }

            for (int i = 0; i < points.Length; i++)
            {
                points[i] += delta;
            }

            if (IsBorderPoint(e.origin))
            {
                wrapper.chunkHandler.EditNeighbourChunksAt(chunkOffset, e.origin, delta);
            }
            RebuildAround(e);
        }

        public float Spacing { get; set; }

        public MarchingCubeEntity GetClosestEntity(Vector3 v3)
        {
            Vector3 rest = v3 - GetAnchorPosition();
            return GetEntityAt((int)rest.x, (int)rest.y, (int)rest.z);
        }

        public MarchingCubeEntity GetEntityFromRayHit(RaycastHit hit)
        {
            return GetClosestEntity(hit.point);
        }


        public Vector3 GetAnchorPosition()
        {
            return wrapper.transform.position + (chunkOffset * MarchingCubeChunkHandler.ChunkSize).Mul(Spacing);
        }

        public void EditPointsNextToChunk(IMarchingCubeChunk chunk, MarchingCubeEntity e, Vector3Int offset, float delta)
        {
            int[] cornerIndices = GetCubeCornerIndicesForPoint(e.origin);

            foreach (int index in cornerIndices)
            {
                Vector3Int indexPoint = CoordFromIndex(index);
                Vector3Int pointOffset = new Vector3Int();
                for (int i = 0; i < 3; i++)
                {
                    if (offset[i] == 0)
                    {
                        pointOffset[i] = 0;
                    }
                    else
                    {
                        int indexOffset = Mathf.CeilToInt((indexPoint[i] / (MarchingCubeChunkHandler.ChunkSize - 2f)) - 1);
                        pointOffset[i] = -indexOffset;
                    }
                }

                if (pointOffset == offset)
                {
                    points[index] += delta;
                }
            }
            RebuildAround(e);
        }


    }
}