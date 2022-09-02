using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using UnityEngine;

namespace MarchingCubes
{

    public class CompressedMarchingCubeChunk : ISizeManager, IEnvironmentSurface
    {

        //TODO: Check this: Graphics.DrawProceduralIndirect

        //~CompressedMarchingCubeChunk()
        //{
        //    totalchunks--;
        //    //Debug.Log("Destroyed chunk");
        //}

        //static int totalchunks = 0;

        //public CompressedMarchingCubeChunk()
        //{
        //    Debug.Log("Total Chunks:" + totalchunks++);
        //}

        #region static fields

        protected static object listLock = new object();

        public static List<Exception> xs = new List<Exception>();

        #endregion

        #region fields


        protected WorldUpdater chunkUpdater;

        public const int MAX_TRIANGLES_PER_MESH = 65000;

        protected List<MarchingCubeMeshDisplayer> activeDisplayers = new List<MarchingCubeMeshDisplayer>();

        protected int lod = 1;

        protected int lodPower;

        protected int targetLodPower = -1;

        protected int VertexCount =>  NumTris * 3;

        protected int vertsLeft;

        protected int chunkSize;

        protected int chunkSizePower;

        protected int vertexSize;

        protected int maxEntityIndexPerAxis;

        public int MaxEntitiesIndexPerAxis => maxEntityIndexPerAxis;

        protected int pointsPerAxis;

        protected int sqrPointsPerAxis;
        
        protected MarchingCubeMeshDisplayer freeDisplayer;


        protected Vector3Int chunkCenterPosition;


        public MarchingCubeChunkHandler chunkHandler;

        //protected List<BaseMeshChild> children = new List<BaseMeshChild>();
        protected Vector3[] vertices;
        protected Color32[] colorData;

        //protected bool isCompletlyAir;

        private Vector3Int anchorPos;

        public int PointsPerAxis => pointsPerAxis;

        public int NoisePointsPerAxis => ((pointsPerAxis - 1) * PointSpacing) + 1;

        protected Queue<CompressedMarchingCubeChunk> readyChunks;

        protected Action<CompressedMarchingCubeChunk> OnChunkFinished;

        protected List<MeshData> data = new List<MeshData>();

        public Maybe<Bounds> MeshBounds { get; protected set; } = new Maybe<Bounds>();


        #endregion

        #region properties

        public bool IsEmpty => NumTris == 0;

        public int NumTris { get; protected set; }

        //public bool IsCompletlyAir => isCompletlyAir;

        public bool IsReady { get; set; }

        public bool HasStarted { get; set; }

        public Material Material { protected get; set; }

        public ChunkGroupTreeLeaf Leaf { get; set; }

        public Vector3Int CenterPos => chunkCenterPosition;

        public WorldUpdater ChunkUpdater { set { chunkUpdater = value; } }


        public ComputeBuffer minDegreeBuffer;

        public ComputeBuffer MinDegreeBuffer { get { return minDegreeBuffer; } set { minDegreeBuffer = value; } }

        protected bool ShouldBuildEnvironment => minDegreeBuffer != null;

        public MeshData MeshData => meshData;

        protected MeshData meshData;

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


        protected bool isInIncreaseUpdater;
        protected bool isInDecreaseUpdater;

        public MarchingCubeChunkHandler ChunkHandler
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

        public Stack<int> GetStoragePath => Leaf.BuildChildIndexList(MarchingCubeChunkHandler.STORAGE_GROUP_SIZE_POWER);

        public int ChunkSizePower
        {
            get => chunkSizePower;
            set { 
                chunkSizePower = value; 
                ChunkSize = (int)Mathf.Pow(2, chunkSizePower); 
            }
        }

        public virtual bool UseCollider => false;

        public bool BuildDetailedEnvironment => LOD == 1;

        public int PointSpacing { get; set; } = 1;


        #endregion properties

        #region getter and setter methods

        #endregion

        public virtual void BuildEnvironmentForChunk()
        {
            if (ShouldBuildEnvironment)
            {
                StartEnvironmentPipeline();
            }
        }

        public virtual void PrepareInitializationWithMeshData(MeshData meshData)
        {
            this.meshData = meshData;
        }


        public virtual void InitializeWithMeshData(MeshData meshData)
        {
            this.meshData = meshData;
            if (!meshData.IsEmpty)
            {
                NumTris = meshData.vertices.Length / 3;
                RebuildFromMeshData(meshData);
            }

            if(meshData.IsEmpty)
            {
                DestroyChunk();
            }

            IsReady = true;

            if (ShouldBuildEnvironment)
            {
                StartEnvironmentPipeline();
            }
        }

        protected void ReturnMinDegreeBuffer()
        {
            if (ShouldBuildEnvironment)
            {
                chunkHandler.ReturnMinDegreeBuffer(minDegreeBuffer);
                minDegreeBuffer = null;
            }
        }

        protected void StartEnvironmentPipeline()
        {
            if (IsEmpty)
                return;

            SetBoundsOfChunk();
            ChunkHandler.StartEnvironmentPipelineForChunk(this);
            ReturnMinDegreeBuffer();

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
            lodPower = MarchingCubeChunkHandler.DEACTIVATE_CHUNK_LOD_POWER;
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
            ReturnMinDegreeBuffer();
        }

  
        public virtual void PrepareDestruction()
        {
            if (Leaf != null)
            {
                if (MarchingCubeChunkHandler.InitialWorldBuildingDone)
                    Leaf.DestroyLeaf();
                else
                    chunkUpdater.leafsWithEmptyChunks.Add(Leaf);
                Leaf = null;
            }
            IsReady = false;
            HasStarted = false;
            MeshBounds.LazyRemoveValue();
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


        protected virtual void RebuildFromMeshData(MeshData meshData)
        {
            SetMeshData(meshData);
        }

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
                return GetMeshInteractableDisplayer((ReducedMarchingCubesChunk)this);
            }
            else
            {
                return GetMeshDisplayer();
            }
        }

        protected MarchingCubeMeshDisplayer GetMeshInteractableDisplayer(ReducedMarchingCubesChunk interactable)
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


        protected virtual void SetMeshData(MeshData meshData)
        {
            MarchingCubeMeshDisplayer displayer = GetFittingMeshDisplayer();
            displayer.ApplyMesh(meshData.colorData, meshData.vertices, Material, meshData.useCollider);
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

      


        public Direction[] NeighbourDirections(int x, int y, int z, int space = 0)
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

            return DirectionHelper.GetAllNonDefaultAxisCombinations(v3);
        }


       

    }

}
