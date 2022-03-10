using MeshGPUInstanciation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using IChunkGroupRoot = MarchingCubes.IChunkGroupRoot<MarchingCubes.CompressedMarchingCubeChunk>;
using StorageGroup = MarchingCubes.GroupMesh<MarchingCubes.StorageTreeRoot, MarchingCubes.StoredChunkEdits, MarchingCubes.StorageTreeLeaf, MarchingCubes.IStorageGroupOrganizer<MarchingCubes.StoredChunkEdits>>;


namespace MarchingCubes
{
    //TODO: Check to use unity mathematics int2, int3 instead of vector for better performance?
    //TODO: When creating a chunk while editing, call getnoise with click changes to only generate noise once

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

        public const int CHUNK_GROUP_SIZE = 1024;

        public const int CHUNK_GROUP_SIZE_POWER = 10;

        public const int DEFAULT_CHUNK_SIZE = 32;

        public const int POINTS_PER_AXIS_IN_DEFAULT_SIZE = DEFAULT_CHUNK_SIZE + 1;

        public const int DEFAULT_CHUNK_SIZE_POWER = 5;

        public const int DEFAULT_MIN_CHUNK_LOD_POWER = 0;

        public const int MAX_CHUNK_LOD_POWER = 5;

        public const int MAX_CHUNK_LOD_BIT_REPRESENTATION_SIZE = 3;

        public const int DESTROY_CHUNK_LOD = MAX_CHUNK_LOD_POWER + 2;

        public const int DEACTIVATE_CHUNK_LOD = MAX_CHUNK_LOD_POWER + 1;

        public const int VOXELS_IN_DEFAULT_SIZED_CHUNK = DEFAULT_CHUNK_SIZE * DEFAULT_CHUNK_SIZE * DEFAULT_CHUNK_SIZE;

        public const int NOISE_POINTS_IN_DEFAULT_SIZED_CHUNK = POINTS_PER_AXIS_IN_DEFAULT_SIZE * POINTS_PER_AXIS_IN_DEFAULT_SIZE * POINTS_PER_AXIS_IN_DEFAULT_SIZE;

        public const int MAX_TRIANGLES_IN_CHUNK = VOXELS_IN_DEFAULT_SIZED_CHUNK * 2;


        protected ChunkGroupMesh chunkGroup = new ChunkGroupMesh(CHUNK_GROUP_SIZE);

        [Save]
        protected StorageGroupMesh storageGroup = new StorageGroupMesh(STORAGE_GROUP_SIZE);

        protected float[] storedNoiseData;

        [Range(1, 253)]
        public int blockAroundPlayer = 16;

        private const int maxTrianglesLeft = 5000000;

        public NoiseData noiseData;

        public ComputeShader densityShader;

        public ComputeShader chunkMeshDataShader;

        public ComputeShader cubesPrepare;

        public ComputeShader noiseEditShader;



        public AnimationCurve lodPowerForDistances;

        public AnimationCurve chunkSizePowerForDistances;

        protected MeshDisplayerPool displayerPool;

        protected InteractableMeshDisplayPool interactableDisplayerPool;

        protected SimpleChunkColliderPool simpleChunkColliderPool;

        //Cant really pool noise array, maybe pool tribuilder aray instead (larger than neccessary)

        public static HashSet<CompressedMarchingCubeChunk> channeledChunks = new HashSet<CompressedMarchingCubeChunk>();

        public AsynchronNeighbourFinder neighbourFinder;

        protected bool hasFoundInitialChunk;


        public int totalTriBuild;

        float[] pointsArray;

        public WorldUpdater worldUpdater;

        public Transform colliderParent;

        private BufferPool minDegreesAtCoordBufferPool;

        public ChunkGenerationPipelinePool chunkPipelinePool;


        public bool useTerrainNoise;


        public int deactivateAfterDistance = 40;

        public Material chunkMaterial;

        public Transform player;

        public int buildAroundDistance = 2;

        protected long buildAroundSqrDistance;

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

        public static object exchangeLocker = new object();

        protected Vector3 startPos;
        protected float maxSqrChunkDistance;

        protected BinaryHeap<float, Vector3Int> closestNeighbours = new BinaryHeap<float, Vector3Int>(float.MinValue, float.MaxValue, 200);

        protected Queue<ChunkNeighbourTask> finishedNeighbourTasks = new Queue<ChunkNeighbourTask>();

        public static object neighbourTaskLocker = new object();



        protected int chunkInAsync = 0;

        public int WaitingForAsyncChunkResultCount => chunkInAsync;

        public void AddFinishedTask(ChunkNeighbourTask task)
        {
            lock(neighbourTaskLocker)
            {
                finishedNeighbourTasks.Enqueue(task);
            }
        }



        public bool NoWorkOnMainThread =>
            hasFoundInitialChunk &&
            !HasFinishedTask &&
            WaitingForAsyncChunkResultCount <= 0 &&
            closestNeighbours.size <= 0;

        public bool InitializationComplete => neighbourFinder.InitializationDone;

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

            chunkGPURequest = new ChunkGPUDataRequest(chunkPipelinePool, storageGroup, minDegreesAtCoordBufferPool);

            TriangulationTableStaticData.BuildLookUpTables();


            watch.Start();
            buildAroundSqrDistance = (long)buildAroundDistance * buildAroundDistance;
            startPos = player.position;


            //CompressedMarchingCubeChunk chunk = FindNonEmptyChunkAround(player.position);
            //maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;
            //BuildRelevantChunksParallelBlockingAround(chunk);

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

        protected void CreatePlanetWithAsyncGPU()
        {
            FindNonEmptyChunkAroundAsync(startPos, (chunk, data) =>
            {
                //neighbourFinder.MaxOutRunningThreads();
                //ChunkNeighbourTask task = new ChunkNeighbourTask(chunk, data);
                //task.FindNeighbours();
                //BuildNeighbourChunks(task);

                BuildNeighbourChunks(new bool[] { true, true, true, true, true, true }, chunk.ChunkSize, chunk.CenterPos);

                //BuildRelevantChunksWithAsyncGpuAround();
                StartCoroutine(WaitTillAsynGenerationDone());
            });
        }

        protected void CreatePlanetFromMeshData()
        {
            MeshData data;
            CompressedMarchingCubeChunk start = FindNonEmptyChunkAround(startPos, out data);

            ChunkNeighbourTask task = new ChunkNeighbourTask(start, data);
            task.NeighbourSearch();
            BuildRelevantChunksFromMeshDataBlockingAround(task);
        }

        Camera mainCam;

        protected IEnumerator WaitTillAsynGenerationDone()
        {
            bool repeat = true;
            List<Exception> x = CompressedMarchingCubeChunk.xs;

            while (repeat)
            {
                Vector3 next;
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
                        isNextInProgress = chunkGroup.HasChunkStartedAt(VectorExtension.ToArray(next));
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
                    Debug.Log("Total triangles: " + totalTriBuild);

                }
                yield return null;
            }
        }

        public void BuildRelevantChunksWithAsyncGpuAround(CompressedMarchingCubeChunk chunk)
        {
            mainCam.enabled = false;
            Time.timeScale = 0;


            bool[] dirs = chunk.HasNeighbourInDirection;
            int count = dirs.Length;
            for (int i = 0; i < count; ++i)
            {
                if (!dirs[i])
                    continue;


                Vector3Int v3 = VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
                closestNeighbours.Enqueue(0, v3);
                ///for initial neighbours build additional chunks to not just wait for first thread to be done
                ///seems to worsen performance?
                //v3 = 2 * VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
                //closestNeighbours.Enqueue(0, v3);

                //v3 = 3 * VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
                //closestNeighbours.Enqueue(0, v3);
            }
            if (closestNeighbours.size > 0)
            {
                BuildRelevantChunksWithAsyncGpuAround();
                StartCoroutine(WaitTillAsynGenerationDone());
            }

            // Debug.Log($"Number of chunks: {ChunkGroups.Count}");
        }

        //Todo: try do this work on compute shader already
        private void BuildRelevantChunksWithAsyncGpuAround()
        {
            Vector3Int next;
            bool isNextInProgress;
            while (closestNeighbours.size > 0)
            {
                do
                {
                    next = closestNeighbours.Dequeue();
                    isNextInProgress = chunkGroup.HasChunkStartedAt(VectorExtension.ToArray(next));
                } while (isNextInProgress && closestNeighbours.size > 0);


                if (!isNextInProgress)
                {
                    CreateChunkWithAsyncGPUReadback(next, FindNeighbourOfChunk);
                }

            }
        }

        protected void FindNeighbourOfChunk(CompressedMarchingCubeChunk c, MeshData meshData)
        {
            if(!meshData.IsEmpty)
                neighbourFinder.AddTask(new ChunkNeighbourTask(c, meshData));
        }


        public void BuildRelevantChunksFromMeshDataBlockingAround(ChunkNeighbourTask task)
        {
            bool[] dirs = task.HasNeighbourInDirection;
            CompressedMarchingCubeChunk chunk = task.chunk;
            int count = dirs.Length;
            for (int i = 0; i < count; ++i)
            {
                if (!dirs[i])
                    continue;

                Vector3Int v3 = VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
                closestNeighbours.Enqueue(0, v3);
                ///for initial neighbours build additional chunks to not just wait for first thread to be done
                ///seems to worsen performance?
                //v3 = 2 * VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
                //closestNeighbours.Enqueue(0, v3);

                //v3 = 3 * VectorExtension.DirectionFromIndex[i] * (chunk.ChunkSize + 1) + chunk.CenterPos;
                //closestNeighbours.Enqueue(0, v3);
            }
            if (closestNeighbours.size > 0)
            {
                BuildRelevantChunksFromMeshDataBlockingAround();
            }

            watch.Stop();
            Debug.Log("Total millis: " + watch.Elapsed.TotalMilliseconds);
            if (totalTriBuild >= maxTrianglesLeft)
            {
                Debug.Log("Aborted");
            }
            Debug.Log("Total triangles: " + totalTriBuild);

            // Debug.Log($"Number of chunks: {ChunkGroups.Count}");
        }

        private void BuildRelevantChunksFromMeshDataBlockingAround()
        {
            List<Exception> x = CompressedMarchingCubeChunk.xs;
            Vector3Int next;
            MeshData data;
            CompressedMarchingCubeChunk chunk;

            bool isNextInProgress;
            while (!InitializationComplete)
            {
                do
                {
                    next = closestNeighbours.Dequeue();
                    isNextInProgress = chunkGroup.HasChunkStartedAt(VectorExtension.ToArray(next));
                } while (isNextInProgress && closestNeighbours.size > 0);


                if (!isNextInProgress)
                {
                    chunk = CreateChunkAt(next, out data);
                    FindNeighbourOfChunk(chunk, data);
                }
                if (totalTriBuild < maxTrianglesLeft)
                {
                    while (closestNeighbours.size == 0 && !InitializationComplete)
                    {
                        //TODO: while waiting create mesh displayers! -> leads to worse performance?
                        while (HasFinishedTask)
                        {
                            BuildNeighbourChunks(GetFinishedTask());
                        }
                    }
                }
            }
        }

        public void BuildNeighbourChunks(ChunkNeighbourTask task)
        {
            BuildNeighbourChunks(task.HasNeighbourInDirection, task.chunk.ChunkSize, task.chunk.CenterPos);
        }

        public void BuildNeighbourChunks(bool[] dirs, int chunkSize, Vector3Int centerPos)
        {
            Vector3Int v3;
            int count = dirs.Length;
            for (int i = 0; i < count; ++i)
            {
                if (!dirs[i])
                    continue;

                v3 = VectorExtension.DirectionFromIndex[i] * (chunkSize + 1) + centerPos;
                float sqrDist = (startPos - v3).sqrMagnitude;

                if (sqrDist <= buildAroundSqrDistance
                    && !chunkGroup.HasGroupItemAt(VectorExtension.ToArray(v3)))
                {
                    closestNeighbours.Enqueue(sqrDist, v3);
                }
                else
                {
                    BuildEmptyChunkAt(v3);
                }
            }
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

        protected void FindNonEmptyChunkAroundAsync(Vector3 pos, Action<CompressedMarchingCubeChunk, MeshData> callback)
        {
            Time.timeScale = 0;
            mainCam.enabled = false;
            FindNonEmptyChunkAroundAsync(pos, callback, 0);
        }

        protected void FindNonEmptyChunkAroundAsync(Vector3 pos, Action<CompressedMarchingCubeChunk, MeshData> callback, int tryCount)
        {
            //TODO:Remove trycount later
            if (tryCount++ >= 100)
                return;

            CreateChunkWithAsyncGPUReadback(pos, (c, meshData) => CheckChunk(c, meshData, callback, tryCount, ref pos));
        }

        protected void CheckChunk(CompressedMarchingCubeChunk chunk, MeshData meshData, Action<CompressedMarchingCubeChunk, MeshData> callback, int tryCount, ref Vector3 pos)
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
                callback(chunk, meshData);
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
        public void CreateChunkWithNoiseEdit(Vector3Int p, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance, out CompressedMarchingCubeChunk chunk)
        {
            bool hasChunkAtPosition = chunkGroup.TryGetGroupItemAt(VectorExtension.ToArray(p), out chunk);

            if (!hasChunkAtPosition || !chunk.HasStarted)
            {
                if (chunk != null)
                {
                    ///current chunk is marks border of generated chunks, so destroy it
                    chunk.DestroyChunk();
                }
                chunk = CreateChunkWithProperties(p, 0, DEFAULT_CHUNK_SIZE_POWER, false,
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

        protected void CreateChunkWithAsyncGPUReadback(Vector3 pos, Action<CompressedMarchingCubeChunk, MeshData> callback, bool allowOverride = false)
        {
            GetSizeAndLodPowerForChunkPosition(pos, out int chunkSizePower, out int lodPower);
            CreateChunkWithPropertiesAsync(VectorExtension.ToVector3Int(pos), lodPower, chunkSizePower, allowOverride, callback);
        }

        protected void CreateChunkWithPropertiesAsync(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride, Action<CompressedMarchingCubeChunk, MeshData> callback, Action WorkOnNoise = null)
        {
            CompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(pos, lodPower, chunkSizePower, allowOverride);
            BuildChunkAsyncFromMeshData(chunk, callback);
        }

        protected CompressedMarchingCubeChunk GetChunkObjectAt(CompressedMarchingCubeChunk chunk, Vector3Int position, int lodPower, int chunkSizePower, bool allowOverride)
        {
            ///Pot racecondition
            ChunkGroupRoot chunkGroupRoot = chunkGroup.GetOrCreateGroupAtGlobalPosition(VectorExtension.ToArray(position));
            chunk.ChunkHandler = this;
            chunk.ChunkSizePower = chunkSizePower;
            chunk.ChunkUpdater = worldUpdater;
            chunk.Material = chunkMaterial;
            chunk.LODPower = lodPower;

            chunkGroupRoot.SetLeafAtPosition(new int[] { position.x, position.y, position.z }, chunk, allowOverride);

            return chunk;
        }

        public void BuildEmptyChunkAt(Vector3Int pos)
        {
            int[] posArray = VectorExtension.ToArray(pos);
            ChunkGroupRoot chunkGroupRoot = chunkGroup.GetOrCreateGroupAtGlobalPosition(posArray);
            if (!chunkGroupRoot.HasLeafAtGlobalPosition(posArray))
            {
                CompressedMarchingCubeChunk chunk = new CompressedMarchingCubeChunk();
                chunk.ChunkHandler = this;
                chunk.ChunkSizePower = CHUNK_GROUP_SIZE_POWER;
                chunk.ChunkUpdater = worldUpdater;
                chunk.LODPower = MAX_CHUNK_LOD_POWER + 1;

                chunk.IsSpawner = true;

                chunkGroupRoot.SetLeafAtPosition(new int[] { pos.x, pos.y, pos.z }, chunk, false);

                simpleChunkColliderPool.GetItemFromPoolFor(chunk);
            }
        }

        protected CompressedMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int position, int lodPower, int chunkSizePower, bool allowOverride)
        {
            if (lodPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
            {
                ReducedMarchingCubesChunk chunk = new ReducedMarchingCubesChunk();
                return GetChunkObjectAt(chunk, position, lodPower, chunkSizePower, allowOverride);
            }
            else
            {
                return GetChunkObjectAt(new CompressedMarchingCubeChunk(), position, lodPower, chunkSizePower, allowOverride);
            }
        }

        protected void SetChunkComponents(CompressedMarchingCubeChunk chunk)
        {
            SetDisplayerOfChunk(chunk);
            SetLODColliderOfChunk(chunk);
        }


        public bool TryGetReadyChunkAt(Vector3Int p, out CompressedMarchingCubeChunk chunk) => chunkGroup.TryGetReadyChunkAt(VectorExtension.ToArray(p), out chunk);


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
        protected void DispatchMultipleChunks(CompressedMarchingCubeChunk[] chunks, Action<CompressedMarchingCubeChunk, MeshData> callbackPerChunk)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                BuildChunkAsyncFromMeshData(chunks[i], callbackPerChunk);
            }
        }

        protected void SetLODColliderOfChunk(CompressedMarchingCubeChunk chunk)
        {
            simpleChunkColliderPool.GetItemFromPoolFor(chunk);
        }

        //public int ComputeCubesFromNoise(CompressedMarchingCubeChunk chunk, int lod, bool resetCounter = true)
        //{
        //    int pointsPerAxis = DispatchCubesFromNoise(chunk, lod, resetCounter);
        //    int numTris = ComputeBufferExtension.GetLengthOfAppendBuffer(trianglesToBuild, triCountBuffer);
        //    totalTriBuild += numTris;

        //    BuildPreparedCubes(chunk, lod, pointsPerAxis, numTris);

        //    return numTris;
        //}


        public int GetFeasibleReducedLodForChunk(CompressedMarchingCubeChunk c, int toLodPower)
        {
            return Mathf.Min(toLodPower, c.LODPower + 1);
        }

        public int GetFeasibleIncreaseLodForChunk(CompressedMarchingCubeChunk c, int toLodPower)
        {
            return Mathf.Max(toLodPower, c.LODPower - 1);
        }

        public void IncreaseChunkLod(CompressedMarchingCubeChunk chunk, int toLodPower)
        {
            toLodPower = GetFeasibleIncreaseLodForChunk(chunk, toLodPower);
            int toLod = RoundToPowerOf2(toLodPower);
            if (toLod >= chunk.LOD || chunk.ChunkSize % toLod != 0)
                Debug.LogWarning($"invalid new chunk lod {toLodPower} from lod {chunk.LODPower}");

            int newSizePow = DEFAULT_CHUNK_SIZE_POWER + toLodPower;
            if (newSizePow == chunk.ChunkSizePower || newSizePow == CHUNK_GROUP_SIZE_POWER)
            {
                ExchangeSingleChunkAsyncParallel(chunk, chunk.AnchorPos, toLodPower, chunk.ChunkSizePower, true);
            }
            else
            {
                SplitChunkAndIncreaseLod(chunk, toLodPower, newSizePow);
            }
        }

        public void SpawnEmptyChunksAround(CompressedMarchingCubeChunk c)
        {
            bool[] dirs = c.HasNeighbourInDirection;
            int count = dirs.Length;
            for (int i = 0; i < count; i++)
            {
                if (!dirs[i])
                    continue;
                Vector3Int v3 = VectorExtension.DirectionFromIndex[i] * (c.ChunkSize + 1) + c.CenterPos;
                BuildEmptyChunkAt(v3);
            }
        }

        protected void ExchangeSingleChunkAsyncParallel(CompressedMarchingCubeChunk from, Vector3Int anchorPos, int lodPow, int sizePow, bool allowOveride)
        {
            from.PrepareDestruction();
            ExchangeChunkAsyncParallel(anchorPos, lodPow, sizePow, allowOveride, (c,_) => { FinishParallelChunk(from, c); });
        }


        protected void FinishParallelChunk(CompressedMarchingCubeChunk from, CompressedMarchingCubeChunk newChunk)
        {
            lock (exchangeLocker)
            {
                worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(from, newChunk));
            }
        }

        protected void BuildChunkAsyncFromMeshData(CompressedMarchingCubeChunk chunk, Action<CompressedMarchingCubeChunk, MeshData> onChunkDone)
        {
            chunkInAsync++;
            chunkGPURequest.DispatchAndGetChunkMeshDataAsync(chunk, SetChunkComponents, (data) =>
                {
                    chunk.InitializeWithMeshData(data);
                    onChunkDone(chunk, data);
                    chunkInAsync--;
                });
        }

        protected void ExchangeChunkAsyncParallel(Vector3Int anchorPos, int lodPow, int sizePow, bool allowOveride, Action<CompressedMarchingCubeChunk, MeshData> onChunkDone)
        {
            CompressedMarchingCubeChunk newChunk = GetThreadedChunkObjectAt(anchorPos, lodPow, sizePow, allowOveride);
            BuildChunkAsyncFromMeshData(newChunk, onChunkDone);
        }

        private void SplitChunkAndIncreaseLod(CompressedMarchingCubeChunk chunk, int toLodPower, int newSizePow)
        {
            int[][] anchors = chunk.Leaf.GetAllChildGlobalAnchorPosition();
            CompressedMarchingCubeChunk[] newChunks = new CompressedMarchingCubeChunk[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3Int v3 = IntArrToVector3(anchors[i]);
                //TODO:use already referenced parent to set children
                newChunks[i] = GetThreadedChunkObjectAt(v3, toLodPower, newSizePow, true);
            }
            chunk.PrepareDestruction();
            object listLock = new object();

            List<CompressedMarchingCubeChunk> chunks = new List<CompressedMarchingCubeChunk>();

            Action<CompressedMarchingCubeChunk, MeshData> f = (c, _) =>
            {
                //c.Hide();
                lock (listLock)
                {
                    chunks.Add(c);
                }
                if (chunks.Count == 8)
                {
                    lock (exchangeLocker)
                    {
                        worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(chunk, chunks));
                    }
                }
            };

            DispatchMultipleChunks(newChunks,f);

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

        public void DecreaseChunkLod(CompressedMarchingCubeChunk chunk, int toLodPower)
        {
            if (toLodPower == DESTROY_CHUNK_LOD)
            {
                chunk.DestroyChunk();
            }
            else if (toLodPower == DEACTIVATE_CHUNK_LOD)
            {
                chunk.ResetChunk();
            }
            else
            {
                toLodPower = GetFeasibleReducedLodForChunk(chunk, toLodPower);
                int toLod = RoundToPowerOf2(toLodPower);
                if (toLod <= chunk.LOD || chunk.ChunkSize % toLod != 0)
                    Debug.LogWarning($"invalid new chunk lod {toLodPower} from lod {chunk.LODPower}");

                if (chunk.Leaf.AllSiblingsAreLeafsWithSameTargetLod())
                {
                    MergeAndReduceChunkBranch(chunk, toLodPower);
                }
                else
                {
                    ///Decrease single chunk lod
                    ExchangeSingleChunkAsyncParallel(chunk, chunk.CenterPos, toLodPower, chunk.ChunkSizePower, true);
                }
            }
        }


        public void MergeAndReduceChunkBranch(CompressedMarchingCubeChunk chunk, int toLodPower)
        {
            List<CompressedMarchingCubeChunk> oldChunks = new List<CompressedMarchingCubeChunk>();
            chunk.Leaf.parent.PrepareBranchDestruction(oldChunks);

            ExchangeChunkAsyncParallel(chunk.CenterPos, toLodPower, chunk.ChunkSizePower + 1, true, (c, _) =>
            {
                lock (exchangeLocker)
                {
                    worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(oldChunks, c));
                }
            });
        }

        public void FreeCollider(ChunkLodCollider c)
        {
            simpleChunkColliderPool.ReturnItemToPool(c);
        }

        public void SetChunkColliderOf(CompressedMarchingCubeChunk c)
        {
            simpleChunkColliderPool.GetItemFromPoolFor(c);
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

        public int GetLodPower(float distance)
        {
            return (int)Mathf.Max(DEFAULT_MIN_CHUNK_LOD_POWER, lodPowerForDistances.Evaluate(distance));
        }

        public int GetLodPowerAt(Vector3 pos)
        {
            return GetLodPower((pos - startPos).magnitude);
        }

        public int GetSizePowerForDistance(float distance)
        {
            return (int)chunkSizePowerForDistances.Evaluate(distance);
        }


        public int GetSizePowerForChunkAtPosition(Vector3 position)
        {
            return GetSizePowerForChunkAtDistance((position - startPos).magnitude);
        }

        public int GetSizePowerForChunkAtDistance(float distance)
        {
            return MIN_CHUNK_SIZE_POWER + GetSizePowerForDistance(distance);
        }


        public void GetSizeAndLodPowerForChunkPosition(Vector3 pos, out int sizePower, out int lodPower)
        {
            float distance = (startPos - pos).magnitude;
            lodPower = GetLodPower(distance);
            sizePower = GetSizePowerForChunkAtDistance(distance);
            //TODO: check this
        }

        protected int RoundToPowerOf2(float f)
        {
            int r = (int)Mathf.Pow(2, Mathf.RoundToInt(f));

            return Mathf.Max(1, r);
        }

        protected void CreatePools()
        {
            chunkPipelinePool = new ChunkGenerationPipelinePool(CreateChunkPipeline);
            minDegreesAtCoordBufferPool = new BufferPool(CreateMinDegreeBuffer);

            simpleChunkColliderPool = new SimpleChunkColliderPool(colliderParent);
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
