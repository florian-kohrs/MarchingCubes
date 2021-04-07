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

        public virtual void InitializeWithMeshDataParallel(TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel, Action OnDone)
        {
            Debug.LogWarning("This class does not support concurrency! Use " + nameof(MarchingCubeChunkThreaded) + "instead!");
            InitializeWithMeshData(tris, activeTris, points, handler, neighbourLODs, surfaceLevel);
        }

        public void InitializeWithMeshData(TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel)
        {
            children.Add(new BaseMeshChild(GetComponent<MeshFilter>(), GetComponent<MeshRenderer>(), GetComponent<MeshCollider>(), new Mesh()));
            BuildMeshData(tris, activeTris, points, handler, neighbourLODs, surfaceLevel);
        }

        protected void BuildMeshData(TriangleBuilder[] tris, int activeTris, float[] points, IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel)
        {
            HasStarted = true;
            this.surfaceLevel = surfaceLevel;
            this.neighbourLODs = neighbourLODs;
            careAboutNeighbourLODS = neighbourLODs.AtLestOnHigerThan(lod);
            chunkHandler = handler;
            this.points = points;
            //Build();
            BuildMarchAll();
            BuildFromTriangleArray(cubeEntities2.ToArray(), activeTris);
            //if (lod == 1)
            //{
            //    BuildChunkEdges();
            //}
            //else
            //{
            //    FindConnectedChunks();
            //}
            IsReady = true;
        }

        protected MarchingCubeChunkNeighbourLODs neighbourLODs;

        // protected Vector4[] firstPoint;

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
        public List<TriangleBuilder> cubeEntities2 = new List<TriangleBuilder>();

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

        protected Vector4 BuildVector4FromCoord(int x, int y, int z)
        {
            x *= lod;
            y *= lod;
            z *= lod;
            return new Vector4(AnchorPos.x + x, AnchorPos.y + y, AnchorPos.z + z, points[IndexFromCoord(x, y, z)]);
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
                BuildVector4FromCoord(x + 1, y + 1, z + 1),
                BuildVector4FromCoord(x, y + 1, z + 1)
            };
        }

        protected Vector4[] GetCubeCornersForPoint(Vector3Int p)
        {
            return GetCubeCornersForPoint(p.x, p.y, p.z);
        }

        protected Vector4[] GetCubeCornersForPointWithLod(Vector3Int p, int spacing)
        {
            return new Vector4[]
            {
                BuildVector4FromCoord(p.x, p.y, p.z),
                BuildVector4FromCoord(p.x + spacing, p.y, p.z),
                BuildVector4FromCoord(p.x + spacing, p.y, p.z + spacing),
                BuildVector4FromCoord(p.x, p.y, p.z + spacing),
                BuildVector4FromCoord(p.x, p.y + spacing, p.z),
                BuildVector4FromCoord(p.x + spacing, p.y + spacing, p.z),
                BuildVector4FromCoord(p.x +spacing, p.y + spacing, p.z + spacing),
                BuildVector4FromCoord(p.x, p.y + spacing, p.z + spacing)
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
                triCount+=3;

            }

            if (e.triangles.Count > 0)
            {
                cubeEntities[IndexFromCoord(p)] = e;
            }
        }

        public void BuildMarchAll()
        {
            for (int x = 0; x < vertexSize; x++)
            {
                for (int y = 0; y < vertexSize; y++)
                {
                    for (int z = 0; z < vertexSize; z++)
                    {
                        MarchIntoBuilder2(x,y,z);
                    }
                }
            }
        }


        int indexFromCoord(int x, int y, int z)
        {
            return z * vertexSize * vertexSize + y * vertexSize + x;
        }

        Vector4 GetHeightDataFrom(int x, int y, int z)
        {
            Vector3 f3 = new Vector3(x, y, z);
            Vector4 pos = AnchorPos + f3 * lod;
            return new Vector4(pos.x, pos.y, pos.z, points[indexFromCoord(x, y, z)]);
        }

        Vector3 interpolateVerts(Vector4 v1, Vector4 v2)
        {
            //return v1.xyz + 0.5 * (v2.xyz - v1.xyz);
            float t = (surfaceLevel - v1.w) / (v2.w - v1.w);
            return new Vector3(v1.x,v1.y,v1.z) + (t) * (new Vector3(v2.x, v2.y, v2.z) - new Vector3(v1.x, v1.y, v1.z));
        }

        uint zipData(int x, int y, int z, int triIndex)
        {
            return (uint)((triIndex << 24) + (x << 16) + (y << 8) + z);
        }

        void MarchIntoBuilder(Vector3Int id)
        {

            // 8 corners of the current cube
            Vector4[] cubeCorners =
            {
        GetHeightDataFrom(id.x, id.y, id.z),
        GetHeightDataFrom(id.x + 1, id.y, id.z),
        GetHeightDataFrom(id.x + 1, id.y, id.z + 1),
        GetHeightDataFrom(id.x, id.y, id.z + 1),
        GetHeightDataFrom(id.x, id.y + 1, id.z),
        GetHeightDataFrom(id.x + 1, id.y + 1, id.z),
        GetHeightDataFrom(id.x + 1, id.y + 1, id.z + 1),
        GetHeightDataFrom(id.x, id.y + 1, id.z + 1)
            };

            // Calculate unique index for each cube configuration.
            // There are 256 possible values
            // A value of 0 means cube is entirely inside surface; 255 entirely outside.
            // The value is used to look up the edge table, which indicates which edges of the cube are cut by the isosurface.
            int cubeIndex = 0;

            if (cubeCorners[0].w < surfaceLevel) cubeIndex |= 1;
            if (cubeCorners[1].w < surfaceLevel) cubeIndex |= 2;
            if (cubeCorners[2].w < surfaceLevel) cubeIndex |= 4;
            if (cubeCorners[3].w < surfaceLevel) cubeIndex |= 8;
            if (cubeCorners[4].w < surfaceLevel) cubeIndex |= 16;
            if (cubeCorners[5].w < surfaceLevel) cubeIndex |= 32;
            if (cubeCorners[6].w < surfaceLevel) cubeIndex |= 64;
            if (cubeCorners[7].w < surfaceLevel) cubeIndex |= 128;

            // Create triangles for current cube configuration
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

                TriangleBuilder tri = new TriangleBuilder();
                tri.tri = new Triangle();
                tri.data = zipData(id.x, id.y, id.z, cubeIndex);
                tri.tri.a = interpolateVerts(cubeCorners[a0], cubeCorners[b0]);
                tri.tri.b = interpolateVerts(cubeCorners[a1], cubeCorners[b1]);
                tri.tri.c = interpolateVerts(cubeCorners[a2], cubeCorners[b2]);

                /*float3 normal = normalize(cross(tri.vertexB - tri.vertexA, tri.vertexC - tri.vertexA));
                float3 middlePoint = (tri.vertexA + tri.vertexB + tri.vertexC) / 3;
                float angleFromCenter = acos(dot(normal, normalize(middlePoint))) * 180 / PI;*/

                cubeEntities2.Add(tri);
            }
        }

        public virtual void MarchIntoBuilder2(int x, int y, int z)
        {
            Vector4[] cubeCorners = GetCubeCornersForPoint(x,y,z);

            short cubeIndex = 0;
            if (cubeCorners[0].w < surfaceLevel) cubeIndex |= 1;
            if (cubeCorners[1].w < surfaceLevel) cubeIndex |= 2;
            if (cubeCorners[2].w < surfaceLevel) cubeIndex |= 4;
            if (cubeCorners[3].w < surfaceLevel) cubeIndex |= 8;
            if (cubeCorners[4].w < surfaceLevel) cubeIndex |= 16;
            if (cubeCorners[5].w < surfaceLevel) cubeIndex |= 32;
            if (cubeCorners[6].w < surfaceLevel) cubeIndex |= 64;
            if (cubeCorners[7].w < surfaceLevel) cubeIndex |= 128;


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

                TriangleBuilder triBuild = new TriangleBuilder();
                triBuild.data = triBuild.zipData(x, y, z, cubeIndex);
                Triangle tri = new Triangle();
                tri.c = InterpolateVerts(cubeCorners[a0], cubeCorners[b0]);
                tri.b = InterpolateVerts(cubeCorners[a1], cubeCorners[b1]);
                tri.a = InterpolateVerts(cubeCorners[a2], cubeCorners[b2]);
                triBuild.tri = tri;
                cubeEntities2.Add(triBuild);
                triCount++;

            }
        }


        public virtual MarchingCubeEntity GetCubeMarchWithLod(Vector3Int p, int spacing)
        {

            MarchingCubeEntity e;
            //if (higherLodNeighbourCubes.TryGetValue(p, out e))
            //    return e;

            e = new MarchingCubeEntity();
            e.origin = p;
            Vector4[] cubeCorners = GetCubeCornersForPointWithLod(p, spacing);

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
            //higherLodNeighbourCubes.Add(p, e);
            return e;
        }


        public void FixConnectionsInDirection(Vector3Int v3)
        {

        }

        protected int DirectionToCornerIndex(Vector3Int v3)
        {
            if (v3.x != 0)
                return 1;
            else if (v3.y != 0)
                return 4;
            else
                return 3;
        }

        public Vector3 GetSimulatedTringlePointAtPositionWithEdge(Vector3Int direction, Vector3Int p)
        {
            return GetSimulatedTringlePointAtPositionWithEdge(DirectionToCornerIndex(direction), p);
        }

        public Vector3 GetSimulatedTringlePointAtPositionWithEdge(int cordnerIndex, Vector3Int p)
        {
            Vector3 result;

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

            for (int i = 0; TriangulationTable.triangulation[cubeIndex][i] != -1; i += 3)
            {
                // Get indices of corner points A and B for each of the three edges
                // of the cube that need to be joined to form the triangle.
                int a0 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i]];
                int b0 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i]];

                if (a0 == 0 && b0 == cordnerIndex)
                {
                    result = InterpolateVerts(cubeCorners[a0], cubeCorners[b0]);
                    return result;
                }

                int a1 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i + 1]];
                int b1 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i + 1]];
                if (a1 == 0 && b1 == cordnerIndex)
                {
                    result = InterpolateVerts(cubeCorners[a1], cubeCorners[b1]);
                    return result;
                }
                int a2 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i + 2]];
                int b2 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i + 2]];
                if (a2 == 0 && b2 == cordnerIndex)
                {
                    result = InterpolateVerts(cubeCorners[a2], cubeCorners[b2]);
                    return result;
                }

            }
            return -Vector3.one;
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
            foreach (MarchingCubeEntity e in cubeEntities.Values)
            {
                if (IsBorderCube(e.origin) && !e.FindMissingNeighbours(IsCubeInBounds, trisWithNeighboursOutOfBounds))
                {
                    for (int i = 0; i < trisWithNeighboursOutOfBounds.Count; i++)
                    {
                        t = trisWithNeighboursOutOfBounds[i];
                        Vector3Int target = chunkOffset + t.neighbour.offset;
                        AddNeighbourFromEntity(target, e);
                    }
                }
            }
        }


        protected void BuildChunkEdges()
        {
            if (IsEmpty)
                return;

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

        protected List<MissingNeighbourData> missingHigherLODNeighbour = new List<MissingNeighbourData>();

        protected void CorrectMarchingCubeInDirection(MarchingCubeEntity e, MissingNeighbourData missingData, int otherLod, Vector3Int dir)
        {
            int lodDiff = otherLod / lod;

            Vector3Int rightCubeIndex = e.origin.Map(f => f - f % lodDiff);
            MarchingCubeEntity originalTest = GetCubeMarchWithLod(e.origin, 1);
            MarchingCubeEntity reference = GetCubeMarchWithLod(rightCubeIndex, lodDiff);

            //int a0 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i]];
            //int b0 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i]];

            //int a1 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i + 1]];
            //int b1 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i + 1]];

            //Vector3Int firstPointCoord = TriangulationTableStaticData.offsetFromCornerIndex[missingData.neighbour.originalEdgePair.x];
            //Vector3Int sndPointCoord = TriangulationTableStaticData.offsetFromCornerIndex[missingData.neighbour.originalEdgePair.y];

            //Vector3Int firstCoord = e.origin + firstPointCoord;
            //Vector3Int sndCoord = e.origin + sndPointCoord;

            //Vector3Int firstNeighbourOffset = new Vector3Int(firstCoord.x % lodDiff, firstCoord.y % lodDiff, firstCoord.z % lodDiff);
            //Vector3Int sndNeighbourOffset = new Vector3Int(
            //    (lodDiff - 1) - (sndCoord.x % lodDiff),
            //    (lodDiff - 1) - (sndCoord.y % lodDiff),
            //    (lodDiff - 1) - (sndCoord.z % lodDiff));   

            PathTriangle t = e.triangles[missingData.neighbour.triangleIndex];
            

            Vector3Int firstNeighbourOffset = new Vector3Int(e.origin.x % lodDiff, e.origin.y % lodDiff, e.origin.z % lodDiff);

            if(dir.x != 0)
            {
                firstNeighbourOffset.x = 0;
            }
            else if(dir.y != 0)
            {
                firstNeighbourOffset.y = 0;
            }
            else if(dir.z != 0)
            {
                firstNeighbourOffset.z = 0;
            }

            if(firstNeighbourOffset == Vector3Int.zero)
            {

            }

            Vector3Int sndNeighbourOffset = new Vector3Int(
                (lodDiff - 1) - (e.origin.x % lodDiff),
                (lodDiff - 1) - (e.origin.y % lodDiff),
                (lodDiff - 1) - (e.origin.z % lodDiff));


            PathTriangle fixThis = e.triangles[missingData.neighbour.triangleIndex];

            MarchingCubeEntity importantFirstNeighbourCube = GetEntityAt(e.origin - firstNeighbourOffset);
            MarchingCubeEntity importantSndNeighbourCube = GetEntityAt(e.origin + sndNeighbourOffset);


            if (dir.x != 0)
            {
                if (dir.x < 0)
                {
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



        protected void Build()
        {
            Vector3Int v = new Vector3Int();

            for (int x = 0; x < vertexSize; x++)
            {
                v.x = x;
                for (int y = 0; y < vertexSize; y++)
                {
                    v.y = y;
                    for (int z = 0; z < vertexSize; z++)
                    {
                        v.z = z;
                        March(v);
                    }
                }
            }
            ApplyChanges();
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
            cubeEntities = new Dictionary<int, MarchingCubeEntity>(vertexSize * vertexSize * vertexSize / 15);
            foreach (TriangleBuilder t in ts)
            {
                if (totalTreeCount >= activeTris)
                {
                    if (usedTriCount > 0)
                    {
                        ApplyChangesToMesh();
                    }
                    break;
                }
                if (!GetOrAddEntityAt(t.Origin, out cube))
                {
                    cube.triangulationIndex = t.TriIndex;
                }
                PathTriangle pathTri = new PathTriangle(t.tri);
                cube.triangles.Add(pathTri);
                for (int i = 0; i < 3; i++)
                {
                    meshTriangles[usedTriCount + i] = usedTriCount + i;
                    vertices[usedTriCount + i] = t.tri[i];
                    colorData[usedTriCount + i] = GetColor(pathTri, i);
                }
                usedTriCount += 3;
                totalTreeCount++;
                if (usedTriCount >= MAX_TRIANGLES_PER_MESH || usedTriCount >= trisLeft)
                {
                    ApplyChangesToMesh();
                    usedTriCount = 0;
                }
            }
        }

        protected static Color brown = new Color(75, 44, 13, 1) / 255f;

        protected Color GetColor(PathTriangle t, int i)
        {
            float slopeProgress = Mathf.InverseLerp(15, 45, t.Slope);
            return (Color.green * (1 - slopeProgress) + brown * slopeProgress) / 2;
        }

        protected void ApplyChanges()
        {
            vertices = new Vector3[triCount];
            meshTriangles = new int[triCount];
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
                        colorData[count + i] = Color.yellow;
                    }
                    count += 3;
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


        protected virtual void SetCurrentMeshData()
        {
            BaseMeshChild displayer = GetNextMeshDisplayer();
            displayer.ApplyMesh(colorData, vertices, meshTriangles, Material);
        }

        protected void ApplyChangesToMesh()
        {
            SetCurrentMeshData();
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