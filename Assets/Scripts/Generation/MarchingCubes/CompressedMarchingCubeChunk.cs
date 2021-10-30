using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class CompressedMarchingCubeChunk : IMarchingCubeChunk
    {


        public virtual void InitializeWithMeshDataParallel(TriangleBuilder[] tris, Queue<IThreadedMarchingCubeChunk> readyChunks, bool keepPoints = false)
        {
            throw new Exception("This class doesnt support concurrency");
        }

        public virtual void InitializeWithMeshData(TriangleBuilder[] tris, bool keepPoints = false)
        {
            HasStarted = true;
            triCount = tris.Length * 3;

            if (points != null)
            {
                isCompletlySolid = IsEmpty && points[0] >= surfaceLevel;
                isCompletlyAir = IsEmpty && points[0] < surfaceLevel;
            }

            neighbourLODs = chunkHandler.GetNeighbourLODSFrom(this);
            careAboutNeighbourLODS = neighbourLODs.HasNeighbourWithHigherLOD(LODPower);
            if (!IsEmpty)
            {
                BuildFromTriangleArray(tris);

                WorkOnBuildedChunk();

                if (!keepPoints)
                {
                    points = null;
                }
            }
            IsReady = true;
        }


        public void ResetChunk()
        {
            FreeAllMeshes();
            OnResetChunk();
            points = null;
        }

        public void SetLeaf(ChunkGroupTreeLeaf leaf)
        {
            this.leaf = leaf;
        }

        protected ChunkGroupTreeLeaf leaf;

        protected virtual void OnResetChunk() { }

        public bool IsReady { get; set; }

        public bool HasStarted { get; protected set; }


        protected float surfaceLevel;

        protected const int MAX_TRIANGLES_PER_MESH = 65000;

        protected MarchingCubeChunkNeighbourLODs neighbourLODs;

        protected MarchingCubeChunkNeighbourLODs NeighbourLODs
        {
            get
            {
                if (neighbourLODs == null)
                {
                    neighbourLODs = chunkHandler.GetNeighbourLODSFrom(this);
                }
                return neighbourLODs;
            }
        }

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
                UpdateChunkData();
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

        public float[] Points
        {
            get
            {
                if (points == null)
                {
                    points = chunkHandler.RequestNoiseForChunk(this);
                }
                return points;
            }
            set
            {
                points = Points;
            }
        }

        protected void RequestPointsIfNotStored()
        {
            if (points == null)
            {
                points = chunkHandler.RequestNoiseForChunk(this);
            }
        }

        protected int triCount;

        protected int connectorTriangleCount = 0;

        protected int trisLeft;

        protected int chunkSize;

        protected int chunkSizePower;

        protected int vertexSize;

        protected int entitiesPerAxis;

        protected int pointsPerAxis;

        protected int sqrPointsPerAxis;

        public Material Material { protected get; set; }


        protected int PointsPerAxis => pointsPerAxis;


        //protected Vector3Int chunkOffset;

        protected Vector3Int chunkCenterPosition;

        public Vector3Int CenterPos => chunkCenterPosition;

        public IMarchingCubeChunkHandler chunkHandler;

        public IMarchingCubeChunkHandler ChunkHandler
        {
            protected get
            {
                return chunkHandler;
            }
            set
            {
                chunkHandler = value;
            }
        }

        public IMarchingCubeChunkHandler GetChunkHandler => chunkHandler;


        public bool[] HasNeighbourInDirection { get; private set; } = new bool[6];

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

        //TODO:Maybe remove this from chunks
        /// <summary>
        /// chunk is completly underground
        /// </summary>
        public bool IsCompletlySolid => isCompletlySolid;

        protected bool isCompletlySolid;

        /// <summary>
        /// chunk is completly air
        /// </summary>
        public bool IsCompletlyAir => isCompletlyAir;

        protected bool isCompletlyAir;

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
            vertexSize = chunkSize / lod;
            entitiesPerAxis = vertexSize - 1;
            pointsPerAxis = vertexSize + 1;
            sqrPointsPerAxis = pointsPerAxis * pointsPerAxis;
        }

        private Vector3Int anchorPos;

        int IMarchingCubeChunk.PointsPerAxis => pointsPerAxis;

        public int ChunkSize
        {
            get => chunkSize;
            protected set { chunkSize = value; UpdateChunkCenterPos(); UpdateChunkData(); }
        }

        public int ChunkSizePower
        {
            get => chunkSizePower;
            set { chunkSizePower = value; ChunkSize = (int)Mathf.Pow(2, chunkSizePower); }
        }

        public float SurfaceLevel { set => surfaceLevel = value; }


        protected virtual void WorkOnBuildedChunk()
        {
            if (neighbourChunksGlue.Count > 0)
            {
                BuildMeshToConnectHigherLodChunks();
            }
        }

        protected void SetNeighbourAt(Vector3Int v3)
        {
            SetNeighbourAt(v3.x, v3.y, v3.z);
        }

        protected void SetNeighbourAt(int x, int y, int z)
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


        protected bool IsDirectionOutOfChunk(Vector3Int v3)
        {
            return v3.x < 0 || v3.y < 0 || v3.z < 0;
        }

        protected virtual void BuildFromTriangleArray(TriangleBuilder[] ts, bool buildMeshAswell = true)
        {
            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            List<MissingNeighbourData> trisWithNeighboursOutOfBounds = new List<MissingNeighbourData>();
            for (int i = 0; i < ts.Length; ++i)
            {
                Vector3Int currentOrigin = ts[i].Origin;

                if (careAboutNeighbourLODS && IsBorderCube(currentOrigin))
                {
                    if (MarchingCubeEntity.FindMissingNeighboursAt(ts[i].triIndex, currentOrigin, IsCubeInBounds, HasNeighbourInDirection))
                    {
                        ProcessNeighboursFromList(trisWithNeighboursOutOfBounds, currentOrigin);
                    }
                }
                SetNeighbourAt(currentOrigin);

                //if (chunkHandler.TryGetReadyChunkAt(target, out c))
                //{
                //    if (c.LODPower > LODPower)
                //    {
                //        BuildMarchingCubeChunkTransitionInDirection(neighbour.originCubeEntity, neighbour, c.LODPower);
                //    }
                //}

                if (buildMeshAswell)
                {
                    AddTriangleToMeshData(in ts[i], ref usedTriCount, ref totalTreeCount);
                }
            }


            //MissingNeighbourData neighbour;
            //IMarchingCubeChunk c;

            //int missingNeighbourCount = trisWithNeighboursOutOfBounds.Count;
            //for (int i = 0; i < missingNeighbourCount; ++i)
            //{
            //    neighbour = trisWithNeighboursOutOfBounds[i];
            //    Vector3Int target = AnchorPos + (neighbour.outsideNeighbour.offset);
            //    Vector3Int border = neighbour.originCubeEntity + neighbour.outsideNeighbour.offset;

            //    AddNeighbourFromEntity(neighbour.outsideNeighbour.offset);

            //    if (chunkHandler.TryGetReadyChunkAt(target, out c))
            //    {
            //        if (c.LODPower > LODPower)
            //        {
            //            BuildMarchingCubeChunkTransitionInDirection(neighbour.originCubeEntity, neighbour, c.LODPower);
            //        }
            //    }
            //    else if (careAboutNeighbourLODS)
            //    {
            //        int neighbourLodPower = neighbourLODs.GetLodPowerFromNeighbourInDirection(neighbour.outsideNeighbour.offset);
            //        if (neighbourLodPower > LODPower)
            //        {
            //            BuildMarchingCubeChunkTransitionInDirection(neighbour.originCubeEntity, neighbour, neighbourLodPower);
            //        }
            //    }
            //}
        }


        protected void ProcessNeighboursFromList(List<MissingNeighbourData> l, Vector3Int origin)
        {
            int count = l.Count;
            for (int i = 0; i < count; ++i)
            {
                MissingNeighbourData t = l[i];
                Vector3Int target = AnchorPos + origin + t.outsideNeighbour.offset;
                //AddNeighbourFromEntity(t.outsideNeighbour.offset);
                IMarchingCubeChunk c;
                int otherLodPower;
                //TODO: may also take non ready chunks!
                if (chunkHandler.TryGetReadyChunkAt(target, out c))
                    otherLodPower = c.LODPower;
                else
                    otherLodPower = neighbourLODs.GetLodPowerFromNeighbourInDirection(t.outsideNeighbour.offset);

                //TODO: Only build chunk glue for cubes at right position
                //BuildMarchingCubeChunkTransitionInDirection(origin, t, otherLodPower);
            }
        }

        public virtual MarchingCubeEntity MarchAt(int x, int y, int z, ICubeNeighbourFinder chunk, int lod)
        {
            float[] noisePoints = GetNoiseInCornersForPoint(x, y, z, lod);

            int cubeIndex = 0;
            if (noisePoints[0] > surfaceLevel) cubeIndex |= 1;
            if (noisePoints[1] > surfaceLevel) cubeIndex |= 2;
            if (noisePoints[2] > surfaceLevel) cubeIndex |= 4;
            if (noisePoints[3] > surfaceLevel) cubeIndex |= 8;
            if (noisePoints[4] > surfaceLevel) cubeIndex |= 16;
            if (noisePoints[5] > surfaceLevel) cubeIndex |= 32;
            if (noisePoints[6] > surfaceLevel) cubeIndex |= 64;
            if (noisePoints[7] > surfaceLevel) cubeIndex |= 128;

            if (cubeIndex > 0 && cubeIndex < 255)
            {
                int[] cubeCorners = GetCubeCornerArrayForPoint(x, y, z, lod);
                MarchingCubeEntity e = new MarchingCubeEntity(chunk, cubeIndex);
                e.origin = new Vector3Int(x, y, z);

                int[] triangulation = TriangulationTable.triangulation[cubeIndex];
                int count = triangulation.Length;
                for (int i = 0; i < count; i += 3)
                {
                    // Get indices of corner points A and B for each of the three edges
                    // of the cube that need to be joined to form the triangle.
                    int a0 = TriangulationTable.cornerIndexAFromEdge[triangulation[i]];
                    int b0 = TriangulationTable.cornerIndexBFromEdge[triangulation[i]];

                    int a1 = TriangulationTable.cornerIndexAFromEdge[triangulation[i + 1]];
                    int b1 = TriangulationTable.cornerIndexBFromEdge[triangulation[i + 1]];

                    int a2 = TriangulationTable.cornerIndexAFromEdge[triangulation[i + 2]];
                    int b2 = TriangulationTable.cornerIndexBFromEdge[triangulation[i + 2]];

                    Triangle tri = new Triangle(
                        InterpolateVerts(cubeCorners, noisePoints, a0, b0),
                        InterpolateVerts(cubeCorners, noisePoints, a1, b1),
                        InterpolateVerts(cubeCorners, noisePoints, a2, b2));

                    e.AddTriangle(new PathTriangle(e, in tri, ChunkHandler.GetColor));
                    triCount += 3;
                }

                return e;
            }
            else
            {
                return null;
            }
        }

        protected Vector3 InterpolateVerts(int[] cubeCorners, float[] points, int startIndex1, int startIndex2)
        {
            int index1 = startIndex1 * 3;
            int index2 = startIndex2 * 3;
            float t = (surfaceLevel - points[startIndex1]) / (points[startIndex2] - points[startIndex1]);
            return new Vector3(
                cubeCorners[index1] + t * (cubeCorners[index2] - cubeCorners[index1]),
                cubeCorners[index1 + 1] + t * (cubeCorners[index2 + 1] - cubeCorners[index1 + 1]),
                cubeCorners[index1 + 2] + t * (cubeCorners[index2 + 2] - cubeCorners[index1 + 2]));
        }

        public virtual MarchingCubeEntity MarchAt(int x, int y, int z, int lod)
        {
            return MarchAt(x, y, z, null, lod);
        }

        public virtual MarchingCubeEntity MarchAt(int x, int y, int z, ICubeNeighbourFinder finder)
        {
            return MarchAt(x, y, z, finder, 1);
        }

        protected void CorrectMarchingCubeInDirection(Vector3Int origin, MissingNeighbourData missingData, IMarchingCubeChunk c)
        {
            BuildMarchingCubeChunkTransitionInDirection(origin, missingData, c.LODPower);
        }

        protected void BuildMarchingCubeChunkTransitionInDirection(Vector3Int origin, MissingNeighbourData missingData, int otherLodPower)
        {
            //Debug.Log("entitiy with neighbour in higher lod chunk");
            ///maybe add corrected triangles to extra mesh to not recompute them when chunk changes and easier remove /swap them if neighbour changes lod


            int lodDiff = (int)Mathf.Pow(2, otherLodPower - lodPower);

            Vector3Int rightCubeIndex = origin.Map(f => f - f % lodDiff);
            int x = rightCubeIndex.x;
            int y = rightCubeIndex.y;
            int z = rightCubeIndex.z;
            int key = PointIndexFromCoord(x, y, z);
            key = (key << MarchingCubeChunkHandler.MAX_CHUNK_LOD_BIT_REPRESENTATION_SIZE) + otherLodPower;
            if (!neighbourChunksGlue.ContainsKey(key))
            {
                //MarchingCubeEntity original = MarchAt(e.origin, 1);
                MarchingCubeEntity bindWithNeighbour = MarchAt(x, y, z, lodDiff);
                if (bindWithNeighbour != null)
                {
                    neighbourChunksGlue.Add(key, bindWithNeighbour);
                    AddCubeForNeigbhourInDirection(VectorExtension.GetIndexFromDirection(missingData.outsideNeighbour.offset), bindWithNeighbour);
                    connectorTriangleCount += bindWithNeighbour.triangles.Length * 3;
                }
                else
                {

                }
            }
        }


        protected void AddTriangleToMeshData(PathTriangle tri, Color c, ref int usedTriCount, ref int totalTriCount, bool isBorderConnectionMesh = false)
        {
            Triangle t = tri.tri;

            meshTriangles[usedTriCount] = usedTriCount;
            meshTriangles[usedTriCount + 1] = usedTriCount + 1;
            meshTriangles[usedTriCount + 2] = usedTriCount + 2;

            colorData[usedTriCount] = c;
            colorData[usedTriCount + 1] = c;
            colorData[usedTriCount + 2] = c;

            vertices[usedTriCount] = t.a;
            vertices[usedTriCount + 1] = t.b;
            vertices[usedTriCount + 2] = t.c;


            usedTriCount += 3;
            totalTriCount++;
            if (usedTriCount >= MAX_TRIANGLES_PER_MESH || usedTriCount >= trisLeft)
            {
                ApplyChangesToMesh(isBorderConnectionMesh);
                usedTriCount = 0;
            }
        }

        protected void AddTriangleToMeshData(in TriangleBuilder t, ref int usedTriCount, ref int totalTriCount, bool isBorderConnectionMesh = false)
        {
            Color c = new Color(t.r / 255f, t.g / 255f, t.b / 255f, 1);

            meshTriangles[usedTriCount] = usedTriCount;
            meshTriangles[usedTriCount + 1] = usedTriCount + 1;
            meshTriangles[usedTriCount + 2] = usedTriCount + 2;

            colorData[usedTriCount] = c;
            colorData[usedTriCount + 1] = c;
            colorData[usedTriCount + 2] = c;

            vertices[usedTriCount] = t.tri.a;
            vertices[usedTriCount + 1] = t.tri.b;
            vertices[usedTriCount + 2] = t.tri.c;

            usedTriCount += 3;
            totalTriCount++;
            if (usedTriCount >= MAX_TRIANGLES_PER_MESH || usedTriCount >= trisLeft)
            {
                ApplyChangesToMesh(isBorderConnectionMesh);
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
            if (this is IMarchingCubeInteractableChunk i)
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
            for (int i = 0; i < activeDisplayers.Count; ++i)
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
            for (int i = 0; i < activeDisplayers.Count; ++i)
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
            bool useCollider = !isBorderConnectionMesh && !(this is CompressedMarchingCubeChunkThreaded);
            displayer.ApplyMesh(colorData, vertices, meshTriangles, Material, useCollider);
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
                int count = e.triangles.Length;
                for (int i = 0; i < count; ++i)
                {
                    AddTriangleToMeshData(e.triangles[i], e.triangles[i].GetColor(), ref usedTriCount, ref totalTreeCount, true);
                }
            }
        }

        public bool IsCubeInBounds(Vector3Int v)
        {
            return IsCubeInBounds(v.x, v.y, v.z);
        }

        public bool IsPointInBounds(Vector3Int v)
        {
            return IsPointInBounds(v.x, v.y, v.z);
        }

        public bool IsPointInBounds(int x, int y, int z)
        {
            return
                x >= 0 && x < pointsPerAxis
                && y >= 0 && y < pointsPerAxis
                && z >= 0 && z < pointsPerAxis;
        }

        public bool IsBorderOrOutsidePoint(Vector3Int v)
        {
            return IsBorderOrOutsidePoint(v.x, v.y, v.z);
        }

        public bool IsBorderOrOutsidePoint(int x, int y, int z)
        {
            return
                x <= 0 || x >= pointsPerAxis - 1
                && y <= 0 && y >= pointsPerAxis - 1
                && z <= 0 && z >= pointsPerAxis - 1;
        }


        public Vector3Int[] NeighbourDirections(Vector3Int v)
        {
            return NeighbourDirections(v.x, v.y, v.z);
        }

        public Vector3Int[] NeighbourDirections(int x, int y, int z, int space = 0)
        {
            Vector3Int v3 = new Vector3Int();


            int pointsMinus = pointsPerAxis - 1 - space;
            if (x <= space)
            {
                v3.x = -1;
            }
            else if (x >= pointsMinus)
            {
                v3.x = 1;
            }

            if (y <= space)
            {
                v3.y = -1;
            }
            else if (y >= pointsMinus)
            {
                v3.y = 1;
            }

            if (z <= space)
            {
                v3.z = -1;
            }
            else if (z >= pointsMinus)
            {
                v3.z = 1;
            }

            return v3.GetAllNonDefaultAxisCombinations();
        }

        public bool IsCubeInBounds(int x, int y, int z)
        {
            return
                x >= 0 && x < vertexSize
                && y >= 0 && y < vertexSize
                && z >= 0 && z < vertexSize;
        }

        protected bool IsBorderCube(Vector3Int v)
        {
            return IsBorderCube(v.x, v.y, v.z);
        }


        protected bool IsBorderCube(int x, int y, int z)
        {
            return x == 0 || x == entitiesPerAxis
                || y == 0 || y == entitiesPerAxis
                || z == 0 || z == entitiesPerAxis;
        }

        public bool IsCubeInBounds(int[] v)
        {
            return IsCubeInBounds(v[0], v[1], v[2]);
        }


        public void AddNeighbourFromEntity(Vector3Int offset)
        {
            HasNeighbourInDirection[VectorExtension.GetIndexFromDirection(offset)] = true;
        }


        public Vector3Int CoordFromCubeIndex(int i)
        {
            return new Vector3Int
               ((i % (vertexSize * vertexSize) % vertexSize)
               , (i % (vertexSize * vertexSize) / vertexSize)
               , (i / (vertexSize * vertexSize))
               );
        }

        public Vector3Int CoordFromPointIndex(int i)
        {
            return new Vector3Int
               (i % sqrPointsPerAxis % pointsPerAxis
               , i % sqrPointsPerAxis / pointsPerAxis
               , i / sqrPointsPerAxis
               );
        }

        public int PointIndexFromCoord(int x, int y, int z)
        {
            int index = z * sqrPointsPerAxis + y * pointsPerAxis + x;
            return index;
        }



        public int PointIndexFromCoord(Vector3Int v)
        {
            return PointIndexFromCoord(v.x, v.y, v.z);
        }


        protected int ClampInChunk(int i)
        {
            return i.FloorMod(vertexSize);
        }


        protected int[] GetCubeCornerArrayForPoint(int x, int y, int z, int spacing)
        {
            Vector3Int v3 = AnchorPos;
            x *= lod;
            y *= lod;
            z *= lod;
            x += v3.x;
            y += v3.y;
            z += v3.z;

            int offset = spacing * lod;
            return new int[]
            {
                x, y, z,
                x + offset, y,z,
                x + offset, y, z + offset,
                x, y, z + offset,
                x, y + offset, z,
                x + offset, y + offset, z,
                x + offset, y + offset, z + offset,
                x, y + offset, z + offset
            };
        }

        protected float[] GetNoiseInCornersForPoint(int x, int y, int z, int lod)
        {
            int pointsLod = pointsPerAxis * lod;
            int sqrPointsLod = sqrPointsPerAxis * lod;
            int pointIndex = PointIndexFromCoord(x, y, z);
            return new float[]
            {
                points[pointIndex],
                points[pointIndex + lod],
                points[pointIndex + lod + sqrPointsLod],
                points[pointIndex + sqrPointsLod],
                points[pointIndex + pointsLod],
                points[pointIndex + lod + pointsLod],
                points[pointIndex + lod + pointsLod + sqrPointsLod],
                points[pointIndex + pointsLod + sqrPointsLod]
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
            if (oldLodPower < LODPower)
            {
                //delete old glue mesh
            }
            if (newLodPower < LODPower)
            {
                //build new connection
            }
        }
    }

}