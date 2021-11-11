using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public class CompressedMarchingCubeChunk : IMarchingCubeChunk
    {

        //TODO: Check this: Graphics.DrawProceduralIndirect

        public virtual void InitializeWithMeshDataParallel(TriangleChunkHeap tris, Queue<IThreadedMarchingCubeChunk> readyChunks)
        {
            throw new Exception("This class doesnt support concurrency");
        }

        ~CompressedMarchingCubeChunk()
        {
            Debug.Log("Destroyed chunk");
        }

        public virtual void InitializeWithMeshDataParallel(TriangleChunkHeap triangleData, Action<IThreadedMarchingCubeChunk> OnChunkDone)
        {
            throw new Exception("This class doesnt support concurrency");
        }
        public bool IsInOtherThread { get; set; }

        public virtual void InitializeWithMeshData(TriangleChunkHeap tris)
        {
            HasStarted = true;
            triCount = tris.triCount * 3;

            if (points != null)
            {
                isCompletlySolid = IsEmpty && points[0] >= surfaceLevel;
                isCompletlyAir = IsEmpty && points[0] < surfaceLevel;
            }

            //neighbourLODs = chunkHandler.GetNeighbourLODSFrom(this);
            //careAboutNeighbourLODS = neighbourLODs.HasNeighbourWithHigherLOD(LODPower);
            if (!IsEmpty)
            {
                RebuildFromTriangleArray(tris);

                points = null;
            }

            IsReady = true;

        }


        public void ResetChunk()
        {
            triCount = 0;
            points = null;
            meshTriangles = null;
            lodPower = MarchingCubeChunkHandler.DEACTIVATE_CHUNK_LOD;
            lod = (int)Mathf.Pow(2, lodPower);
            vertices = null;
            FreeAllMeshes();
        }

        public void DestroyChunk()
        {
            FreeAllMeshes();
            PrepareDestruction();
        }

        public void SetLeaf(ChunkGroupTreeLeaf leaf)
        {
            this.leaf = leaf;
        }

        protected ChunkGroupTreeLeaf leaf;

        protected WorldUpdater chunkUpdater;

        protected ChunkLodCollider chunkSimpleCollider;


        public ChunkLodCollider ChunkSimpleCollider
        {
            set
            {
                chunkSimpleCollider = value;
            }
        }


        public void PrepareDestruction()
        {
            chunkUpdater.RemoveLowerLodChunk(this);
            if (leaf != null)
            {
                leaf.RemoveLeaf(this);
                leaf = null;
            }
            IsReady = false;
            HasStarted = false;
            FreeSimpleChunkCollider();
        }

        public void GetSimpleCollider()
        {
            if (chunkSimpleCollider == null)
            {
                chunkHandler.SetChunkColliderOf(this);
            }
        }

        public void FreeSimpleChunkCollider()
        {
            if(chunkSimpleCollider != null)
            {
                chunkHandler.FreeCollider(chunkSimpleCollider);
                chunkSimpleCollider = null;
            }
        }

        public WorldUpdater ChunkUpdater
        {
            set
            {
                chunkUpdater = value;
            }
        }

        public bool IsReady { get; set; }

        public bool HasStarted { get; protected set; }


        protected float surfaceLevel;

        protected const int MAX_TRIANGLES_PER_MESH = 65000;

        protected List<MarchingCubeMeshDisplayer> activeDisplayers = new List<MarchingCubeMeshDisplayer>();

        protected int lod = 1;

        public int LOD
        {
            get
            {
                return lod;
            }
            protected set
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
                targetLodPower = value;
                LOD = (int)Mathf.Pow(2, lodPower);
            }
        }

        protected int targetLodPower = -1;

        public int TargetLODPower
        {
            get
            {
                return targetLodPower;
            }
            set
            {
                targetLodPower = value;
                if(targetLodPower == MarchingCubeChunkHandler.DESTROY_CHUNK_LOD)
                {
                    DestroyChunk();
                }
                else if(targetLodPower > lodPower)
                {
                    chunkUpdater.lowerChunkLods.Add(this);
                    chunkUpdater.increaseChunkLods.Remove(this);
                }
                else if(targetLodPower < lodPower)
                {
                    chunkUpdater.increaseChunkLods.Add(this);
                    chunkUpdater.lowerChunkLods.Remove(this);
                }
                else
                {
                    chunkUpdater.lowerChunkLods.Remove(this);
                    chunkUpdater.increaseChunkLods.Remove(this);
                }
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
                points = value;
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

        protected int trisLeft;

        protected int chunkSize;

        protected int chunkSizePower;

        protected int vertexSize;

        protected int maxEntityIndexPerAxis;

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
            maxEntityIndexPerAxis = vertexSize - 1;
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

        public bool IsSpawner { get; set; }

        protected void SetNeighbourAt(int x, int y, int z)
        {
            if (x == 0)
            {
                HasNeighbourInDirection[1] = true;
            }
            else if (x == maxEntityIndexPerAxis)
            {
                HasNeighbourInDirection[0] = true;
            }

            if (y == 0)
            {
                HasNeighbourInDirection[3] = true;
            }
            else if (y == maxEntityIndexPerAxis)
            {
                HasNeighbourInDirection[2] = true;
            }

            if (z == 0)
            {
                HasNeighbourInDirection[5] = true;
            }
            else if (z == maxEntityIndexPerAxis)
            {
                HasNeighbourInDirection[4] = true;
            }
        }


        protected virtual void RebuildFromTriangleArray(TriangleChunkHeap heap)
        {
            trisLeft = triCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            TriangleBuilder[] ts = heap.tris;
            int endIndex = heap.startIndex + heap.triCount;
            for (int i = heap.startIndex; i < endIndex; ++i)
            {
                SetNeighbourAt(ts[i].x, ts[i].y, ts[i].z);

                AddTriangleToMeshData(in ts[i], ref usedTriCount, ref totalTreeCount);
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

        protected MarchingCubeMeshDisplayer freeDisplayer;

        public void AddDisplayer(MarchingCubeMeshDisplayer b)
        {
            freeDisplayer = b;
            activeDisplayers.Add(b);
        }

        protected MarchingCubeMeshDisplayer GetMeshDisplayer()
        {
            if(freeDisplayer != null)
            {
                MarchingCubeMeshDisplayer result = freeDisplayer;
                freeDisplayer = null;
                return result;
            }
            else
            {
                MarchingCubeMeshDisplayer d = chunkHandler.GetNextMeshDisplayer();
                activeDisplayers.Add(d);
                return d;
            }
        }

        protected MarchingCubeMeshDisplayer GetBestMeshDisplayer()
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

        protected MarchingCubeMeshDisplayer GetMeshInteractableDisplayer(IMarchingCubeInteractableChunk interactable)
        {
            if (freeDisplayer != null)
            {
                MarchingCubeMeshDisplayer result = freeDisplayer;
                result.SetInteractableChunk(interactable);
                freeDisplayer = null;
                return result;
            }
            else
            {
                MarchingCubeMeshDisplayer d = chunkHandler.GetNextInteractableMeshDisplayer(interactable);
                activeDisplayers.Add(d);
                return d;
            }
        }

        public void GiveUnusedDisplayerBack()
        {
            chunkHandler.TakeMeshDisplayerBack(freeDisplayer);
            activeDisplayers.Clear();
            freeDisplayer = null;
        }

        /// <summary>
        /// only resets mesh with a collider active (does not include border connections meshed)
        /// </summary>
        public void SoftResetMeshDisplayers()
        {
            for (int i = 0; i < activeDisplayers.Count; ++i)
            {
                if (activeDisplayers[i].IsColliderActive)
                    FreeMeshDisplayerAt(ref i);
            }
            freeDisplayer = null;
        }

        protected void FreeMeshDisplayerAt(ref int index)
        {
            chunkHandler.FreeMeshDisplayer(activeDisplayers[index]);
            activeDisplayers.RemoveAt(index);
            index -= 1;
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
            MarchingCubeMeshDisplayer displayer = GetBestMeshDisplayer();
            bool useCollider = !isBorderConnectionMesh && !(this is CompressedMarchingCubeChunkThreaded);
            displayer.ApplyMesh(colorData, vertices, meshTriangles, Material, useCollider);
        }


        protected void ApplyChangesToMesh(bool isBorderConnectionMesh)
        {
            SetCurrentMeshData(isBorderConnectionMesh);
            trisLeft -= meshTriangles.Length;
            //if (trisLeft > 0)
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

        public bool IsBorderOrOutsidePoint(int x, int y, int z)
        {
            return
                x <= 0 || x >= pointsPerAxis - 1
                && y <= 0 && y >= pointsPerAxis - 1
                && z <= 0 && z >= pointsPerAxis - 1;
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

        protected bool IsBorderCube(int x, int y, int z)
        {
            return x == 0 || x == maxEntityIndexPerAxis
                || y == 0 || y == maxEntityIndexPerAxis
                || z == 0 || z == maxEntityIndexPerAxis;
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

        public ChunkGroupTreeLeaf GetLeaf()
        {
            return leaf;
        }

    }

}