﻿using MeshGPUInstanciation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


namespace MarchingCubes
{
    //TODO: Check to use unity mathematics int2, int3 instead of vector for better performance?
    [Serializable]
    public class MarchingCubeChunkHandler : SaveableMonoBehaviour
    {


        /// <summary>
        /// This should be the same value as in the compute shader "MarchingCubes"
        /// </summary>
        protected const float THREAD_GROUP_SIZE = 4;

        public const float REBUILD_SHADER_THREAD_GROUP_SIZE = 4;

        public const int MIN_CHUNK_SIZE = 8;

        public const int MIN_CHUNK_SIZE_POWER = 3;

        public const int SURFACE_LEVEL = 0;

        public const int STORAGE_GROUP_SIZE = 128;

        public const int STORAGE_GROUP_SIZE_POWER = 7;

        public const int STORAGE_GROUP_UNTIL_LOD = STORAGE_GROUP_SIZE_POWER - DEFAULT_CHUNK_SIZE_POWER;

        public static readonly int CHUNK_GROUP_SIZE = (int)Mathf.Pow(2,CHUNK_GROUP_SIZE_POWER);

        public const int CHUNK_GROUP_SIZE_POWER = DEFAULT_CHUNK_SIZE_POWER + MAX_CHUNK_EXTRA_SIZE_POWER;

        public const int DEFAULT_CHUNK_SIZE = 32;

        public const int POINTS_PER_AXIS_IN_DEFAULT_SIZE = DEFAULT_CHUNK_SIZE + 1;

        public const int DEFAULT_CHUNK_SIZE_POWER = 5;

        public const int MAX_CHUNK_EXTRA_SIZE_POWER = 5;

        public const int DEFAULT_MIN_CHUNK_LOD_POWER = 0;

        public const int MAX_CHUNK_LOD_POWER = 5;

        public const int MAX_CHUNK_LOD_BIT_REPRESENTATION_SIZE = 3;

        public const int DEACTIVATE_CHUNK_LOD_POWER = MAX_CHUNK_LOD_POWER + 1;

        public const int VOXELS_IN_DEFAULT_SIZED_CHUNK = DEFAULT_CHUNK_SIZE * DEFAULT_CHUNK_SIZE * DEFAULT_CHUNK_SIZE;

        public const int NOISE_POINTS_IN_DEFAULT_SIZED_CHUNK = POINTS_PER_AXIS_IN_DEFAULT_SIZE * POINTS_PER_AXIS_IN_DEFAULT_SIZE * POINTS_PER_AXIS_IN_DEFAULT_SIZE;

        public const int MAX_TRIANGLES_IN_CHUNK = VOXELS_IN_DEFAULT_SIZED_CHUNK * 2;


        protected ChunkGroupMesh chunkGroup = new ChunkGroupMesh(CHUNK_GROUP_SIZE_POWER + 1);

        [Save]
        protected StorageGroupMesh storageGroup = new StorageGroupMesh(STORAGE_GROUP_SIZE_POWER + 1);

        protected float[] storedNoiseData;

        private const int maxTrianglesLeft = 5000000;

        public NoiseData noiseData;

        public ComputeShader densityShader;

        public ComputeShader chunkMeshDataShader;

        public ComputeShader cubesPrepare;

        public ComputeShader noiseEditShader;

        public ComputeShader FindNonEmptyChunksShader;

        protected MeshDisplayerPool displayerPool;

        protected InteractableMeshDisplayPool interactableDisplayerPool;

        //Cant really pool noise array, maybe pool tribuilder aray instead (larger than neccessary)

        public static HashSet<CompressedMarchingCubeChunk> channeledChunks = new HashSet<CompressedMarchingCubeChunk>();

        public AsynchronNeighbourFinder neighbourFinder;

        protected bool hasFoundInitialChunk;

        public ChunkUpdateValues updateValues;

        public int totalTriBuild;

        public WorldUpdater worldUpdater;

        public Transform colliderParent;

        private BufferPool minDegreesAtCoordBufferPool;

        public ChunkGenerationPipelinePool chunkPipelinePool;

        protected bool initializationDone;

        public bool useTerrainNoise;


        public int deactivateAfterDistance = 40;

        public Material chunkMaterial;

        protected Vector3 StartPos => new Vector3(0,noiseData.radius,0);

        public int buildAroundDistance = 4000;

        protected long buildAroundSqrDistance;

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

        public static object exchangeLocker = new object();

        protected Vector3 startPos;
        protected float maxSqrChunkDistance;

        protected BinaryHeap<float, ChunkReadyState> closestNeighbours = new BinaryHeap<float, ChunkReadyState>(float.MinValue, float.MaxValue, 200);

        protected Queue<ChunkNeighbourTask> finishedNeighbourTasks = new Queue<ChunkNeighbourTask>();

        public static object neighbourTaskLocker = new object();

        protected Camera mainCam;


        protected int chunkInAsync = 0;

        public int WaitingForAsyncChunkResultCount => chunkInAsync;

        public void AddFinishedTask(ChunkNeighbourTask task)
        {
            lock(neighbourTaskLocker)
            {
                finishedNeighbourTasks.Enqueue(task);
            }
        }

        protected static MarchingCubeChunkHandler instance;

        public override void OnAwake()
        {
            if (instance != null)
            {
                throw new Exception("There can be only one marchingCubesChunkHandler!");
            }
                instance = this;
        }


        public static bool InitialWorldBuildingDone => instance.initializationDone;



        public bool NoWorkOnMainThread =>
            hasFoundInitialChunk &&
            !HasFinishedTask &&
            WaitingForAsyncChunkResultCount <= 0 &&
            closestNeighbours.size <= 0;

        public bool NeighbourBuildingComplete => neighbourFinder.InitializationDone;

        public bool HasFinishedTask => finishedNeighbourTasks.Count > 0;

        protected ChunkNeighbourTask GetFinishedTask()
        {
            ChunkNeighbourTask result;
            lock (neighbourTaskLocker)
            {
                if (finishedNeighbourTasks.Count > 0)
                {
                    result = finishedNeighbourTasks.Dequeue();
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }

        //TODO:GPU instancing from script generated meshes and add simple colliders as game objects

        //TODO: Changing lods on rapid moving character not really working. also mesh vertices error thrown sometimes

        //TODO: Handle chunks spawn with too low lod outside of next level lod collider -> no call to reduce lod

        //Test
        public SpawnGrassForMarchingCube grass;

        public EnvironmentSpawner environmentSpawner;

        public ChunkGPUDataRequest chunkGPURequest;



        private void Start()
        {
            neighbourFinder = new AsynchronNeighbourFinder(this);
            ChunkNeighbourTask.chunkGroup = chunkGroup;

            mainCam = Camera.main;
            noiseData.ApplyNoiseBiomData();

            CreatePools();


            ChunkGPUDataRequest.AssignEmptyMinDegreeBuffer(CreateEmptyMinDegreeBuffer());
            chunkGPURequest = new ChunkGPUDataRequest(chunkPipelinePool, storageGroup, minDegreesAtCoordBufferPool);

            TriangulationTableStaticData.BuildLookUpTables();
            BuildUpdateValues();

            watch.Start();
            buildAroundSqrDistance = (long)buildAroundDistance * buildAroundDistance;
            startPos = StartPos;


            //CompressedMarchingCubeChunk chunk = FindNonEmptyChunkAround(player.position);
            //maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;
            //BuildRelevantChunksParallelBlockingAround(chunk);

            ///uncomment to make work!
            CreatePlanetWithAsyncGPU();

            //CreatePlanetFromMeshData();

            //BuildRelevantChunksParallelWithAsyncGpuAround(chunk);

            //int amount = 5;
            //for (int x = -amount; x <= amount; x++)
            //{
            //    for (int y = -amount; y <= amount; y++)
            //    {
            //        CreateChunkWithAsyncGPUReadbackParallel(startPos + new Vector3(32 * x, -DEFAULT_CHUNK_SIZE, 32 * y));
            //    }
            //}

            //FindNonEmptyChunkAroundAsync(startPos, (chunk) =>
            //{
            //    maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;

            //    BuildRelevantChunksParallelWithAsyncGpuAround(chunk);
            //});
        }

        protected void BuildUpdateValues()
        {
            float distThreshold = 1.1f;
            float chunkDeactivateDist = buildAroundDistance * distThreshold;
            float chunkDestroyDistance = chunkDeactivateDist + chunkGroup.GROUP_SIZE * 2;
            updateValues = new ChunkUpdateValues(500, chunkDestroyDistance,
                new float[] { 250, 500, 1000, 1750, 3000, chunkDeactivateDist }, 1.1f);
            worldUpdater.InitializeUpdateRoutine(updateValues);
        }

        protected Vector3Int[] ScanForNonEmptyChunks()
        {
            ChunkGenerationGPUData.ApplyStaticShaderProperties(FindNonEmptyChunksShader);
            Vector3 chunkStartPos = GlobalPositionToDefaultAnchorPosition(StartPos);
            Vector3Int[] nonEmptyPositions = chunkGPURequest.ScanForNonEmptyChunksAround(chunkStartPos, DEFAULT_CHUNK_SIZE_POWER + 2, DEFAULT_CHUNK_SIZE_POWER);
            ChunkGenerationGPUData.FreeStaticInitialData();
            return nonEmptyPositions;
        }

        protected void CreatePlanetWithAsyncGPU()
        {
            Vector3Int[] positions = ScanForNonEmptyChunks();
            hasFoundInitialChunk = positions.Length > 0;
            if (hasFoundInitialChunk)
            {
                //BuildAllChunksAsync(new Vector3Int[] { positions[0] });
                //OnInitialializationDone();
                BuildAllInitialChunksAsync(positions);
                StartCoroutine(WaitTillAsynGenerationDone());
            }
            else
            {
                FindNonEmptyChunkAroundAsync(startPos, (chunk) =>
                {
                    Time.timeScale = 1;
                    mainCam.enabled = true;
                    BuildNeighbourChunks(chunk);
                    StartCoroutine(WaitTillAsynGenerationDone());
                });
            }
        }

        protected void BuildAllInitialChunksAsync(Vector3Int[] pos)
        {
            for (int i = 0; i < pos.Length; i++)
            {
                CreateChunkWithAsyncGPUReadback(pos[i], FindNeighbourOfChunk);
            }
        }

        //protected void CreatePlanetFromMeshData()
        //{
        //    MeshData data;
        //    CompressedMarchingCubeChunk start = FindNonEmptyChunkAround(startPos, out data);

        //    ChunkNeighbourTask task = new ChunkNeighbourTask(start, data);
        //    task.NeighbourSearch();
        //    BuildRelevantChunksFromMeshDataBlockingAround(task);
        //}



        public List<Action> OnInitializationDoneCallback = new List<Action>();

        protected void OnInitialializationDone()
        {
            worldUpdater.ClearLeafesWithoutValue();
            initializationDone = true;

            foreach (var item in OnInitializationDoneCallback)
            {
                item();
            }
        }

        protected IEnumerator WaitTillAsynGenerationDone()
        {
            Time.timeScale = 0;
            mainCam.enabled = false;

            bool repeat = true;
            List<Exception> x = CompressedMarchingCubeChunk.xs;

            while (repeat)
            {
                ChunkReadyState next;
                bool isNextInProgress;


                if (totalTriBuild < maxTrianglesLeft)
                {

                    //TODO: while waiting create mesh displayers! -> leads to worse performance?
                    while (finishedNeighbourTasks.Count > 0)
                    {
                        ChunkNeighbourTask task = GetFinishedTask();
                        BuildNeighbourChunks(task);
                    }
                }

                while (closestNeighbours.size > 0)
                {
                    do
                    {
                        next = closestNeighbours.Dequeue();
                        isNextInProgress = next.lastParent.HasChildAtIndex(next.lastParentsChildIndex);
                    } while (isNextInProgress && closestNeighbours.size > 0);

                    if (!isNextInProgress)
                    {
                        CreateChunkWithAsyncGPUReadback(next, FindNeighbourOfChunk);
                    }
                }

                if (neighbourFinder.InitializationDone)
                {
                    repeat= false;
                    Time.timeScale = 1;
                    mainCam.enabled = true;

                    watch.Stop();
                    Debug.Log("Total millis: " + watch.Elapsed.TotalMilliseconds);
                    if (totalTriBuild >= maxTrianglesLeft)
                    {
                        Debug.Log("Aborted");
                    }
                    //Debug.Log("Total triangles: " + totalTriBuild);
                    OnInitialializationDone();
                }
                yield return null;
            }
        }


        //Todo: try do this work on compute shader already
        //private void BuildRelevantChunksWithAsyncGpuAround()
        //{
        //    Vector3Int next;
        //    bool isNextInProgress;
        //    while (closestNeighbours.size > 0)
        //    {
        //        do
        //        {
        //            next = closestNeighbours.Dequeue();
        //            isNextInProgress = chunkGroup.HasLeafAtGlobalPosition(VectorExtension.ToArray(next));
        //        } while (isNextInProgress && closestNeighbours.size > 0);


        //        if (!isNextInProgress)
        //        {
        //            CreateChunkWithAsyncGPUReadback(next, FindNeighbourOfChunk);
        //        }

        //    }
        //}

        public void FindNeighbourOfChunk(CompressedMarchingCubeChunk c)
        {
            if(!c.MeshData.IsEmpty)
                neighbourFinder.AddTask(new ChunkNeighbourTask(c, c.MeshData));
        }


        //public void BuildRelevantChunksFromMeshDataBlockingAround(ChunkNeighbourTask task)
        //{
        //    bool[] dirs = task.HasNeighbourInDirection;
        //    CompressedMarchingCubeChunk chunk = task.chunk;
        //    int count = dirs.Length;
        //    for (int i = 0; i < count; ++i)
        //    {
        //        if (!dirs[i])
        //            continue;

        //        Vector3Int v3 = VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
        //        closestNeighbours.Enqueue(0, v3);
        //        ///for initial neighbours build additional chunks to not just wait for first thread to be done
        //        ///seems to worsen performance?
        //        //v3 = 2 * VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
        //        //closestNeighbours.Enqueue(0, v3);

        //        //v3 = 3 * VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
        //        //closestNeighbours.Enqueue(0, v3);
        //    }
        //    if (closestNeighbours.size > 0)
        //    {
        //        BuildRelevantChunksFromMeshDataBlockingAround();
        //    }

        //    watch.Stop();
        //    Debug.Log("Total millis: " + watch.Elapsed.TotalMilliseconds);
        //    if (totalTriBuild >= maxTrianglesLeft)
        //    {
        //        Debug.Log("Aborted");
        //    }
        //    Debug.Log("Total triangles: " + totalTriBuild);
        //    OnInitialializationDone();
        //    // Debug.Log($"Number of chunks: {ChunkGroups.Count}");
        //}

        //private void BuildRelevantChunksFromMeshDataBlockingAround()
        //{
        //    List<Exception> x = CompressedMarchingCubeChunk.xs;
        //    Vector3Int next;
        //    MeshData data;
        //    CompressedMarchingCubeChunk chunk;

        //    bool isNextInProgress;
        //    while (!NeighbourBuildingComplete)
        //    {
        //        do
        //        {
        //            next = closestNeighbours.Dequeue();
        //            isNextInProgress = chunkGroup.HasLeafAtGlobalPosition(VectorExtension.ToArray(next));
        //        } while (isNextInProgress && closestNeighbours.size > 0);


        //        if (!isNextInProgress)
        //        {
        //            chunk = CreateChunkAt(next, out data);
        //            FindNeighbourOfChunk(chunk);
        //        }
        //        if (totalTriBuild < maxTrianglesLeft)
        //        {
        //            while (closestNeighbours.size == 0 && !NeighbourBuildingComplete)
        //            {
        //                //TODO: while waiting create mesh displayers! -> leads to worse performance?
        //                while (HasFinishedTask)
        //                {
        //                    BuildNeighbourChunks(GetFinishedTask());
        //                }
        //            }
        //        }
        //    }
        //}

        public void BuildNeighbourChunks(CompressedMarchingCubeChunk chunk)
        {
            //TODO: FINISH awdda
        }


        public void BuildNeighbourChunks(ChunkNeighbourTask task)
        {
            bool[] dirs = task.HasNeighbourInDirection;
            ChunkDirectionSearchState[] searches = task.searchedNeighbours;
            for (int i = 0; i < ChunkNeighbourTask.MAX_NEIGHBOUR_CHUNKS; ++i)
            {
                if (!dirs[i])
                    continue;

                ChunkDirectionSearchState searchState = searches[i];

                bool canBuildNeighbour = searchState.ContinueFollowPathBuildingNodesToEmptyLeafPosition(out ChunkReadyState readyState);

                if (canBuildNeighbour)
                {
                    float sqrDist = (startPos - readyState.lastParent.GetChildCenterPositionAtIndex(readyState.lastParentsChildIndex)).sqrMagnitude;
                    if(updateValues.ShouldChunkBeDestroyed(sqrDist))
                    {
                        continue;
                    }
                    else if (updateValues.ShouldChunkBeDeactivated(sqrDist))
                    {
                        BuildSpawnerInDirection(task.chunk, (Direction)i);
                    }
                    else
                    {
                        closestNeighbours.Enqueue(sqrDist, readyState);
                    }
                }
            }
        }

        public void BuildSpawnersAround(CompressedMarchingCubeChunk chunk)
        {
            for (int i = 0; i < ChunkNeighbourTask.MAX_NEIGHBOUR_CHUNKS; ++i)
            {
                BuildSpawnerInDirection(chunk, (Direction)i);
            }
        }

        protected void BuildSpawnerInDirection(CompressedMarchingCubeChunk chunk, Direction d)
        {
            chunkGroup.CreateEmptyChunkGroupAdjacentTo(chunk, d);
        }

        protected bool lastChunkWasAir = true;

        protected CompressedMarchingCubeChunk FindNonEmptyChunkAround(Vector3 pos, out MeshData data)
        {
            bool isEmpty = true;
            CompressedMarchingCubeChunk chunk = null;
            int tryCount = 0;
            data = default;
            //TODO:Remove trycount later
            while (isEmpty && tryCount++ < 100)
            {
                chunk = CreateChunkAt(pos, out data);
                isEmpty = chunk.IsEmpty;
                if (chunk.IsEmpty)
                {
                    //TODO: maybe just read noise points here and completly remove isSolid or Air
                    if (lastChunkWasAir)
                    {
                        pos.y -= chunk.ChunkSize;
                    }
                    else
                    {
                        pos.y += chunk.ChunkSize;
                    }
                }
            }
            hasFoundInitialChunk = true;
            return chunk;
        }

        protected void FindNonEmptyChunkAroundAsync(Vector3 pos, Action<CompressedMarchingCubeChunk> callback)
        {
            Time.timeScale = 0;
            mainCam.enabled = false;
            FindNonEmptyChunkAroundAsync(pos, callback, 0);
        }

        protected void FindNonEmptyChunkAroundAsync(Vector3 pos, Action<CompressedMarchingCubeChunk> callback, int tryCount)
        {
            //TODO:Remove trycount later
            if (tryCount++ >= 100)
                return;

            CreateChunkWithAsyncGPUReadback(pos, (c) => CheckChunk(c, callback, tryCount, ref pos));
        }

        protected void CheckChunk(CompressedMarchingCubeChunk chunk, Action<CompressedMarchingCubeChunk> callback, int tryCount, ref Vector3 pos)
        {
            if (chunk.IsEmpty)
            {
                //TODO: maybe just read noise points here and completly remove isSolid or Air
                if (lastChunkWasAir)
                {
                    pos.y -= chunk.ChunkSize;
                }
                else
                {
                    pos.y += chunk.ChunkSize;
                }
                FindNonEmptyChunkAroundAsync(pos, callback, tryCount);
            }
            else
            {
                hasFoundInitialChunk = true;
                callback(chunk);
            }
        }


        /// <summary>
        /// returns true if the chunk was created
        /// </summary>
        /// <param name="p"></param>
        /// <param name="editPoint"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="delta"></param>
        /// <param name="maxDistance"></param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public void CreateChunkWithNoiseEdit(ChunkReadyState readyState, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance, out CompressedMarchingCubeChunk chunk)
        {
            bool hasChunkAtPosition = readyState.HasLeaf(out chunk);
            if (chunk.LODPower > 0)
                throw new ArgumentException("Leaf must be of lod 0 to be edited!");
            if (!hasChunkAtPosition)
            {
                ReducedMarchingCubesChunk newChunk = GetThreadedChunkObjectAtChildIndex(readyState.lastParent,readyState.lastParentsChildIndex);
                BuildChunkMeshData(newChunk,
                    (b) => {
                        ApplyNoiseEditing(b, editPoint, start, end, delta, maxDistance);
                    });
            }
        }


        //TODO:Check if collider can be removed from most chunks.
        //Collision can be approximated by calling noise function for lowest point of object and checking if its noise is larger than surface value

        protected CompressedMarchingCubeChunk CreateChunkAt(Vector3 pos, out MeshData data, bool allowOverride = false)
        {
            GetSizeAndLodPowerForChunkPosition(pos, out int chunkSizePower, out int lodPower);
            return CreateChunkWithProperties(VectorExtension.ToVector3Int(pos), lodPower, chunkSizePower, allowOverride, out data);
        }

        protected CompressedMarchingCubeChunk CreateChunkAt(Vector3 pos, bool allowOverride = false)
        {
            GetSizeAndLodPowerForChunkPosition(pos, out int chunkSizePower, out int lodPower);
            return CreateChunkWithProperties(VectorExtension.ToVector3Int(pos), lodPower, chunkSizePower, allowOverride);
        }

        protected CompressedMarchingCubeChunk CreateChunkWithProperties(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride, Action<ComputeBuffer> WorkOnNoise = null)
        {
            return CreateChunkWithProperties(pos, lodPower, chunkSizePower, allowOverride, out MeshData _, WorkOnNoise);
        }

        protected CompressedMarchingCubeChunk CreateChunkWithProperties(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride, out MeshData data, Action<ComputeBuffer> WorkOnNoise = null)
        {
            CompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(pos, lodPower, chunkSizePower, allowOverride);
            data = BuildChunkMeshData(chunk, WorkOnNoise);
            return chunk;
        }

        protected void CreateChunkWithAsyncGPUReadback(Vector3 pos, Action<CompressedMarchingCubeChunk> callback, bool allowOverride = false)
        {
            GetSizeAndLodPowerForChunkPosition(pos, out int chunkSizePower, out int lodPower);
            CreateChunkWithPropertiesAsync(VectorExtension.ToVector3Int(pos), lodPower, chunkSizePower, allowOverride, callback);
        }

        protected void CreateChunkWithAsyncGPUReadback(ChunkReadyState readyChunkParent, Action<CompressedMarchingCubeChunk> callback, bool allowOverride = false)
        {
            CompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAtParent(readyChunkParent);
            BuildChunkAsyncFromMeshData(chunk, callback);
        }

        protected void CreateChunkWithPropertiesAsync(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride, Action<CompressedMarchingCubeChunk> callback)
        {
            CompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(pos, lodPower, chunkSizePower, allowOverride);
            BuildChunkAsyncFromMeshData(chunk, callback);
        }

        protected void GetChunkObjectAt(CompressedMarchingCubeChunk chunk, Vector3Int position, int lodPower, int chunkSizePower, bool allowOverride)
        {
            chunk.NodeSizePower = chunkSizePower;
            chunk.LODPower = lodPower;
            InitializeNonEmptyChunk(chunk);

            chunkGroup.SetValueAtGlobalPosition(position, chunk, allowOverride);
        }

        protected void InitializeNonEmptyChunk(CompressedMarchingCubeChunk chunk)
        {
            chunk.HasStarted = true;
            chunk.ChunkHandler = this;
            chunk.ChunkUpdater = worldUpdater;
            chunk.Material = chunkMaterial;
        }


        protected void InitializeChunkObject(CompressedMarchingCubeChunk chunk, ChunkGroupTreeNode node)
        {
            chunk.NodeSizePower = node.SizePower;
            chunk.LODPower = GetLodPowerFromSizePower(node.SizePower);
            InitializeNonEmptyChunk(chunk);

            node.Parent.SetLeafAtIndex(chunk, node.Index);
        }

        protected void InitializeChunkAtChildIndex(CompressedMarchingCubeChunk chunk, ChunkReadyState readyState)
        {
            InitializeChunkAtChildIndex(chunk, readyState.lastParent, readyState.lastParentsChildIndex);
        }

        protected void InitializeChunkAtChildIndex(CompressedMarchingCubeChunk chunk, ChunkGroupTreeNode node, int childIndex)
        {
            chunk.NodeSizePower = node.SizePower - 1;
            chunk.LODPower = GetLodPowerFromSizePower(node.SizePower - 1);
            InitializeNonEmptyChunk(chunk);

            node.SetLeafAtIndex(chunk, childIndex);
        }

        protected CompressedMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int position, int lodPower, int chunkSizePower, bool allowOverride)
        {
            CompressedMarchingCubeChunk chunk = GetMarchingCubeChunkFromLodPower(lodPower);
            GetChunkObjectAt(chunk, position, lodPower, chunkSizePower, allowOverride);
            return chunk;
        }

        protected CompressedMarchingCubeChunk GetMarchingCubeChunkFromLodPower(int lodPower)
        {
            if (lodPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
                return new ReducedMarchingCubesChunk();
            else
                return new CompressedMarchingCubeChunk();
        }

        protected void AddThreadedChunkObjectsAtEmptyChildPosition(ChunkGroupTreeNode node, List<CompressedMarchingCubeChunk> newChunkList)
        {
            for (int i = 0; i < 8; i++)
            {
                if(node.children[i] == null)
                {
                    newChunkList.Add(GetThreadedChunkObjectAtChildIndex(node, i));
                }
            }
        }

        protected CompressedMarchingCubeChunk GetThreadedCompressedChunkObjectAt(ChunkGroupTreeNode node)
        {
            CompressedMarchingCubeChunk c = new CompressedMarchingCubeChunk();
            InitializeChunkObject(c, node);
            return c;
        }

        protected ReducedMarchingCubesChunk GetThreadedChunkObjectAtChildIndex(ChunkGroupTreeNode node, int childIndex)
        {
            ReducedMarchingCubesChunk c = new ReducedMarchingCubesChunk();
            InitializeChunkAtChildIndex(c, node, childIndex);
            return c;
        }

        protected CompressedMarchingCubeChunk GetThreadedChunkObjectAtParent(ChunkReadyState state)
        {
            CompressedMarchingCubeChunk c = GetMarchingCubeChunkFromLodPower(state.lastParent.LodPower - 1);
            InitializeChunkAtChildIndex(c, state.lastParent, state.lastParentsChildIndex);
            return c;
        }

        protected void SetChunkComponents(CompressedMarchingCubeChunk chunk)
        {
            SetDisplayerOfChunk(chunk);
        }

        //public MarchingCubeChunkNeighbourLODs GetNeighbourLODSFrom(ReducedMarchingCubesChunk chunk)
        //{
        //    MarchingCubeChunkNeighbourLODs result = new MarchingCubeChunkNeighbourLODs();
        //    Vector3Int[] coords = VectorExtension.GetAllAdjacentDirections;
        //    for (int i = 0; i < coords.Length; ++i)
        //    {
        //        MarchingCubeNeighbour neighbour = new MarchingCubeNeighbour();
        //        Vector3Int neighbourPos = chunk.CenterPos + chunk.ChunkSize * coords[i];
        //        if (!TryGetChunkAtPosition(neighbourPos, out neighbour.chunk))
        //        {
        //            //change name to extectedLodPower
        //            neighbour.estimatedLodPower = GetLodPowerAt(neighbourPos);
        //        }
        //        result[i] = neighbour;
        //    }

        //    return result;
        //}


        //TODO:Remove keep points
        //protected void BuildChunk(CompressedMarchingCubeChunk chunk, Action<ComputeBuffer> WorkOnNoise = null)
        //{
        //    TriangleChunkHeap ts = chunkGPURequest.DispatchAndGetTriangleData(chunk, SetChunkComponents, WorkOnNoise);
        //    chunk.InitializeWithTriangleData(ts);
        //}


        protected MeshData BuildChunkMeshData(CompressedMarchingCubeChunk chunk, Action<ComputeBuffer> WorkOnNoise = null)
        {
            channeledChunks.Add(chunk);
            MeshData data = chunkGPURequest.DispatchAndGetChunkMeshData(chunk, SetChunkComponents, WorkOnNoise);
            chunk.InitializeWithMeshData(data);
            return data;
        }

        private void ApplyNoiseEditing(ComputeBuffer noiseBuffer, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance)
        {
            SetNoiseEditProperties(noiseBuffer,editPoint, start, end, delta, maxDistance);
            int threadsPerAxis = Mathf.CeilToInt(POINTS_PER_AXIS_IN_DEFAULT_SIZE / THREAD_GROUP_SIZE);
            noiseEditShader.Dispatch(0, threadsPerAxis, threadsPerAxis, threadsPerAxis);
        }

        private void SetNoiseEditProperties(ComputeBuffer noiseBuffer, Vector3 editPoint, Vector3 start, Vector3 end, float delta, float maxDistance)
        {
            noiseEditShader.SetBuffer(0,"points", noiseBuffer);

            noiseEditShader.SetVector("clickPoint", editPoint);
            noiseEditShader.SetVector("start", start);
            noiseEditShader.SetVector("end", end);
            noiseEditShader.SetFloat("delta", delta);
            noiseEditShader.SetFloat("maxDistance", maxDistance);
        }


        //TODO: Maybe pool theese for fewer pipeline instances
        protected void DispatchMultipleChunksAsync(CompressedMarchingCubeChunk[] chunks, Action<CompressedMarchingCubeChunk> callbackPerChunk)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                PrepareChunkAsyncFromMeshData(chunks[i], callbackPerChunk);
            }
        }

        protected void DispatchMultipleChunksAsync(List<CompressedMarchingCubeChunk> chunks, Action<CompressedMarchingCubeChunk> callbackPerChunk)
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                PrepareChunkAsyncFromMeshData(chunks[i], callbackPerChunk);
            }
        }

        protected void BuildChunkAsyncFromMeshData(CompressedMarchingCubeChunk chunk, Action<CompressedMarchingCubeChunk> onChunkDone)
        {
            chunkInAsync++;
            chunkGPURequest.DispatchAndGetChunkMeshDataAsync(chunk, SetChunkComponents, (data) =>
                {
                    chunk.InitializeWithMeshData(data);
                    onChunkDone(chunk);
                    chunkInAsync--;
                });
        }

        protected void PrepareChunkAsyncFromMeshData(CompressedMarchingCubeChunk chunk, Action<CompressedMarchingCubeChunk> onChunkDone)
        {
            chunkInAsync++;
            chunkGPURequest.DispatchAndGetChunkMeshDataAsync(chunk, SetChunkComponents, (data) =>
            {
                chunk.PrepareInitializationWithMeshData(data);
                worldUpdater.AddChunkToInitialize(new ChunkInitializeTask(onChunkDone, chunk));
                chunkInAsync--;
            });
        }

        protected void ExchangeChunkAsyncParallel(Vector3Int anchorPos, int lodPow, int sizePow, bool allowOveride, Action<CompressedMarchingCubeChunk> onChunkDone)
        {
            CompressedMarchingCubeChunk newChunk = GetThreadedChunkObjectAt(anchorPos, lodPow, sizePow, allowOveride);
            PrepareChunkAsyncFromMeshData(newChunk, onChunkDone);
        }

        protected void ExchangeChunkAsyncParallel(ChunkGroupTreeNode node, Action<CompressedMarchingCubeChunk> onChunkDone)
        {
            CompressedMarchingCubeChunk newChunk = GetThreadedCompressedChunkObjectAt(node);
            PrepareChunkAsyncFromMeshData(newChunk, onChunkDone);
        }


        protected Vector3Int IntArrToVector3(int[] arr) => new Vector3Int(arr[0], arr[1], arr[2]);

        //protected int NumberOfSavedChunksAt(Vector3Int pos, int sizePow)
        //{
        //    Vector3Int coord = PositionToStorageGroupCoord(pos);
        //    StorageTreeRoot r;
        //    if (storageGroups.TryGetValue(coord, out r))
        //    {
        //        IStorageGroupOrganizer<StoredChunkEdits> node;
        //        if (r.TryGetNodeWithSizePower(new int[] { pos.x, pos.y, pos.z }, sizePow, out node))
        //        {
        //            return node.ChildrenWithMipMapReady;
        //        }
        //    }
        //    return 0;
        //}

        public void MergeAndReduceChunkNode(ChunkGroupTreeNode node)
        {
            List<CompressedMarchingCubeChunk> oldChunks = new List<CompressedMarchingCubeChunk>();
            node.PrepareBranchDestruction(oldChunks);
            int lodPower = GetLodPowerFromSizePower(node.SizePower);
            ExchangeChunkAsyncParallel(node, (c) =>
            {
                c.BakeMeshe sIfUsingCollider();
                lock (exchangeLocker)
                {
                    worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(oldChunks, c));
                }
            });
        }

        public void SplitChunkLeaf(ChunkSplitExchange exchange)
        {
            CompressedMarchingCubeChunk oldChunk = exchange.leaf.leaf;
            oldChunk.PrepareDestruction();

            int exchangeCount = exchange.newNodes.Count;
            int newLeafs = exchangeCount * 8 - (exchangeCount - 1);

            List<CompressedMarchingCubeChunk> newChunks = new List<CompressedMarchingCubeChunk>(newLeafs);
            
            for (int i = 0; i < exchangeCount; i++)
            {
                AddThreadedChunkObjectsAtEmptyChildPosition(exchange.newNodes[i], newChunks);
            }
            object listLock = new object();

            List<CompressedMarchingCubeChunk> chunks = new List<CompressedMarchingCubeChunk>();

            Action<CompressedMarchingCubeChunk> f = (c) =>
            {
                //c.Hide();
                lock (listLock)
                {
                    chunks.Add(c);
                }
                c.BakeMes hesIfUsingCollider();
                if (chunks.Count == newChunks.Count)
                {
                    lock (exchangeLocker)
                    {
                        worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(oldChunk, chunks, exchange.newNodes));
                    }
                }
            };

            DispatchMultipleChunksAsync(newChunks, f);
        }

        public MarchingCubeMeshDisplayer GetNextMeshDisplayer()
        {
            return displayerPool.GetItemFromPoolFor(null);
        }

        public MarchingCubeMeshDisplayer GetNextInteractableMeshDisplayer(ReducedMarchingCubesChunk chunk)
        {
            return interactableDisplayerPool.GetItemFromPoolFor(chunk);
        }

        protected void SetDisplayerOfChunk(CompressedMarchingCubeChunk c)
        {
            if (c is ReducedMarchingCubesChunk interactableChunk)
            {
                interactableChunk.AddDisplayer(GetNextInteractableMeshDisplayer(interactableChunk));
            }
            else
            {
                c.AddDisplayer(GetNextMeshDisplayer());
            }
        }

        public void TakeMeshDisplayerBack(MarchingCubeMeshDisplayer display)
        {
            if (display.HasCollider)
            {
                interactableDisplayerPool.ReturnItemToPool(display);
            }
            else
            {
                displayerPool.ReturnItemToPool(display);
            }
        }

        public void FreeAllDisplayers(List<MarchingCubeMeshDisplayer> displayers)
        {
            for (int i = 0; i < displayers.Count; ++i)
            {
                TakeMeshDisplayerBack(displayers[i]);
            }
        }

        public int GetLodPower(float sqrDistance)
        {
            return updateValues.GetLodPowerForSqrDistance(sqrDistance);
        }


        public void GetSizeAndLodPowerForChunkPosition(Vector3 pos, out int sizePower, out int lodPower)
        {
            float sqrDistance = (startPos - pos).sqrMagnitude;
            lodPower = GetLodPower(sqrDistance);
            sizePower = GetSizePowerFromLodPower(lodPower);
            //TODO: check this
        }

        protected int GetSizePowerFromLodPower(int lodPower) => lodPower + DEFAULT_CHUNK_SIZE_POWER;
        protected int GetSizeFromLodPower(int lodPower) => RoundToPowerOf2(lodPower + DEFAULT_CHUNK_SIZE_POWER);
        protected int GetLodPowerFromSizePower(int sizePower) => sizePower - DEFAULT_CHUNK_SIZE_POWER;

        protected int RoundToPowerOf2(int f)
        {
            return (int)Mathf.Pow(2, f);
        }

        protected void CreatePools()
        {
            chunkPipelinePool = new ChunkGenerationPipelinePool(CreateChunkPipeline);
            minDegreesAtCoordBufferPool = new BufferPool(CreateMinDegreeBuffer);

            displayerPool = new MeshDisplayerPool(transform);
            interactableDisplayerPool = new InteractableMeshDisplayPool(transform);
        }

        protected ChunkGenerationGPUData CreateChunkPipeline()
        {
             ChunkGenerationGPUData result = new ChunkGenerationGPUData();

            //TODO: Test shader variant collection less shader variant loading time (https://docs.unity3d.com/ScriptReference/ShaderVariantCollection.html)

            result.densityGeneratorShader = Instantiate(densityShader);
            result.prepareTrisShader = Instantiate(cubesPrepare);
            result.buildMeshDataShader = Instantiate(chunkMeshDataShader);

            result.triCountBuffer = CreateCopyCountBuffer();
            result.pointsBuffer = CreatePointsBuffer();
            result.savedPointsBuffer = CreatePointsBuffer();
            result.preparedTrisBuffer = CreatePrepareTriangleBuffer();

            result.ApplyStaticProperties();

            return result;
        }

        protected ComputeBuffer CreateMinDegreeBuffer()
        {
            return new ComputeBuffer(VOXELS_IN_DEFAULT_SIZED_CHUNK, sizeof(float));
        }
        protected ComputeBuffer CreateEmptyMinDegreeBuffer()
        {
            return new ComputeBuffer(1, sizeof(float));
        }
        protected ComputeBuffer CreateCopyCountBuffer()
        {
            return new ComputeBuffer(MAX_CHUNKS_PER_ITERATION, sizeof(int), ComputeBufferType.Raw);
        }

        protected ComputeBuffer CreatePointsBuffer()
        {
            return new ComputeBuffer(NOISE_POINTS_IN_DEFAULT_SIZED_CHUNK, sizeof(float));
        }

        protected ComputeBuffer CreatePrepareTriangleBuffer()
        {
            return new ComputeBuffer(VOXELS_IN_DEFAULT_SIZED_CHUNK, sizeof(int) * 2, ComputeBufferType.Append);
        }

        protected const int MAX_CHUNKS_PER_ITERATION = 1;

        protected void ReleaseBuffers()
        {
            chunkPipelinePool.DisposeAll();
            minDegreesAtCoordBufferPool.DisposeAll();
            ChunkGPUDataRequest.DisposeEmptyMinDegreeBuffer();
        }

        protected override void onDestroy()
        {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }
        }

        //TODO: Dont store when chunk knows he stored before

        public void ComputeGrassFor(IEnvironmentSurface environmentChunk)
        {
            grass.ComputeGrassFor(environmentChunk);
        }

        public void ReturnMinDegreeBuffer(ComputeBuffer minDegreeBuffer)
        {
            minDegreesAtCoordBufferPool.ReturnItemToPool(minDegreeBuffer);
        }

        protected Vector3 GlobalPositionToDefaultAnchorPosition(Vector3 globalPos)
        {
            return new Vector3(((int)(globalPos.x / DEFAULT_CHUNK_SIZE)) * DEFAULT_CHUNK_SIZE,
                ((int)(globalPos.y / DEFAULT_CHUNK_SIZE)) * DEFAULT_CHUNK_SIZE,
                ((int)(globalPos.z / DEFAULT_CHUNK_SIZE)) * DEFAULT_CHUNK_SIZE);
        }

        public void StartEnvironmentPipelineForChunk(IEnvironmentSurface environmentChunk)
        {
            //grass.ComputeGrassFor(environmentChunk);
            //environmentSpawner.AddEnvironmentForOriginalChunk(environmentChunk);
        }

        public void Store(Vector3Int anchorPos, ReducedMarchingCubesChunk chunk, bool overrideNoise = false) => storageGroup.Store(anchorPos, chunk, overrideNoise);

        public bool TryLoadPoints(CompressedMarchingCubeChunk marchingCubeChunk, out float[] loadedPoints) => storageGroup.TryLoadPoints(marchingCubeChunk,out loadedPoints);

        public float[] RequestNoiseForChunk(CompressedMarchingCubeChunk chunk)
        {
            return chunkGPURequest.RequestNoiseForChunk(chunk);
        }

        public MeshData DispatchRebuildAround(ReducedMarchingCubesChunk chunk, Vector3Int clickedIndex, Vector3 startVec, Vector3 endVec, float marchSquare)
        {
            return chunkGPURequest.DispatchAndGetChunkMeshData(chunk, null);
        }

    }
}
