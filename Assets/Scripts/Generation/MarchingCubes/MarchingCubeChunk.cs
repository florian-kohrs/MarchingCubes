using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MarchingCubeChunk : MonoBehaviour
    {

        public void InitializeWithMeshData(Material mat, Triangle[] tris, ComputeBuffer noiseBuffer, IMarchingCubeChunkHandler handler, float surfaceLevel)
        {
            this.surfaceLevel = surfaceLevel;
            chunkHandler = handler;

            cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            points = new Vector4[VertexSize * VertexSize * VertexSize];

            noiseBuffer.GetData(points, 0, 0, points.Length);
            noiseBuffer.Release();

            this.mat = mat;
            triCount = tris.Length;
            BuildFromTriangleArray(tris);
            ApplyChanges();
            BuildChunkEdges();
        }

        public bool IsEmpty => AllTriangles.Count <= 0;

        /// <summary>
        /// chunk is completly underground
        /// </summary>
        public bool IsDense => IsEmpty && points[0].w >= surfaceLevel;

        /// <summary>
        /// chunk is completly air
        /// </summary>
        public bool IsSparse => IsEmpty && points[0].w < surfaceLevel;

        public IMarchingCubeChunkHandler chunkHandler;

        public int ChunkSize => MarchingCubeChunkHandler.VoxelsPerChunkAxis;

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

        protected MeshFilter meshFilter;
        protected MeshCollider meshCollider;

        protected Vector4[] points;

        public Vector3Int chunkOffset;

        public int VertexSize => ChunkSize + 1;

        protected float surfaceLevel;

        protected Color[] colorData;

        protected List<Triangle> allTriangles;

        protected List<Triangle> AllTriangles
        {
            get
            {
                if (allTriangles == null)
                {
                    BuildAllTriangles();
                }
                return allTriangles;
            }
        }

        protected void BuildAllTriangles()
        {
            allTriangles = new List<Triangle>();
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        MarchingCubeEntity e = cubeEntities[x, y, z];
                        if (e != null)
                        {
                            foreach (PathTriangle t in e.triangles)
                            {
                                allTriangles.Add(t.tri);
                            }
                        }
                    }
                }
            }
        }


        protected int triCount;

        protected MarchingCubeEntity[,,] cubeEntities;

        public MarchingCubeEntity[,,] CubeEntities => cubeEntities;

        protected Mesh mesh;

        protected NoiseFilter noiseFilter;

        protected Vector4[] GetCubeCornersForPoint(Vector3Int p)
        {
            return new Vector4[]
            {
            points[IndexFromCoord(p.x, p.y, p.z)],
            points[IndexFromCoord(p.x + 1, p.y, p.z)],
            points[IndexFromCoord(p.x + 1, p.y, p.z + 1)],
            points[IndexFromCoord(p.x, p.y, p.z + 1)],
            points[IndexFromCoord(p.x, p.y + 1, p.z)],
            points[IndexFromCoord(p.x + 1, p.y + 1, p.z)],
            points[IndexFromCoord(p.x + 1, p.y + 1, p.z + 1)],
            points[IndexFromCoord(p.x, p.y + 1, p.z + 1)]
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

        public virtual void March(Vector3Int p, Vector4[] points)
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

                tri.origin = p;
                tri.triangulationIndex = cubeIndex;
                tri.c = InterpolateVerts(cubeCorners[a0], cubeCorners[b0]);
                tri.b = InterpolateVerts(cubeCorners[a1], cubeCorners[b1]);
                tri.a = InterpolateVerts(cubeCorners[a2], cubeCorners[b2]);
                e.triangles.Add(new PathTriangle(this, tri));
                triCount++;

            }
            //e.BuildInternNeighbours();
            cubeEntities[p.x, p.y, p.z] = e;
        }

        protected void BuildChunkEdges()
        {
            if (IsEmpty)
            {
                return;
            }

            Vector3Int v3 = new Vector3Int();
            for (int x = 0; x < ChunkSize; x++)
            {
                v3.x = x;
                for (int y = 0; y < ChunkSize; y++)
                {
                    v3.y = y;
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        v3.z = z;
                        if (CubeEntities[x, y, z] != null)
                        {

                            CubeEntities[x, y, z].BuildInternNeighbours();

                            if ((x + y + z) % 2 == 0 || IsBorderPoint(v3))
                            {
                                List<MissingNeighbourData> trisWithNeighboursOutOfBounds
                                   = CubeEntities[x, y, z].BuildNeighbours(CubeEntities, IsInBounds);

                                if (trisWithNeighboursOutOfBounds != null)
                                {
                                    foreach (MissingNeighbourData t in trisWithNeighboursOutOfBounds)
                                    {
                                        //Vector3Int offset = t.neighbour.offset.Map(Math.Sign);
                                        Vector3Int target = chunkOffset + t.neighbour.offset;
                                        MarchingCubeChunk c;
                                        AddNeighbourFromEntity(target, CubeEntities[x, y, z]);
                                        if (chunkHandler.Chunks.TryGetValue(target, out c))
                                        {
                                            Vector3Int pos = (v3 + t.neighbour.offset).Map(i => i.FloorMod(ChunkSize));
                                            MarchingCubeEntity e = c.CubeEntities[pos.x, pos.y, pos.z];
                                            CubeEntities[x, y, z].BuildSpecificNeighbourInNeighbour(e, CubeEntities[x, y, z].triangles[t.neighbour.triangleIndex], t.neighbour.rotatedEdgePair);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


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

        public Mesh Mesh
        {
            get
            {
                if (mesh == null)
                {
                    mesh = new Mesh();
                    meshFilter = this.GetOrAddComponent<MeshFilter>();
                    meshFilter.mesh = mesh;

                    meshCollider = GetComponent<MeshCollider>();
                    meshCollider.sharedMesh = mesh;
                }
                return mesh;
            }
        }


        protected void ResetMesh()
        {
            triCount = 0;
            allTriangles = null;
        }

        protected Material mat;


        protected void Build()
        {
            Vector3Int v = new Vector3Int();

            for (int x = 0; x < ChunkSize; x++)
            {
                v.x = x;
                for (int y = 0; y < ChunkSize; y++)
                {
                    v.y = y;
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        v.z = z;
                        March(v, points);
                    }
                }
            }
            ApplyChanges();
        }

        protected MarchingCubeEntity GetEntityAt(int x, int y, int z)
        {
            MarchingCubeEntity result = CubeEntities[x, y, z];
            if (result == null)
            {
                result = new MarchingCubeEntity();
                result.origin = new Vector3Int(x, y, z);
                CubeEntities[x, y, z] = result;
            }
            return result;
        }

        public void BuildFromTriangleArray(Triangle[] ts)
        {
            allTriangles = new List<Triangle>();
            Vector3Int v = new Vector3Int();
            MarchingCubeEntity cube;
            cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            //for (int x = 0; x < ChunkSize; x++)
            //{
            //    v.x = x;
            //    for (int y = 0; y < ChunkSize; y++)
            //    {
            //        v.y = y;
            //        for (int z = 0; z < ChunkSize; z++)
            //        {
            //            v.z = z;
            //            cube = new MarchingCubeEntity();
            //            cube.origin = v;
            //            CubeEntities[x, y, z] = cube;
            //        }
            //    }
            //}

            foreach (Triangle t in ts)
            {
                cube = GetEntityAt(t.origin.x, t.origin.y, t.origin.z);
                cube.triangulationIndex = (short)t.triangulationIndex;
                cube.triangles.Add(new PathTriangle(this, t));
                allTriangles.Add(t);
            }

            //for (int x = 0; x < ChunkSize; x += 2)
            //{
            //    v.x = x;
            //    for (int y = 0; y < ChunkSize; y++)
            //    {
            //        v.y = y;
            //        for (int z = 0; z < ChunkSize; z++)
            //        {
            //            v.z = z;
            //            cube = CubeEntities[x, y, z];
            //            //cube.BuildInternNeighbours();
            //        }
            //    }
            //}
        }

        protected void ApplyChanges()
        {

            Vector3[] vertices = new Vector3[triCount * 3];
            int[] meshTriangles = new int[triCount * 3];
            colorData = new Color[triCount * 3];

            int count = 0;
            foreach (Triangle t in AllTriangles)
            {
                for (int x = 0; x < 3; x++)
                {
                    meshTriangles[count * 3 + x] = count * 3 + x;
                    vertices[count * 3 + x] = t[x];
                    colorData[count * 3 + x] = new Color(0.5471698f, 0.2647888f, 0.1f, 1);
                }
                count++;
            }

            ApplyChangesToMesh(vertices, meshTriangles);
        }

        protected void ApplyChangesToMesh(Vector3[] vertices, int[] meshTriangles)
        {
            Mesh.Clear();

            mesh.vertices = vertices;

            mesh.triangles = meshTriangles;
            mesh.colors = colorData;
            GetComponent<MeshRenderer>().material = mat;
            meshCollider.sharedMesh = mesh;

            mesh.RecalculateNormals();
        }

        public void Rebuild()
        {
            ResetMesh();
            Build();
        }

        public void RebuildAround(MarchingCubeEntity e)
        {
            allTriangles = null;
            triCount -= e.triangles.Count;

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
            ApplyChanges();
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
                v.x >= 0 && v.x < MarchingCubeChunkHandler.VoxelsPerChunkAxis
                && v.y >= 0 && v.y < MarchingCubeChunkHandler.VoxelsPerChunkAxis
                && v.z >= 0 && v.z < MarchingCubeChunkHandler.VoxelsPerChunkAxis;
        }

        protected bool IsBorderPoint(Vector3 p)
        {
            return p.x % (ChunkSize - 1) == 0
                || p.y % (ChunkSize - 1) == 0
                || p.z % (ChunkSize - 1) == 0;
        }

        public PathTriangle GetTriangleAt(int index)
        {
            Triangle t = AllTriangles[index];
            Vector3Int p = t.origin;
            return CubeEntities[p.x, p.y, p.z].triangles.Where(tri => tri.tri.Equals(t)).FirstOrDefault();
        }

        public MarchingCubeEntity GetCubeAtTriangleIndex(int index)
        {
            Triangle t = AllTriangles[index];
            Vector3Int p = t.origin;
            return CubeEntities[p.x, p.y, p.z];
        }

        public void EditPointsAroundTriangleIndex(int sign, int triIndex, int editDistance)
        {
            Triangle t = AllTriangles[triIndex];

            int[] cornerIndices = GetCubeCornerIndicesForPoint(t.origin);
            float delta = sign * 1f /** Time.deltaTime*/;

            foreach (int i in cornerIndices)
            {
                Vector4 newV4 = points[i];
                newV4.w += delta;
                points[i] = newV4;
            }

            //for (int i = 0; i < points.Length; i++)
            //{
            //    Vector4 newV4 = points[i];
            //    newV4.w += delta;
            //    points[i] = newV4;
            //}

            Vector3 p = CoordFromIndex(triIndex);

            if (IsBorderPoint(t.origin))
            {
                chunkHandler.EditNeighbourChunksAt(this, t.origin, delta);
            }
            RebuildAround(CubeEntities[t.origin.x, t.origin.y, t.origin.z]);
        }


        public void EditPointsNextToChunk(MarchingCubeChunk chunk, MarchingCubeEntity e, Vector3Int offset, float delta)
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
                        int indexOffset = Mathf.CeilToInt((indexPoint[i] / (MarchingCubeChunkHandler.VoxelsPerChunkAxis - 2f)) - 1);
                        pointOffset[i] = -indexOffset;
                    }
                }

                if (pointOffset == offset)
                {
                    Vector4 newV4 = points[index];
                    newV4.w += delta;
                    points[index] = newV4;
                }
            }
            RebuildAround(e);
        }



    }
}