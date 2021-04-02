using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MarchingCubeChunk : MonoBehaviour, IHasMarchingCubeChunk
    {

        public void InitializeWithMeshData(Material mat, TriangleBuilder[] tris, ComputeBuffer noiseBuffer, IMarchingCubeChunkHandler handler, float surfaceLevel)
        {
            this.surfaceLevel = surfaceLevel;
            chunkHandler = handler;
            points = new Vector4[VertexSize * VertexSize * VertexSize];

            noiseBuffer.GetData(points, 0, 0, points.Length);
            //firstPoint = new Vector4[1];
            //noiseBuffer.GetData(firstPoint, 0, 0, firstPoint.Length);
            noiseBuffer.Release();
            children.Add(new BaseMeshChild(GetComponent<MeshFilter>(), GetComponent<MeshRenderer>(), GetComponent<MeshCollider>(), new Mesh()));
            this.mat = mat;
            BuildFromTriangleArray(tris);
            BuildChunkEdges();
        }

       // protected Vector4[] firstPoint;

        protected List<BaseMeshChild> children = new List<BaseMeshChild>();

        protected void AddCurrentMeshDataChild()
        {
            GameObject g = new GameObject();
            g.transform.parent = transform;
            g.AddComponent<MeshFilter>();
        }

        public bool IsEmpty => triCount == 0;

        /// <summary>
        /// chunk is completly underground
        /// </summary>
        public bool IsCompletlySolid => IsEmpty && points[0].w >= surfaceLevel;

        /// <summary>
        /// chunk is completly air
        /// </summary>
        public bool IsCompletlyAir => IsEmpty && points[0].w < surfaceLevel;

        public IMarchingCubeChunkHandler chunkHandler;

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

        public int VertexSize => MarchingCubeChunkHandler.ChunkSize + 1;

        public MarchingCubeChunk GetChunk => this;

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

                tri.c = InterpolateVerts(cubeCorners[a0], cubeCorners[b0]);
                tri.b = InterpolateVerts(cubeCorners[a1], cubeCorners[b1]);
                tri.a = InterpolateVerts(cubeCorners[a2], cubeCorners[b2]);
                e.triangles.Add(new PathTriangle(this, tri));
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

            foreach (MarchingCubeEntity e in cubeEntities.Values)
            {
                e.BuildInternNeighbours();
                if ((e.origin.x + e.origin.y + e.origin.z) % 2 == 0 || IsBorderPoint(e.origin))
                {
                    List<MissingNeighbourData> trisWithNeighboursOutOfBounds
                                   = e.BuildNeighbours(GetEntityAt, IsInBounds);

                    if (trisWithNeighboursOutOfBounds != null)
                    {
                        foreach (MissingNeighbourData t in trisWithNeighboursOutOfBounds)
                        {
                            //Vector3Int offset = t.neighbour.offset.Map(Math.Sign);
                            Vector3Int target = chunkOffset + t.neighbour.offset;
                            MarchingCubeChunk c;
                            AddNeighbourFromEntity(target, e);
                            if (chunkHandler.Chunks.TryGetValue(target, out c))
                            {
                                Vector3Int pos = (e.origin + t.neighbour.offset).Map(i => i.FloorMod(MarchingCubeChunkHandler.ChunkSize));
                                MarchingCubeEntity cube = c.GetEntityAt(pos);
                                e.BuildSpecificNeighbourInNeighbour(cube, e.triangles[t.neighbour.triangleIndex], t.neighbour.rotatedEdgePair);
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


        protected void ResetMesh()
        {
            triCount = 0;
        }

        protected Material mat;


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
            ApplyChanges();
        }

        protected MarchingCubeEntity GetOrAddEntityAt(int x, int y, int z)
        {
            int key = IndexFromCoord(x, y, z);
            MarchingCubeEntity result;
            if (!cubeEntities.TryGetValue(key, out result))
            {
                result = new MarchingCubeEntity();
                result.origin = new Vector3Int(x, y, z);
                cubeEntities[key] = result;
            }
            return result;
        }

        protected Color triColor = new Color(0f, 0.5471698f, 0.1f, 1);

        public void BuildFromTriangleArray(TriangleBuilder[] ts)
        {
            triCount = ts.Length * 3;
            trisLeft = triCount;

            ResetArrayData();

            int usedTriCount = 0;

            MarchingCubeEntity cube;
            int chunksize = MarchingCubeChunkHandler.ChunkSize;
            cubeEntities = new Dictionary<int, MarchingCubeEntity>(chunksize * chunksize * chunksize / 15);
            foreach (TriangleBuilder t in ts)
            {
                cube = GetOrAddEntityAt(t.origin.x, t.origin.y, t.origin.z);
                cube.triangulationIndex = (short)t.triangulationIndex;
                cube.triangles.Add(new PathTriangle(this, t.tri));
                for (int i = 0; i < 3; i++)
                {
                    meshTriangles[usedTriCount + i] = usedTriCount + i;
                    vertices[usedTriCount + i] = t.tri[i];
                    colorData[usedTriCount + i] = triColor;
                }
                usedTriCount += 3;
                if (usedTriCount >= MAX_TRIANGLES_PER_MESH || usedTriCount >= trisLeft)
                {
                    ApplyChangesToMesh();
                    usedTriCount = 0;
                }
            }

        }


        protected void ApplyChanges()
        {
            Vector3[] vertices = new Vector3[triCount];
            int[] meshTriangles = new int[triCount];
            colorData = new Color[triCount];

            int count = 0;

            foreach (MarchingCubeEntity e in cubeEntities.Values)
            {
                foreach (PathTriangle t in e.triangles)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        meshTriangles[count + i] = count + i;
                        vertices[count + i] = t.tri[i];
                        colorData[count + i] = triColor;
                    }
                    count+=3;
                }
            }

            ApplyChangesToMesh();
        }

        public BaseMeshChild GetNextMeshDisplayer()
        {
            if (children[children.Count - 1].IsAppliedMesh)
            {
                BaseMeshChild result = new BaseMeshChild(this, transform);
                children.Add(result);
                return result;
            }
            else
            {
                return children[children.Count - 1];
            }
        }

        protected const int MAX_TRIANGLES_PER_MESH = 65000;

        protected void ApplyChangesToMesh()
        {
            BaseMeshChild displayer = GetNextMeshDisplayer();
            displayer.ApplyMesh(colorData, vertices, meshTriangles,  mat);
            trisLeft -= meshTriangles.Length;
            if(trisLeft > 0)
            {
                ResetArrayData();
            }
        }

        protected void ResetArrayData()
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
                Vector4 newV4 = points[i];
                newV4.w += delta;
                points[i] = newV4;
            }

            for (int i = 0; i < points.Length; i++)
            {
                Vector4 newV4 = points[i];
                newV4.w += delta;
                points[i] = newV4;
            }

            if (IsBorderPoint(e.origin))
            {
                chunkHandler.EditNeighbourChunksAt(this, e.origin, delta);
            }
            RebuildAround(e);
        }


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
            return transform.position + chunkOffset * MarchingCubeChunkHandler.ChunkSize;
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
                        int indexOffset = Mathf.CeilToInt((indexPoint[i] / (MarchingCubeChunkHandler.ChunkSize - 2f)) - 1);
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