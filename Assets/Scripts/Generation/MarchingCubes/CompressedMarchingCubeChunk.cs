using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MarchingCubes
{

    public class CompressedMarchingCubeChunk : ICompressedMarchingCubeChunk
    {

        //TODO: Check this: Graphics.DrawProceduralIndirect

        //~CompressedMarchingCubeChunk()
        //{
        //    Debug.Log("Destroyed chunk");
        //}

        #region static fields

        protected static object listLock = new object();

        public static List<Exception> xs = new List<Exception>();

        #endregion

        #region fields


        protected WorldUpdater chunkUpdater;

        protected ChunkLodCollider chunkSimpleCollider;

        protected const int MAX_TRIANGLES_PER_MESH = 65000;

        protected List<MarchingCubeMeshDisplayer> activeDisplayers = new List<MarchingCubeMeshDisplayer>();

        protected int lod = 1;

        protected int lodPower;

        protected int targetLodPower = -1;

        protected int TriCount =>  NumTris * 3;

        protected int trisLeft;

        protected int chunkSize;

        protected int chunkSizePower;

        protected int vertexSize;

        protected int maxEntityIndexPerAxis;

        protected int pointsPerAxis;

        protected int sqrPointsPerAxis;
        
        protected MarchingCubeMeshDisplayer freeDisplayer;


        protected Vector3Int chunkCenterPosition;


        public IMarchingCubeChunkHandler chunkHandler;

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
        protected Color32[] colorData;

        //protected bool isCompletlyAir;

        private Vector3Int anchorPos;

        int ICompressedMarchingCubeChunk.PointsPerAxis => pointsPerAxis;

        protected float surfaceLevel;

        protected Queue<ICompressedMarchingCubeChunk> readyChunks;

        protected Action<ICompressedMarchingCubeChunk> OnChunkFinished;

        protected List<MeshData> data = new List<MeshData>();

        public Maybe<Bounds> MeshBounds { get; protected set; } = new Maybe<Bounds>();


        #endregion

        #region properties

        public bool IsEmpty => NumTris == 0;

        public int NumTris { get; protected set; }

        //public bool IsCompletlyAir => isCompletlyAir;

        public bool IsReady { get; set; }

        public bool HasStarted { get; protected set; }

        public Material Material { protected get; set; }

        public bool[] HasNeighbourInDirection { get; private set; } = new bool[6];

        public bool IsSpawner { get; set; }

        public bool IsInOtherThread { get; set; }

        public ChunkGroupTreeLeaf Leaf { get; set; }

        public float SurfaceLevel { set => surfaceLevel = value; }

        protected int PointsPerAxis => pointsPerAxis;

        public Vector3Int CenterPos => chunkCenterPosition;

        public ChunkLodCollider ChunkSimpleCollider { set { chunkSimpleCollider = value; } }

        public WorldUpdater ChunkUpdater { set { chunkUpdater = value; } }


        public ComputeBuffer minDegreeBuffer;

        public ComputeBuffer MinDegreeBuffer { get { return minDegreeBuffer; } set { minDegreeBuffer = value; } }

        protected bool ShouldBuildEnvironment => minDegreeBuffer != null;

        protected TriangleChunkHeap triangleHeap;

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

        public int TargetLODPower
        {
            get
            {
                return targetLodPower;
            }
            set
            {
                targetLodPower = value;
                if (targetLodPower == MarchingCubeChunkHandler.DESTROY_CHUNK_LOD)
                {
                    DestroyChunk();
                }
                else if (targetLodPower > lodPower)
                {
                    chunkUpdater.lowerChunkLods.Add(this);
                    chunkUpdater.increaseChunkLods.Remove(this);
                }
                else if (targetLodPower < lodPower)
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

        public virtual bool UseCollider => false;

        public bool BuildDetailedEnvironment => LOD == 1;

        public TriangleChunkHeap ChunkHeap => triangleHeap;


        #endregion properties

        #region getter and setter methods

        public void SetSimpleCollider()
        {
            if (chunkSimpleCollider == null)
            {
                chunkHandler.SetChunkColliderOf(this);
            }
        }

        #endregion

        #region async chunk building

        public void InitializeWithMeshDataParallel(TriangleChunkHeap heap, Queue<ICompressedMarchingCubeChunk> readyChunks)
        {
            this.readyChunks = readyChunks;
            StartParallel(heap);
        }

        public void InitializeWithMeshDataParallel(TriangleChunkHeap heap, Action<ICompressedMarchingCubeChunk> OnChunkFinished)
        {
            this.OnChunkFinished = OnChunkFinished;
            StartParallel(heap);
        }

        protected void StartParallel(TriangleChunkHeap heap)
        {
            HasStarted = true;
            ThreadPool.QueueUserWorkItem((o) => RequestChunk(heap));
        }

        protected void RequestChunk(TriangleChunkHeap heap)
        {
            try
            {
                IsInOtherThread = true;
                InitializeWithMeshData(heap);
                OnChunkDone();
            }
            catch (Exception x)
            {
                xs.Add(x);
                Console.WriteLine(x);
                //Debug.LogException(x);
            }
        }

        protected void OnChunkDone()
        {
            if (readyChunks != null)
            {
                lock (listLock)
                {
                    readyChunks.Enqueue(this);
                }
            }
            if(OnChunkFinished != null)
            {
                OnChunkFinished(this);
                OnChunkFinished = null;
            }
        }

        public void BuildAllMeshes()
        {
            for (int i = 0; i < data.Count; ++i)
            {
                ApplyChangesToMesh(data[i]);
            }
        }

        protected void ApplyChangesToMesh(in MeshData d)
        {
            MarchingCubeMeshDisplayer displayer = GetFittingMeshDisplayer();
            displayer.ApplyMesh(d.colorData, d.vertices, d.triangles, Material, d.useCollider);
        }

        public virtual void SetChunkOnMainThread()
        {
            IsInOtherThread = false;
            if (!IsEmpty)
            {
                BuildAllMeshes();
            }

            if (ShouldBuildEnvironment)
            {
                StartEnvironmentPipeline(triangleHeap);
            }
            CleanUpOnMainThread();
        }

        #endregion async chunk building


        public virtual void InitializeWithMeshData(TriangleChunkHeap tris)
        {
            HasStarted = true;
            NumTris = tris.triCount;

            //neighbourLODs = chunkHandler.GetNeighbourLODSFrom(this);
            //careAboutNeighbourLODS = neighbourLODs.HasNeighbourWithHigherLOD(LODPower);
            if (!IsEmpty)
            {
                RebuildFromTriangleArray(tris);
            }
            IsReady = true;

            if (ShouldBuildEnvironment)
            {
                if (!IsInOtherThread)
                {
                    StartEnvironmentPipeline(tris);
                    CleanUpOnMainThread();
                }
                else
                {
                    triangleHeap = tris;
                }
            }
        }

        protected void CleanUpOnMainThread()
        {
            triangleHeap = null;
            if (ShouldBuildEnvironment)
            {
                chunkHandler.ReturnMinDegreeBuffer(minDegreeBuffer);
                minDegreeBuffer = null;
            }
        }

        protected void StartEnvironmentPipeline(TriangleChunkHeap tris)
        {
            if (IsEmpty)
                return;

            SetBoundsOfChunk();
            triangleHeap = tris;
            if (!IsInOtherThread)
            {
                ChunkHandler.StartEnvironmentPipelineForChunk(this);
                triangleHeap = null;
            }
        }


        protected void SetBoundsOfChunk()
        {
            MeshBounds.Value = activeDisplayers[0].mesh.bounds;
            for (int i = 1; i < activeDisplayers.Count; i++)
            {
                MeshBounds.Value.Encapsulate(activeDisplayers[i].mesh.bounds);
            }
        } 

        public void ResetChunk()
        {
            NumTris = 0;
            meshTriangles = null;
            lodPower = MarchingCubeChunkHandler.DEACTIVATE_CHUNK_LOD;
            lod = (int)Mathf.Pow(2, lodPower);
            vertices = null;
            FreeAllMeshes();
            OnChunkReset();
        }

        protected virtual void OnChunkReset() { }

        public void DestroyChunk()
        {
            FreeAllMeshes();
            PrepareDestruction();
            CleanUpOnMainThread();
        }

  
        public virtual void PrepareDestruction()
        {
            chunkUpdater.RemoveLowerLodChunk(this);
            if (Leaf != null)
            {
                Leaf.RemoveLeaf(this);
                Leaf = null;
            }
            IsReady = false;
            HasStarted = false;
            MeshBounds.LazyRemoveValue();
            FreeSimpleChunkCollider();
        }

      

        public void FreeSimpleChunkCollider()
        {
            if(chunkSimpleCollider != null)
            {
                chunkHandler.FreeCollider(chunkSimpleCollider);
                chunkSimpleCollider = null;
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
            trisLeft = TriCount;

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

        #region build mesh from triangles

        protected void AddTriangleToMeshData(PathTriangle tri, ref int usedTriCount, ref int totalTriCount)
        {
            Triangle t = tri.tri;

            meshTriangles[usedTriCount] = usedTriCount;
            meshTriangles[usedTriCount + 1] = usedTriCount + 1;
            meshTriangles[usedTriCount + 2] = usedTriCount + 2;

            colorData[usedTriCount] = tri.colorAndSteepness;
            colorData[usedTriCount + 1] = tri.colorAndSteepness;
            colorData[usedTriCount + 2] = tri.colorAndSteepness;

            vertices[usedTriCount] = t.a;
            vertices[usedTriCount + 1] = t.b;
            vertices[usedTriCount + 2] = t.c;


            usedTriCount += 3;
            totalTriCount++;
            if (usedTriCount >= MAX_TRIANGLES_PER_MESH || usedTriCount >= trisLeft)
            {
                ApplyChangesToMesh();
                usedTriCount = 0;
            }
        }

        protected void AddTriangleToMeshData(in TriangleBuilder t, ref int usedTriCount, ref int totalTriCount)
        {
            Color32 c = t.color32;

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
                ApplyChangesToMesh();
                usedTriCount = 0;
            }
        }

        #endregion

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

        protected MarchingCubeMeshDisplayer GetFittingMeshDisplayer()
        {
            if (UseCollider)
            {
                return GetMeshInteractableDisplayer((IMarchingCubeChunk)this);
            }
            else
            {
                return GetMeshDisplayer();
            }
        }

        protected MarchingCubeMeshDisplayer GetMeshInteractableDisplayer(IMarchingCubeChunk interactable)
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

        protected void FreeAllMeshes()
        {
            chunkHandler.FreeAllDisplayers(activeDisplayers);
            activeDisplayers.Clear();
        }



        protected virtual void SetCurrentMeshData()
        {
            MarchingCubeMeshDisplayer displayer = GetFittingMeshDisplayer();
            displayer.ApplyMesh(colorData, vertices, meshTriangles, Material, UseCollider);
        }


        protected void ApplyChangesToMesh()
        {
            if (IsInOtherThread)
            {
                data.Add(new MeshData(meshTriangles, vertices, colorData, UseCollider));
            }
            else
            {
                SetCurrentMeshData();
                trisLeft -= meshTriangles.Length;
                //if (trisLeft > 0)
                {
                    ResetArrayData();
                }
            }
        }


        protected void ResetArrayData()
        {
            int size = Mathf.Min(trisLeft, MAX_TRIANGLES_PER_MESH + 1);
            meshTriangles = new int[size];
            vertices = new Vector3[size];
            colorData = new Color32[size];
        }

        #region chunk queries

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

        #endregion chunk queries

        #region index and point transformations
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



        #endregion index and point transformations

      


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


       

    }

}