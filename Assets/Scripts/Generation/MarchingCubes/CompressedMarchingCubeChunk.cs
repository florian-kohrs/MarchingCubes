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
        //    Debug.Log("Destroyed chunk");
        //}

        #region static fields

        protected static object listLock = new object();

        public static List<Exception> xs = new List<Exception>();

        #endregion

        #region fields


        protected WorldUpdater chunkUpdater;

        protected ChunkLodCollider chunkSimpleCollider;

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

        public bool HasStarted { get; protected set; }

        public Material Material { protected get; set; }

        public bool[] HasNeighbourInDirection { get; private set; } = new bool[6];

        public bool IsSpawner { get; set; }

        public ChunkGroupTreeLeaf Leaf { get; set; }

        public Vector3Int CenterPos => chunkCenterPosition;

        public ChunkLodCollider ChunkSimpleCollider { set { chunkSimpleCollider = value; } }

        public WorldUpdater ChunkUpdater { set { chunkUpdater = value; } }


        public ComputeBuffer minDegreeBuffer;

        public ComputeBuffer MinDegreeBuffer { get { return minDegreeBuffer; } set { minDegreeBuffer = value; } }

        protected bool ShouldBuildEnvironment => minDegreeBuffer != null;

        public Maybe<MeshData> MeshData => meshData;

        protected Maybe<MeshData> meshData;

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
                if (targetLodPower > lodPower)
                {
                    chunkUpdater.lowerChunkLods.Add(this);
                    chunkUpdater.increaseChunkLods.Remove(this);
                }
                else if (targetLodPower == lodPower)
                {
                    chunkUpdater.lowerChunkLods.Remove(this);
                    chunkUpdater.increaseChunkLods.Remove(this);
                }
                else
                {
                    chunkUpdater.increaseChunkLods.Add(this);
                    chunkUpdater.lowerChunkLods.Remove(this);
                }
            }
        }


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

        public int ChunkSizePower
        {
            get => chunkSizePower;
            set { chunkSizePower = value; ChunkSize = (int)Mathf.Pow(2, chunkSizePower); }
        }

        public virtual bool UseCollider => false;

        public bool BuildDetailedEnvironment => LOD == 1;

        public int PointSpacing { get; set; } = 1;


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

        public virtual void BuildEnvironmentForChunk()
        {
            if (ShouldBuildEnvironment)
            {
                StartEnvironmentPipeline();
            }
        }

     
        public virtual void InitializeWithMeshData(MeshData meshData)
        {
            HasStarted = true;
            this.meshData = new Maybe<MeshData>();
            this.meshData.Value = meshData;

            if (!meshData.IsEmpty)
            {
                NumTris = meshData.vertices.Length / 3;
                RebuildFromMeshData(meshData);
            }

            if(meshData.IsEmpty && !IsSpawner)
            {
                DestroyChunk();
            }

            //TODO: ELSE{PrepareDestroy? give displayers back?}
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
            ReturnMinDegreeBuffer();
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


        protected void SetNeighbourAt(int neighbourData)
        {
            //for (int i = 0; i < 8; i++)
            //{
            //    HasNeighbourInDirection[i] = HasNeighbourInDirection[i] && neighbourData % 2 == 0;
            //    neighbourData = neighbourData >> 1;
            //}

            if((neighbourData & 1) == 1)
                HasNeighbourInDirection[0] = true;
            else if((neighbourData & 2) == 1)
                HasNeighbourInDirection[1] = true;

            if ((neighbourData & 4) == 1)
                HasNeighbourInDirection[2] = true;
            else if ((neighbourData & 8) == 1)
                HasNeighbourInDirection[3] = true;

            if ((neighbourData & 16) == 1)
                HasNeighbourInDirection[4] = true;
            else if ((neighbourData & 32) == 1)
                HasNeighbourInDirection[5] = true;

            if ((neighbourData & 64) == 1)
                HasNeighbourInDirection[6] = true;
            else if ((neighbourData & 128) == 1)
                HasNeighbourInDirection[7] = true;

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
