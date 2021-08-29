using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class CompressedMarchingCubeChunk : IMarchingCubeChunk
    {

        public virtual void InitializeWithMeshDataParallel(TriangleBuilder[] tris, float[] points, MarchingCubeChunkNeighbourLODs neighbourLod, Action OnDone = null)
        {
            throw new Exception("This class doesnt support concurrency");
        }

        public virtual void InitializeWithMeshData(TriangleBuilder[] tris, float[] points, MarchingCubeChunkNeighbourLODs neighbourLod)
        {
            BuildChunkFromMeshData(tris, points, neighbourLODs);
        }

        //public virtual void InitializeEmpty(IMarchingCubeChunkHandler handler, MarchingCubeChunkNeighbourLODs neighbourLODs, float surfaceLevel)
        //{
        //    throw new NotImplementedException();
        //}

        public void ResetChunk()
        {
            FreeAllMeshes();
            OnResetChunk();
            points = null;
        }

        protected virtual void OnResetChunk() { }

        public bool IsReady { get; protected set; }

        public bool HasStarted { get; protected set; }

        public int SizeGrower
        {
            get
            {
                return sizeGrower;
            }
            set
            {
                sizeGrower = value;
                UpdateChunkData();
            }
        }

        protected int sizeGrower = 1;

        protected float surfaceLevel;

        protected const int MAX_TRIANGLES_PER_MESH = 65000;

        protected MarchingCubeChunkNeighbourLODs neighbourLODs;

        protected bool careAboutNeighbourLODS;

        protected List<BaseMeshDisplayer> activeDisplayers = new List<BaseMeshDisplayer>();

        protected int lod = 1;

        public int LOD
        {
            get
            {
                return lod;
            }
            set
            {
                lod = value;
                vertexSize = chunkSize / lod * SizeGrower;
                pointsPerAxis = vertexSize + 1;
            }
        }

        protected int lodPower;

        public int LODPower
        {
            get
            {
                return lodPower;
            }
            set
            {
                lodPower = value;
                LOD = (int)Mathf.Pow(2, lodPower);
            }
        }

        protected int GetLODPowerFromLOD(int lod) => (int)Mathf.Log(lod, 2);

        protected float[] points;

        public float[] Points => points;

        protected int triCount;

        protected int connectorTriangleCount = 0;

        protected int trisLeft;

        protected int chunkSize;

        protected int vertexSize;

        protected int pointsPerAxis;

        public int NeighbourCount => NeighboursReachableFrom.Count;


        public Material Material { protected get; set; }


        protected int PointsPerAxis => pointsPerAxis;


        //protected Vector3Int chunkOffset;

        protected Vector3Int chunkCenterPosition;

        public Vector3Int CenterPos => chunkCenterPosition;

        public IMarchingCubeChunkHandler chunkHandler;

        public IMarchingCubeChunkHandler ChunkHandler
        {
            set
            {
                chunkHandler = value;
            }
        }

        public IMarchingCubeChunkHandler GetChunkHandler => chunkHandler;

        public Dictionary<Vector3Int, HashSet<MarchingCubeEntity>> NeighboursReachableFrom = new Dictionary<Vector3Int, HashSet<MarchingCubeEntity>>(new Vector3EqualityComparer());

        public List<Vector3Int> NeighbourIndices
        {
            get
            {
                List<Vector3Int> result = new List<Vector3Int>();
                for (int i = 0; i < HasNeighbourInDirection.Length; i++)
                {
                    if (HasNeighbourInDirection[i] != null)
                        result.Add(HasNeighbourInDirection[i].Value);
                }
                return result;
            } 
        }


        public Vector3Int?[] HasNeighbourInDirection { get; private set;} = new Vector3Int?[6];

        /// <summary>
        /// stores chunkEntities based on their position as index
        /// </summary>
        protected Dictionary<int, MarchingCubeEntity> neighbourChunksGlue = new Dictionary<int, MarchingCubeEntity>();

        /// <summary>
        /// stores for each direction of the neighbour all cubes
        /// </summary>
        protected Dictionary<int, List<MarchingCubeEntity>> cubesForNeighbourInDirection = new Dictionary<int, List<MarchingCubeEntity>>();

        //protected List<BaseMeshChild> children = new List<BaseMeshChild>();
        protected Vector3[] vertices;
        protected int[] meshTriangles;
        protected Color[] colorData;

        public bool IsEmpty => triCount == 0;

        /// <summary>
        /// chunk is completly underground
        /// </summary>
        public bool IsCompletlySolid => IsEmpty && points[0] >= surfaceLevel;

        /// <summary>
        /// chunk is completly air
        /// </summary>
        public bool IsCompletlyAir => IsEmpty && points[0] < surfaceLevel;


        public Vector3Int AnchorPos
        {
            get
            {
                return anchorPos;
            }
            set
            {
                anchorPos = value;
                UpdateChunkCenterPos();
            }
        }

        private void UpdateChunkCenterPos()
        {
            int halfSize = ChunkSize / 2;
            chunkCenterPosition = new Vector3Int(
                anchorPos.x + halfSize,
                anchorPos.y + halfSize,
                anchorPos.z + halfSize);
        }

        private void UpdateChunkData()
        {
            vertexSize = chunkSize / lod * sizeGrower;
            pointsPerAxis = vertexSize + 1;
        }

        private Vector3Int anchorPos;

        int IMarchingCubeChunk.PointsPerAxis => pointsPerAxis;

        public int ChunkSize { get => chunkSize; 
            set { chunkSize = value; UpdateChunkCenterPos(); UpdateChunkData(); } }

        public float SurfaceLevel { set => surfaceLevel = value; }

        protected Vector3Int GetGlobalEstimatedNeighbourPositionFromOffset(Vector3Int offset)
        {
            return CenterPos + offset * chunkSize;
            //Vector3Int center = CenterPos;
            //return new Vector3Int(
            //    center.x + offset.x * chunkSize,
            //    center.y + offset.y * chunkSize, 
            //    center.z + offset.z * chunkSize);
        }

        protected Vector3Int GetGlobalNeighbourBorderPositionFromOffset(Vector3Int offset)
        {
            return CenterPos + offset * chunkSize / 2;
        }

        protected virtual void BuildChunkFromMeshData(TriangleBuilder[] tris, float[] points, MarchingCubeChunkNeighbourLODs neighbourLODs)
        {
            HasStarted = true;
            this.points = points;
            this.neighbourLODs = neighbourLODs;
            triCount = tris.Length * 3;
            careAboutNeighbourLODS = neighbourLODs.HasNeighbourWithHigherLOD(LODPower);
            BuildFromTriangleArray(tris);

            WorkOnBuildedChunk();

            IsReady = true;
        }

        protected virtual void WorkOnBuildedChunk()
        {
            BuildMeshToConnectHigherLodChunks();
        }


        protected Vector3Int TransformBorderPointToChunk(Vector3Int v3, Vector3Int dir, IMarchingCubeChunk neighbour)
        {
            Vector3Int result = FlipBorderCoordinateToNeighbourChunk(v3, dir, neighbour);

            float sizeDiff = neighbour.ChunkSize / (float)ChunkSize;

            Vector3Int transformedAnchorPosition;

            if (IsDirectionOutOfChunk(dir))
            {
                transformedAnchorPosition = AnchorPos + neighbour.ChunkSize * dir;
            }
            else
            {
                transformedAnchorPosition = AnchorPos + ChunkSize * dir;
            }


            Vector3Int anchorDiff = transformedAnchorPosition - neighbour.AnchorPos;

            result = result + anchorDiff;

            return result;
        }

        protected bool IsDirectionOutOfChunk(Vector3Int v3)
        {
            return v3.x < 0 || v3.y < 0 || v3.z < 0;
        }

        protected Vector3Int FlipBorderCoordinateToNeighbourChunk(Vector3Int v3, Vector3Int dir, IMarchingCubeChunk neighbour)
        {
            Vector3Int result = v3;
            if (dir.x < 0)
                result.x = neighbour.ChunkSize - 1;
            else if (dir.x > 0)
                result.x = 0;
            else if (dir.y < 0)
                result.y = neighbour.ChunkSize - 1;
            else if (dir.y > 0)
                result.y = 0;
            else if (dir.z < 0)
                result.z = neighbour.ChunkSize - 1;
            else if (dir.z > 0)
                result.z = 0;
            return result;
        }

        protected virtual void BuildFromTriangleArray(TriangleBuilder[] ts, bool buildMeshAswell = true)
        {
            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            TriangleBuilder t;
            for (int i = 0; i < ts.Length; i++)
            {
                t = ts[i];
                Vector3Int currentOrigin = t.Origin;
                if (!NeighboursReachableFrom.ContainsKey(currentOrigin))
                {
                    MarchingCubeEntity.FindMissingNeighboursAt(t.TriIndex, currentOrigin, IsCubeInBounds, trisWithNeighboursOutOfBounds);
                }

                if (buildMeshAswell)
                {
                    AddTriangleToMeshData(t.tri, ref usedTriCount, ref totalTreeCount);
                }
            }

            MissingNeighbourData neighbour;
            IMarchingCubeChunk c;

            for (int i = 0; i < trisWithNeighboursOutOfBounds.Count; i++)
            {
                neighbour = trisWithNeighboursOutOfBounds[i];
                Vector3Int target = GetGlobalEstimatedNeighbourPositionFromOffset(neighbour.outsideNeighbour.offset);
                Vector3Int border = neighbour.originCubeEntity + neighbour.outsideNeighbour.offset;

                AddNeighbourFromEntity(neighbour.outsideNeighbour.offset, target, null);

                if (chunkHandler.TryGetReadyChunkAt(target, out c))
                {
                    if (c.LODPower > LODPower)
                    {
                        Vector3Int pos = TransformBorderPointToChunk(border, neighbour.outsideNeighbour.offset, c);

                        CorrectMarchingCubeInDirection(neighbour.originCubeEntity, neighbour, c.LODPower);
                    }
                }
                else if (careAboutNeighbourLODS)
                {
                    int neighbourLodPower = neighbourLODs.GetLodPowerFromNeighbourInDirection(neighbour.outsideNeighbour.offset);
                    if (neighbourLodPower > LODPower)
                    {
                        CorrectMarchingCubeInDirection(neighbour.originCubeEntity, neighbour, neighbourLodPower);
                    }
                }
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
            return MarchAt(new Vector3Int(x, y, z), lod);
        }

        protected virtual Vector3 InterpolateVerts(Vector4 v1, Vector4 v2)
        {
            Vector3 v = v1.GetXYZ();
            float t = (surfaceLevel - v1.w) / (v2.w - v1.w);
            return v + t * (v2.GetXYZ() - v);
        }

        protected void CorrectMarchingCubeInDirection(Vector3Int origin, MissingNeighbourData missingData, IMarchingCubeChunk c)
        {
            CorrectMarchingCubeInDirection(origin, missingData, c.LODPower);
        }

        protected void CorrectMarchingCubeInDirection(Vector3Int origin, MissingNeighbourData missingData, int otherLodPower)
        {
            //Debug.Log("entitiy with neighbour in higher lod chunk");
            ///maybe add corrected triangles to extra mesh to not recompute them when chunk changes and easier remove /swap them if neighbour changes lod


            int lodDiff = (int)Mathf.Pow(2, otherLodPower - lodPower);

            Vector3Int rightCubeIndex = origin.Map(f => f - f % lodDiff);
            int key = PointIndexFromCoord(rightCubeIndex);
            key = (key << MarchingCubeChunkHandler.MAX_CHUNK_LOD_BIT_REPRESENTATION_SIZE) + otherLodPower;
            if (!neighbourChunksGlue.ContainsKey(key))
            {
                //MarchingCubeEntity original = MarchAt(e.origin, 1);
                MarchingCubeEntity bindWithNeighbour = MarchAt(rightCubeIndex, lodDiff);
                neighbourChunksGlue.Add(key, bindWithNeighbour);
                AddCubeForNeigbhourInDirection(VectorExtension.GetIndexFromDirection(missingData.outsideNeighbour.offset), bindWithNeighbour);
                connectorTriangleCount += bindWithNeighbour.triangles.Count * 3;
            }
        }

        public void NeighbourReplacedAtWithLodPower(int lodPower)
        {

        }

        protected void AddTriangleToMeshData(PathTriangle tri, ref int usedTriCount, ref int totalTriCount, bool isBorderConnectionMesh = false)
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
                ApplyChangesToMesh(isBorderConnectionMesh);
                usedTriCount = 0;
            }
        }

        protected void AddTriangleToMeshData(Triangle tri, ref int usedTriCount, ref int totalTriCount, bool useCollider = true)
        {
           Vector3 normal = (Vector3.Cross(tri.b - tri.a, tri.c - tri.a)).normalized;
           Vector3 middlePoint = (tri.a + tri.b + tri.c) / 3;
           float slope = Mathf.Acos(Vector3.Dot(normal, middlePoint.normalized)) * 180 / Mathf.PI;

            for (int x = 0; x < 3; x++)
            {
                meshTriangles[usedTriCount + x] = usedTriCount + x;
                vertices[usedTriCount + x] = tri[x];
                colorData[usedTriCount + x] = GetColor(normal,middlePoint,slope);
            }
            usedTriCount += 3;
            totalTriCount++;
            if (usedTriCount >= MAX_TRIANGLES_PER_MESH || usedTriCount >= trisLeft)
            {
                ApplyChangesToMesh(useCollider);
                usedTriCount = 0;
            }
        }

        protected BaseMeshDisplayer GetMeshDisplayer()
        {
            BaseMeshDisplayer d = chunkHandler.GetNextMeshDisplayer();
            activeDisplayers.Add(d);
            return d;
        }

        protected BaseMeshDisplayer GetBestMeshDisplayer()
        {
            if(this is IMarchingCubeInteractableChunk i)
            {
                return GetMeshInteractableDisplayer(i);
            }
            else
            {
                return GetMeshDisplayer();
            }
        }

        protected BaseMeshDisplayer GetMeshInteractableDisplayer(IMarchingCubeInteractableChunk interactable)
        {
            BaseMeshDisplayer d = chunkHandler.GetNextInteractableMeshDisplayer(interactable);
            activeDisplayers.Add(d);
            return d;
        }


        /// <summary>
        /// only resets mesh with a collider active (does not include border connections meshed)
        /// </summary>
        protected void SoftResetMeshDisplayers()
        {
            for (int i = 0; i < activeDisplayers.Count; i++)
            {
                if (activeDisplayers[i].IsColliderActive)
                    FreeMeshDisplayerAt(ref i);
            }
        }

        protected void FreeMeshDisplayerAt(ref int index)
        {
            chunkHandler.FreeMeshDisplayer(activeDisplayers[index]);
            activeDisplayers.RemoveAt(index);
            index -= 1;
        }

        /// <summary>
        /// only resets mesh without attached chunks (border meshes)
        /// </summary>
        protected void ResetBorderGlueMesh()
        {
            for (int i = 0; i < activeDisplayers.Count; i++)
            {
                if (!activeDisplayers[i].HasChunk)
                    FreeMeshDisplayerAt(ref i);
            }
        }

        protected void FreeAllMeshes()
        {
            chunkHandler.FreeAllDisplayers(activeDisplayers);
            activeDisplayers.Clear();
        }


        protected void AddCubeForNeigbhourInDirection(int key, MarchingCubeEntity c)
        {
            List<MarchingCubeEntity> cubes;
            if (!cubesForNeighbourInDirection.TryGetValue(key, out cubes))
            {
                cubes = new List<MarchingCubeEntity>();
                cubesForNeighbourInDirection.Add(key, cubes);
            }
            cubes.Add(c);
        }


        protected virtual void SetCurrentMeshData(bool isBorderConnectionMesh)
        {
            BaseMeshDisplayer displayer = GetBestMeshDisplayer();
            displayer.ApplyMesh(colorData, vertices, meshTriangles, Material, !isBorderConnectionMesh);
        }


        protected void ApplyChangesToMesh(bool isBorderConnectionMesh)
        {
            SetCurrentMeshData(isBorderConnectionMesh);
            trisLeft -= meshTriangles.Length;
            if (trisLeft > 0)
            {
                ResetArrayData();
            }
        }

        protected static Color brown = new Color(75, 44, 13, 1) / 255f;

        protected Color GetColor(PathTriangle t)
        {
            return GetColor(t.normal, t.middlePoint, t.slope);
        }

        protected Color GetColor(Vector3 normal, Vector3 middlePoint, float slope)
        {
            float slopeProgress = Mathf.InverseLerp(15, 45, slope);
            return (Color.green * (1 - slopeProgress) + brown * slopeProgress) / 2;
        }

        protected void ResetArrayData()
        {
            int size = Mathf.Min(trisLeft, MAX_TRIANGLES_PER_MESH + 1);
            meshTriangles = new int[size];
            vertices = new Vector3[size];
            colorData = new Color[size];
        }


        protected void BuildMeshToConnectHigherLodChunks()
        {
            trisLeft = connectorTriangleCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            var outerEnum = neighbourChunksGlue.Values.GetEnumerator();
            MarchingCubeEntity e;
            while (outerEnum.MoveNext())
            {
                e = outerEnum.Current;
                for (int i = 0; i < e.triangles.Count; i++)
                {
                    AddTriangleToMeshData(e.triangles[i], ref usedTriCount, ref totalTreeCount, true);
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


        public void AddNeighbourFromEntity(Vector3Int offset, Vector3Int v3, MarchingCubeEntity from)
        {
            HasNeighbourInDirection[VectorExtension.GetIndexFromDirection(offset)] = v3;

            ///also save size to fill hirachy with smaller chunks
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


        protected Vector3Int CoordFromCubeIndex(int i)
        {
            return new Vector3Int
               ((i % (vertexSize * vertexSize) % vertexSize)
               , (i % (vertexSize * vertexSize) / vertexSize)
               , (i / (vertexSize * vertexSize))
               );
        }

        protected Vector3Int CoordFromPointIndex(int i)
        {
            return new Vector3Int
               ((i % (PointsPerAxis * PointsPerAxis) % PointsPerAxis)
               , (i % (PointsPerAxis * PointsPerAxis) / PointsPerAxis)
               , (i / (PointsPerAxis * PointsPerAxis))
               );
        }

        protected int PointIndexFromCoord(int x, int y, int z)
        {
            int index = z * PointsPerAxis * PointsPerAxis + y * PointsPerAxis + x;
            return index;
        }

        protected int PointIndexFromCoord(Vector3Int v)
        {
            return PointIndexFromCoord(v.x, v.y, v.z);
        }


        protected int ClampInChunk(int i)
        {
            return i.FloorMod(vertexSize);
        }


        protected Vector4 BuildVector4FromCoord(int x, int y, int z, int lod)
        {
            int globalLod = lod * this.lod;
            //x *= lod;
            //y *= lod;
            //z *= lod;
            return new Vector4(AnchorPos.x + x * globalLod, AnchorPos.y + y * globalLod, AnchorPos.z + z * globalLod, points[PointIndexFromCoord(x, y, z)]);
        }

        protected Vector4 BuildVector4FromCoord(int x, int y, int z)
        {
            return new Vector4(AnchorPos.x + x * lod, AnchorPos.y + y * lod, AnchorPos.z + z * lod, points[PointIndexFromCoord(x, y, z)]);
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

        protected static readonly Vector3[] CubeCornersOffset = 
                new Vector3[]{
                    new Vector3(0,0,0),
                    new Vector3(1,0,0),
                    new Vector3(1,0,1),
                    new Vector3(0,0,1),
                    new Vector3(0,1,0),
                    new Vector3(1,1,0),
                    new Vector3(1,1,1),
                    new Vector3(0,1,1)
            };
        

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
                PointIndexFromCoord(p.x, p.y, p.z),
                PointIndexFromCoord(p.x + 1, p.y, p.z),
                PointIndexFromCoord(p.x + 1, p.y, p.z + 1),
                PointIndexFromCoord(p.x, p.y, p.z + 1),
                PointIndexFromCoord(p.x, p.y + 1, p.z),
                PointIndexFromCoord(p.x + 1, p.y + 1, p.z),
                PointIndexFromCoord(p.x + 1, p.y + 1, p.z + 1),
                PointIndexFromCoord(p.x, p.y + 1, p.z + 1)
            };
        }

        public void ChangeNeighbourLodTo(int newLodPower, Vector3Int dir)
        {
            int oldLodPower = neighbourLODs.GetLodPowerFromNeighbourInDirection(dir);
            if(oldLodPower < LODPower)
            {
                //delete old glue mesh
            }
            if(newLodPower < LODPower)
            {
                //build new connection
            }
        }
    }

}