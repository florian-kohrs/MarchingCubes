using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MarchingCubeChunk : MonoBehaviour, IMarchingCubeChunk, IMarchingCubeInteractableChunk, IHasMarchingCubeChunk
    {

        public virtual void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel, Action OnDone)
        {
            Debug.LogWarning("This class does not support concurrency! Use " + nameof(MarchingCubeChunkThreaded) + "instead!");
            InitializeWithMeshData(tris, points, handler, neighbourLODs, surfaceLevel);
        }

        public void InitializeWithMeshData(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel)
        {
            children.Add(new BaseMeshChild(GetComponent<MeshFilter>(), GetComponent<MeshRenderer>(), GetComponent<MeshCollider>(), new Mesh()));
            BuildMeshData(tris, points, handler, neighbourLODs, surfaceLevel);
        }

        protected void BuildMeshData(TriangleBuilder[] tris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel)
        {

            HasStarted = true;
            this.surfaceLevel = surfaceLevel;
            this.neighbourLODs = neighbourLODs;
            triCount = tris.Length * 3;
            careAboutNeighbourLODS = neighbourLODs.AtLestOnHigerThan(lod);
            chunkHandler = handler;
            this.points = points;

            BuildFromTriangleArray(tris);
            //BuildAll(2/lod);
            if (lod == 1)
            {
                BuildChunkEdges();
            }
            else
            {
                FindConnectedChunks();
            }

            //if (careAboutNeighbourLODS)
            //{
            //    BuildMeshFromCurrentTriangles();
            //}

            BuildMeshToConnectHigherLodChunks();

            IsReady = true;


        }

        protected MarchingCubeChunkNeighbourLODs neighbourLODs;

        protected List<BaseMeshChild> children = new List<BaseMeshChild>();

        public Material Material { protected get; set; }

        public bool IsReady { get; protected set; }

        protected const int MAX_TRIANGLES_PER_MESH = 65000;

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
        public bool IsCompletlySolid => IsEmpty && points[0] >= surfaceLevel;

        /// <summary>
        /// chunk is completly air
        /// </summary>
        public bool IsCompletlyAir => IsEmpty && points[0] < surfaceLevel;


        protected int vertexSize = MarchingCubeChunkHandler.ChunkSize;

        protected int PointSize => vertexSize + 1;

        public int lod = 1;

        public IMarchingCubeChunkHandler chunkHandler;

        public Dictionary<Vector3Int, HashSet<MarchingCubeEntity>> NeighboursReachableFrom = new Dictionary<Vector3Int, HashSet<MarchingCubeEntity>>();

        public IEnumerable<Vector3Int> NeighbourIndices => NeighboursReachableFrom.Keys;

        public int LOD
        {
            get
            {
                return lod;
            }
            set
            {
                lod = value;
                vertexSize = MarchingCubeChunkHandler.ChunkSize / lod;
            }
        }

        protected float[] points;

        public Vector3Int chunkOffset;

        public IMarchingCubeInteractableChunk GetChunk => this;

        public Vector3Int ChunkOffset { get => chunkOffset; set => chunkOffset = value; }

        public int NeighbourCount => NeighboursReachableFrom.Count;

        public bool HasStarted { get; protected set; }
        public float Spacing { get; set; }

        protected float surfaceLevel;

        protected Color[] colorData;
        protected Vector3[] vertices;
        protected int[] meshTriangles;


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

        protected bool careAboutNeighbourLODS;

        protected int triCount;

        protected int trisLeft;

        public Dictionary<int, MarchingCubeEntity> cubeEntities = new Dictionary<int, MarchingCubeEntity>();

        protected Dictionary<Vector3Int, MarchingCubeEntity> higherLodNeighbourCubes = new Dictionary<Vector3Int, MarchingCubeEntity>();

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


        protected NoiseFilter noiseFilter;


        protected Vector4 BuildVector4(Vector3 v3, float w)
        {
            return new Vector4(v3.x, v3.y, v3.z, w);
        }

        public Vector3 AnchorPos { get; set; }

        protected Vector4 BuildVector4FromCoord(int x, int y, int z, int lod)
        {
            int globalLod = lod * this.lod;
            //x *= lod;
            //y *= lod;
            //z *= lod;
            return new Vector4(AnchorPos.x + x * globalLod, AnchorPos.y + y * globalLod, AnchorPos.z + z * globalLod, points[IndexFromCoord(x, y, z)]);
        }

        protected Vector4 BuildVector4FromCoord(int x, int y, int z)
        {
            return new Vector4(AnchorPos.x + x * lod, AnchorPos.y + y * lod, AnchorPos.z + z * lod, points[IndexFromCoord(x, y, z)]);
        }



        protected Vector4[] GetCubeCornersForPoint(int x, int y, int z, int spacing)
        {
            return GetCubeCornersForPointWithLod(x, y, z, spacing);
        }

        protected Vector4[] GetCubeCornersForPoint(Vector3Int p)
        {
            return GetCubeCornersForPoint(p.x, p.y, p.z);
        }

        protected Vector4[] GetCubeCornersForPointWithLod(Vector3Int p, int spacing)
        {
            return GetCubeCornersForPointWithLod(p.x, p.y, p.z, spacing);
        }

        protected Vector4[] GetCubeCornersForPoint(int x, int y, int z)
        {
            return new Vector4[]
            {
                BuildVector4FromCoord(x, y, z),
                BuildVector4FromCoord(x + 1, y, z),
                BuildVector4FromCoord(x + 1, y, z + 1),
                BuildVector4FromCoord(x, y, z + 1),
                BuildVector4FromCoord(x, y + 1, z),
                BuildVector4FromCoord(x + 1, y + 1, z),
                BuildVector4FromCoord(x +1, y + 1, z + 1),
                BuildVector4FromCoord(x, y + 1, z + 1)
            };
        }

        protected Vector4[] GetCubeCornersForPointWithLod(int x, int y, int z, int spacing)
        {
            return new Vector4[]
            {
                BuildVector4FromCoord(x, y, z),
                BuildVector4FromCoord(x + spacing, y, z),
                BuildVector4FromCoord(x + spacing, y, z + spacing),
                BuildVector4FromCoord(x, y, z + spacing),
                BuildVector4FromCoord(x, y + spacing, z),
                BuildVector4FromCoord(x + spacing, y + spacing, z),
                BuildVector4FromCoord(x + spacing, y + spacing, z + spacing),
                BuildVector4FromCoord(x, y + spacing, z + spacing)
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

        public virtual void March(Vector3Int p)
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
                cubeEntities[IndexFromCoord(p)] = e;
            }
        }

        public virtual MarchingCubeEntity MarchAt(Vector3Int v3, int lod)
        {
            MarchingCubeEntity e = new MarchingCubeEntity();
            e.origin = v3;

            Vector4[] cubeCorners = GetCubeCornersForPoint(v3.x, v3.y, v3.z, lod);

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
            }
            return e;
        }

        public virtual MarchingCubeEntity MarchAt(int x, int y, int z, int lod)
        {
            MarchingCubeEntity e = new MarchingCubeEntity();
            e.origin = new Vector3Int(x, y, z);

            Vector4[] cubeCorners = GetCubeCornersForPoint(x, y, z, lod);

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

            uint data = TriangleBuilder.zipData(x, y, z, cubeIndex);

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
            }
            return e;
        }


        protected int ClampInChunk(int i)
        {
            return i.FloorMod(vertexSize);
        }

        protected void FindConnectedChunks()
        {
            if (IsEmpty)
                return;

            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            MissingNeighbourData t;
            IMarchingCubeChunk c;
            foreach (MarchingCubeEntity e in cubeEntities.Values)
            {
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
                                MarchingCubeEntity cube = c.GetEntityAt(pos);
                                CorrectMarchingCubeInDirection(e, t, c.LOD, t.neighbour.offset);
                            }
                        }
                        else if (careAboutNeighbourLODS)
                        {
                            int neighbourLod = neighbourLODs.GetLodFromNeighbourInDirection(t.neighbour.offset);
                            if (neighbourLod > lod)
                            {
                                CorrectMarchingCubeInDirection(e, t, neighbourLod, t.neighbour.offset);
                            }
                        }
                    }
                }
            }
        }


        protected void BuildChunkEdges()
        {
            if (IsEmpty)
                return;

            lodNeighbourPointCorrectionLookUp = new Dictionary<Vector3, Vector3>();

            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            MissingNeighbourData t;
            foreach (MarchingCubeEntity e in cubeEntities.Values)
            {
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
                            IMarchingCubeChunk c;
                            AddNeighbourFromEntity(target, e);
                            if (chunkHandler.TryGetReadyChunkAt(target, out c))
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
                                    CorrectMarchingCubeInDirection(e, t, c.LOD, t.neighbour.offset);
                                }
                            }
                            else if (careAboutNeighbourLODS)
                            {
                                int neighbourLod = neighbourLODs.GetLodFromNeighbourInDirection(t.neighbour.offset);
                                if (neighbourLod > lod)
                                {
                                    CorrectMarchingCubeInDirection(e, t, neighbourLod, t.neighbour.offset);
                                }
                            }
                        }
                        trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
                    }
                }
            }
        }

        protected Dictionary<int, MarchingCubeEntity> neighbourChunksGlue = new Dictionary<int, MarchingCubeEntity>();

        protected int glueTriangleCount = 0;

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

        protected void CorrectMarchingCubeInDirection(MarchingCubeEntity e, MissingNeighbourData missingData, int otherLod, Vector3Int dir)
        {
            //Debug.Log("entitiy with neighbour in higher lod chunk");
            ///maybe add corrected triangles to extra mesh to not recompute them when chunk changes and easier remove /swap them if neihghbour changes lod

            int lodDiff = otherLod / lod;

            Vector3Int rightCubeIndex = e.origin.Map(f => f - f % lodDiff);
            int key = IndexFromCoord(rightCubeIndex);
            if (!neighbourChunksGlue.ContainsKey(key))
            {
                Vector3Int diff = e.origin - rightCubeIndex;
                //MarchingCubeEntity original = MarchAt(e.origin, 1);
                MarchingCubeEntity bindWithNeighbour = MarchAt(rightCubeIndex, lodDiff);
                neighbourChunksGlue.Add(key, bindWithNeighbour);
                glueTriangleCount += bindWithNeighbour.triangles.Count * 3;
            }
            //e.triangles.ForEach(t => t. = reference.triangles;
            //try
            //{


            //    for (int i = 0; i < original.triangles.Count; i++)
            //    {

            //        for (int x = 0; x < 3; x++)
            //        {
            //            bool modified = false;
            //            Triangle triangle = e.triangles[i].tri;
            //            Vector3 v = original.triangles[i].tri[x];
            //            if (IsPointTouchingBorderInDirection(v, dir))
            //            {
            //                Debug.Log("Found Point to fix");
            //                int aIndex = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[original.triangulationIndex][i * 3 + x]];
            //                int bIndex = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[original.triangulationIndex][i * 3 + x]];
            //                //int rotatedA = TriangulationTableStaticData.RotateCornerIndex(aIndex, dir);
            //                //int rotatedB = TriangulationTableStaticData.RotateCornerIndex(aIndex, dir);
            //                triangle = e.triangles[i].tri;
            //                Vector3 triPos;
            //                if (GetPointWithCorner(reference, aIndex, bIndex, out triPos))
            //                {
            //                    triangle[x] = triPos;
            //                    modified = true;
            //                }
            //                else
            //                {
            //                    Debug.LogWarning("Didnt found point with corners!");
            //                }
            //            }
            //            if (modified)
            //            {
            //                e.triangles[i] = new PathTriangle(triangle);
            //            }
            //        }
            //    }
            //}
            //catch (Exception x)
            //{

            //    MarchingCubeEntity reference = MarchAt(rightCubeIndex, lodDiff);
            //}
        }

        protected void BuildMeshToConnectHigherLodChunks()
        {
            trisLeft = glueTriangleCount;


            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            foreach (MarchingCubeEntity t in neighbourChunksGlue.Values)
            {
                for (int i = 0; i < t.triangles.Count; i++)
                {
                    AddTriangleToMeshData(t.triangles[i], ref usedTriCount, ref totalTreeCount, false);
                }
            }
        }

        protected virtual Vector3 InterpolateVerts(Vector4 v1, Vector4 v2)
        {
            Vector3 v = v1.GetXYZ();
            float t = (surfaceLevel - v1.w) / (v2.w - v1.w);
            return v + t * (v2.GetXYZ() - v);
        }



        protected virtual Vector3 InterpolatePositions(Vector3 v1, Vector3 v2, float p)
        {
            return v1 + p * (v2 - v1);
        }

        protected Vector3Int CoordFromCubeIndex(int i)
        {
            return new Vector3Int
               ((i % (vertexSize * vertexSize) % vertexSize)
               , (i % (vertexSize * vertexSize) / vertexSize)
               , (i / (vertexSize * vertexSize))
               );
        }

        protected int IndexFromCoord(int x, int y, int z)
        {
            int index = z * PointSize * PointSize + y * PointSize + x;
            return index;
        }

        protected int IndexFromCoord(Vector3Int v)
        {
            return IndexFromCoord(v.x, v.y, v.z);
        }


        protected void ResetMesh()
        {
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
                        MarchingCubeEntity e = MarchAt(v, localLod);
                        if (e.triangles.Count > 0)
                        {
                            cubeEntities[IndexFromCoord(x, y, z)] = e;
                            triCount += e.triangles.Count * 3;
                        }
                        //March(v);
                    }
                }
            }
            // BuildMeshFromCurrentTriangles();
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

        public void BuildFromTriangleArray(TriangleBuilder[] ts, bool buildMeshAswell = true)
        {
            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            MarchingCubeEntity cube;
            cubeEntities = new Dictionary<int, MarchingCubeEntity>(vertexSize * vertexSize * vertexSize / 15);
            foreach (TriangleBuilder t in ts)
            {
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

            foreach (MarchingCubeEntity t in cubeEntities.Values)
            {
                for (int i = 0; i < t.triangles.Count; i++)
                {
                    AddTriangleToMeshData(t.triangles[i], ref usedTriCount, ref totalTreeCount, false);
                }
            }
        }

        protected void AddTriangleToMeshData(PathTriangle tri, ref int usedTriCount, ref int totalTriCount, bool useCollider = true)
        {
            for (int x = 0; x < 3; x++)
            {
                meshTriangles[usedTriCount + x] = usedTriCount + x;
                vertices[usedTriCount + x] = tri.tri[x];
                colorData[usedTriCount + x] = GetColor(tri);
            }
            usedTriCount += 3;
            totalTriCount++;
            if (usedTriCount >= MAX_TRIANGLES_PER_MESH || usedTriCount >= trisLeft)
            {
                ApplyChangesToMesh(useCollider);
                usedTriCount = 0;
            }
        }

        protected static Color brown = new Color(75, 44, 13, 1) / 255f;

        protected Color GetColor(PathTriangle t)
        {
            float slopeProgress = Mathf.InverseLerp(15, 45, t.Slope);
            return (Color.green * (1 - slopeProgress) + brown * slopeProgress) / 2;
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


        protected virtual void SetCurrentMeshData(bool useCollider)
        {
            BaseMeshChild displayer = GetNextMeshDisplayer();
            displayer.ApplyMesh(colorData, vertices, meshTriangles, Material, useCollider);
        }

        protected void ApplyChangesToMesh(bool useCollider)
        {
            SetCurrentMeshData(useCollider);
            trisLeft -= meshTriangles.Length;
            if (trisLeft > 0)
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
            BuildAll();
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
                        if (IsCubeInBounds(v))
                        {
                            March(v);
                            ///inform neighbours about eventuell change!
                        }
                    }
                }
            }
            // ApplyChanges();
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
                if (IsCubeInBounds(v3))
                {
                    yield return v3;
                }
            }
        }

        protected bool IsCubeInBounds(Vector3Int v)
        {
            return
                v.x >= 0 && v.x < vertexSize
                && v.y >= 0 && v.y < vertexSize
                && v.z >= 0 && v.z < vertexSize;
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

            if (IsBorderCube(e.origin))
            {
                chunkHandler.EditNeighbourChunksAt(chunkOffset, e.origin, delta);
            }
            RebuildAround(e);
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

        public void EditPointsNextToChunk(IMarchingCubeChunk chunk, MarchingCubeEntity e, Vector3Int offset, float delta)
        {
            int[] cornerIndices = GetCubeCornerIndicesForPoint(e.origin);

            foreach (int index in cornerIndices)
            {
                Vector3Int indexPoint = CoordFromCubeIndex(index);
                Vector3Int pointOffset = new Vector3Int();
                for (int i = 0; i < 3; i++)
                {
                    if (offset[i] == 0)
                    {
                        pointOffset[i] = 0;
                    }
                    else
                    {
                        int indexOffset = Mathf.CeilToInt((indexPoint[i] / (vertexSize - 2f)) - 1);
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

        public void SetActive(bool b)
        {
            gameObject.SetActive(b);
        }

    }
}