using MeshGPUInstanciation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using IChunkGroupRoot = MarchingCubes.IChunkGroupRoot<MarchingCubes.ICompressedMarchingCubeChunk>;
using StorageGroup = MarchingCubes.GroupMesh<MarchingCubes.StorageTreeRoot, MarchingCubes.StoredChunkEdits, MarchingCubes.StorageTreeLeaf, MarchingCubes.IStorageGroupOrganizer<MarchingCubes.StoredChunkEdits>>;


namespace MarchingCubes
{
    //TODO: Check to use unity mathematics int2, int3 instead of vector for better performance?
    //TODO: When creating a chunk while editing, call getnoise with click changes to only generate noise once

    [Serializable]
    public class MarchingCubeChunkHandler : SaveableMonoBehaviour, IMarchingCubeChunkHandler
    {


        /// <summary>
        /// This should be the same value as in the compute shader "MarchingCubes"
        /// </summary>
        protected const float threadGroupSize = 4;

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

        public ComputeShader rebuildShader;


        public ComputeShader densityShader;

        public ComputeShader cubesPrepare;

        public ComputeShader buildPreparedCubes;

        public ComputeShader noiseEditShader;



        [Header("Voxel Settings")]
        //public float boundsSize = 8;
        public Vector3 noiseOffset = Vector3.zero;


        //[Range(2, 100)]
        //public int numPointsPerAxis = 30;

        public AnimationCurve lodPowerForDistances;

        public AnimationCurve chunkSizePowerForDistances;


        //public Dictionary<Vector3Int, IMarchingCubeChunk> Chunks => chunks;

        //protected HashSet<BaseMeshChild> inUseDisplayer = new HashSet<BaseMeshChild>();


        protected MeshDisplayerPool displayerPool;

        protected InteractableMeshDisplayPool interactableDisplayerPool;

        protected SimpleChunkColliderPool simpleChunkColliderPool;

        //Cant really pool noise array, maybe pool tribuilder aray instead (larger than neccessary)

        protected int channeledChunks = 0;

        protected bool hasFoundInitialChunk;


        public int totalTriBuild;

        TriangleBuilder[] tris;// = new TriangleBuilder[CHUNK_VOLUME * 5];
        float[] pointsArray;

        private ComputeBuffer pointBiomIndex;
        private ComputeBuffer biomBuffer;

        public WorldUpdater worldUpdater;

        public Transform colliderParent;

        private BufferPool minDegreesAtCoordBufferPool;

        public ChunkGenerationPipelinePool chunkPipelinePool;

        protected ChunkGenerationGPUData currentChunkPipeline;

        public bool useTerrainNoise;


        public int deactivateAfterDistance = 40;

        public Material chunkMaterial;

        public Transform player;

        public int buildAroundDistance = 2;

        protected long buildAroundSqrDistance;

        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

        public static object exchangeLocker = new object();

        public int minSteepness = 15;
        public int maxSteepness = 50;

        protected Vector3 startPos;
        protected float maxSqrChunkDistance;

        protected BinaryHeap<float, Vector3Int> closestNeighbours = new BinaryHeap<float, Vector3Int>(float.MinValue, float.MaxValue, 200);


        //TODO:GPU instancing from script generated meshes and add simple colliders as game objects

        //TODO: Changing lods on rapid moving character not really working. also mesh vertices error thrown sometimes

        //TODO: Handle chunks spawn with too low lod outside of next level lod collider -> no call to reduce lod

        //Test
        public SpawnGrassForMarchingCube grass;

        public EnvironmentSpawner environmentSpawner;

        PoolOf<ComputeShader> test;

        private void Start()
        {
            //try this to decouoe compute shaders 
            //pool entire shader and buffer cluster at once?
            test = new PoolOf<ComputeShader>(()=>Instantiate(cubesPrepare));
            //cubesPrepare = test.GetItemFromPool();
            //test.GetItemFromPool();
            //test.GetItemFromPool();
            //test.GetItemFromPool();

            simpleChunkColliderPool = new SimpleChunkColliderPool(colliderParent);
            displayerPool = new MeshDisplayerPool(transform);
            interactableDisplayerPool = new InteractableMeshDisplayPool(transform);
            CreateAllBuffersWithSizes();

            TriangulationTableStaticData.BuildLookUpTables();

            InitializeDensityGenerator();


            ApplyPreparerProperties(cubesPrepare);

            watch.Start();
            buildAroundSqrDistance = (long)buildAroundDistance * buildAroundDistance;
            startPos = player.position;


            //ICompressedMarchingCubeChunk chunk = FindNonEmptyChunkAround(player.position);
            //maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;
            //BuildRelevantChunksParallelBlockingAround(chunk);

            for (int i = 1; i < 2; i++)
            {
                CreateChunkAtAsync(startPos + new Vector3(0, -DEFAULT_CHUNK_SIZE, 0) * i, (c) => { });
                //CreateChunkAtAsync(startPos + new Vector3(32, -DEFAULT_CHUNK_SIZE, 0) * i, (c) => { });
                //CreateChunkAtAsync(startPos + new Vector3(32, -DEFAULT_CHUNK_SIZE, 32) * i, (c) => { });
            }

            //FindNonEmptyChunkAroundAsync(startPos, (chunk) =>
            //{
            //    maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;

            //    //BuildRelevantChunksParallelBlockingAround(chunk);
            //});
        }

        protected void ApplyPreparerProperties(ComputeShader s)
        {
            //s.SetBuffer(0, "points", pointsBuffer);
        }

        protected void InitializeDensityGenerator()
        {
            //densityGenerator.SetBuffer(pointsBuffer, savedPointBuffer, pointBiomIndex);
        }

        public void BuildRelevantChunksParallelBlockingAround(ICompressedMarchingCubeChunk chunk)
        {
            bool[] dirs = chunk.HasNeighbourInDirection;
            int count = dirs.Length;
            for (int i = 0; i < count; ++i)
            {
                if (!dirs[i])
                    continue;

                Vector3Int v3 = VectorExtension.GetDirectionFromIndex(i) * (chunk.ChunkSize + 1) + chunk.CenterPos;
                closestNeighbours.Enqueue(0, v3);
                ///for initial neighbours build additional chunks to not just wait for first thread to be done
                ///seems to worsen performance?
                //v3 = 2 * VectorExtension.GetDirectionFromIndex(i) * (chunk.ChunkSize + 1) + chunk.CenterPos;
                //closestNeighbours.Enqueue(0, v3);

                //v3 = 3 * VectorExtension.GetDirectionFromIndex(i) * (chunk.ChunkSize + 1) + chunk.CenterPos;
                //closestNeighbours.Enqueue(0, v3);
            }
            if (closestNeighbours.size > 0)
            {
                BuildRelevantChunksParallelBlockingAround();
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

        //Todo: try do this work on compute shader already
        private void BuildRelevantChunksParallelBlockingAround()
        {
            List<Exception> x = CompressedMarchingCubeChunk.xs;
            Vector3Int next;
            bool isNextInProgress;
            while (closestNeighbours.size > 0)
            {
                do
                {
                    next = closestNeighbours.Dequeue();
                    isNextInProgress = chunkGroup.HasChunkStartedAt(next);
                } while (isNextInProgress && closestNeighbours.size > 0);


                if (!isNextInProgress)
                {
                    CreateChunkParallelAt(next);
                }
                if (totalTriBuild < maxTrianglesLeft)
                {
                    while ((closestNeighbours.size == 0 && channeledChunks > x.Count) /*|| channeledChunks > maxRunningThreads*/)
                    {
                        //TODO: while waiting create mesh displayers! -> leads to worse performance?
                        while (readyParallelChunks.Count > 0)
                        {
                            OnParallelChunkDoneCallBack(readyParallelChunks.Dequeue());
                        }
                    }
                }
            }
        }


        public IEnumerator BuildRelevantChunksParallelAround(ICompressedMarchingCubeChunk chunk)
        {
            bool[] dirs = chunk.HasNeighbourInDirection;
            int count = dirs.Length;
            for (int i = 0; i < count; ++i)
            {
                if (!dirs[i])
                    continue;

                Vector3Int v3 = VectorExtension.GetDirectionFromIndex(i) * (chunk.ChunkSize + 1) + chunk.CenterPos;
                closestNeighbours.Enqueue(0, v3);
            }
            if (closestNeighbours.size > 0)
            {
                yield return BuildRelevantChunksParallelAround();
            }

            watch.Stop();
            Debug.Log("Total millis: " + watch.Elapsed.TotalMilliseconds);
            if (totalTriBuild >= maxTrianglesLeft)
            {
                Debug.Log("Aborted");
            }
            Debug.Log("Total triangles: " + totalTriBuild);
        }

        private IEnumerator BuildRelevantChunksParallelAround()
        {
            List<Exception> x = CompressedMarchingCubeChunk.xs;
            Vector3Int next;
            bool isNextInProgress;
            while (closestNeighbours.size > 0)
            {
                do
                {
                    next = closestNeighbours.Dequeue();
                    isNextInProgress = chunkGroup.HasChunkStartedAt(next);
                } while (isNextInProgress && closestNeighbours.size > 0);

                if (!isNextInProgress)
                {
                    CreateChunkParallelAt(next);
                }
                if (totalTriBuild < maxTrianglesLeft)
                {
                    while ((closestNeighbours.size == 0 && channeledChunks > x.Count) /*|| channeledChunks > maxRunningThreads*/)
                    {
                        while (readyParallelChunks.Count > 0)
                        {
                            OnParallelChunkDoneCallBack(readyParallelChunks.Dequeue());
                        }
                        yield return null;
                    }
                }
            }
        }

        protected void OnParallelChunkDoneCallBack(ICompressedMarchingCubeChunk chunk)
        {
            channeledChunks--;

            chunk.SetChunkOnMainThread();
            if (chunk.IsEmpty)
            {
                chunk.DestroyChunk();
            }
            else
            {
                Vector3Int v3;
                bool[] dirs = chunk.HasNeighbourInDirection;
                int count = dirs.Length;
                for (int i = 0; i < count; ++i)
                {
                    if (!dirs[i])
                        continue;

                    v3 = VectorExtension.GetDirectionFromIndex(i) * (chunk.ChunkSize + 1) + chunk.CenterPos;
                    float sqrDist = (startPos - v3).sqrMagnitude;

                    if (sqrDist <= buildAroundSqrDistance
                        && !chunkGroup.HasGroupItemAt(v3))
                    {
                        closestNeighbours.Enqueue(sqrDist, v3);
                    }
                    else
                    {
                        BuildEmptyChunkAt(v3);
                    }
                }
            }
        }

        protected bool lastChunkWasAir;

        protected ICompressedMarchingCubeChunk FindNonEmptyChunkAround(Vector3 pos)
        {
            bool isEmpty = true;
            ICompressedMarchingCubeChunk chunk = null;
            int tryCount = 0;
            //TODO:Remove trycount later
            while (isEmpty && tryCount++ < 100)
            {
                chunk = CreateChunkAt(pos);
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

        protected void FindNonEmptyChunkAroundAsync(Vector3 pos, Action<ICompressedMarchingCubeChunk> callback)
        {
            FindNonEmptyChunkAroundAsync(pos, callback, 0);
        }

        protected void FindNonEmptyChunkAroundAsync(Vector3 pos, Action<ICompressedMarchingCubeChunk> callback, int tryCount)
        {
            //TODO:Remove trycount later
            if (tryCount++ >= 100)
                return;

            CreateChunkAtAsync(pos, (c) => CheckChunk(c, callback, tryCount, ref pos));
        }

        protected void CheckChunk(ICompressedMarchingCubeChunk chunk, Action<ICompressedMarchingCubeChunk> callback, int tryCount, ref Vector3 pos)
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

        protected void CreateChunkParallelAt(Vector3 pos)
        {
            int lodPower;
            int chunkSizePower;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSizePower, out lodPower);

            ICompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(Vector3Int.FloorToInt(pos), lodPower, chunkSizePower, false);
            BuildChunkParallel(chunk);
        }

        public bool TryGetOrCreateChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk)
        {
            if (!chunkGroup.TryGetGroupItemAt(p, out chunk))
            {
                chunk = CreateChunkWithProperties(p, 0, DEFAULT_CHUNK_SIZE_POWER, false);
            }
            if (chunk != null && !chunk.IsReady)
            {
                Debug.LogWarning("Unfinished chunk next to requested chunk. may lead to holes in mesh!");
            }
            return chunk != null;
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
        public bool CreateChunkWithNoiseEdit(Vector3Int p, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance, out ICompressedMarchingCubeChunk chunk)
        {
            bool createdChunk = false;
            bool hasChunkAtPosition = chunkGroup.TryGetGroupItemAt(p, out chunk);

            if (!hasChunkAtPosition || !chunk.HasStarted)
            {
                if (chunk != null)
                {
                    ///current chunk is marks border of generated chunks, so destroy it
                    chunk.DestroyChunk();
                }
                chunk = CreateChunkWithProperties(p, 0, DEFAULT_CHUNK_SIZE_POWER, false,
                    () => {
                        ApplyNoiseEditing(33, editPoint, start, end, delta, maxDistance);
                    });
                createdChunk = true;
            }
            return createdChunk;
        }

        protected ICompressedMarchingCubeChunk CreateChunkAt(Vector3Int p, bool allowOverride = false)
        {
            return CreateChunkAt(p, allowOverride);
        }

        //TODO:Check if collider can be removed from most chunks.
        //Collision can be approximated by calling noise function for lowest point of object and checking if its noise is larger than surface value

        protected ICompressedMarchingCubeChunk CreateChunkAt(Vector3 pos, bool allowOverride = false)
        {
            int lodPower;
            int chunkSizePower;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSizePower, out lodPower);
            return CreateChunkWithProperties(VectorExtension.ToVector3Int(pos), lodPower, chunkSizePower, allowOverride);
        }

        protected ICompressedMarchingCubeChunk CreateChunkWithProperties(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride, Action WorkOnNoise = null)
        {
            ICompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(pos, lodPower, chunkSizePower, allowOverride);
            BuildChunk(chunk, WorkOnNoise);
            return chunk;
        }

        protected void CreateChunkAtAsync(Vector3 pos, Action<ICompressedMarchingCubeChunk> callback, bool allowOverride = false)
        {
            int lodPower;
            int chunkSizePower;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSizePower, out lodPower);
            CreateChunkWithPropertiesAsync(VectorExtension.ToVector3Int(pos), lodPower, chunkSizePower, allowOverride, callback);
        }

        protected void CreateChunkWithPropertiesAsync(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride, Action<ICompressedMarchingCubeChunk> callback, Action WorkOnNoise = null)
        {
            ICompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(pos, lodPower, chunkSizePower, allowOverride);
            BuildChunkAsync(chunk, callback);
        }

        protected ICompressedMarchingCubeChunk GetChunkObjectAt(ICompressedMarchingCubeChunk chunk, Vector3Int position, int lodPower, int chunkSizePower, bool allowOverride)
        {
            ///Pot racecondition
            ChunkGroupRoot chunkGroupRoot = chunkGroup.GetOrCreateGroupAtGlobalPosition(position);
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
            ChunkGroupRoot chunkGroupRoot = chunkGroup.GetOrCreateGroupAtGlobalPosition(pos);
            if (!chunkGroupRoot.HasLeafAtGlobalPosition(pos))
            {
                ICompressedMarchingCubeChunk chunk = new CompressedMarchingCubeChunk();
                chunk.ChunkHandler = this;
                chunk.ChunkSizePower = CHUNK_GROUP_SIZE_POWER;
                chunk.ChunkUpdater = worldUpdater;
                //chunk.Material = chunkMaterial;
                //chunk.SurfaceLevel = surfaceLevel;
                chunk.LODPower = MAX_CHUNK_LOD_POWER + 1;

                chunk.IsSpawner = true;

                chunkGroupRoot.SetLeafAtPosition(new int[] { pos.x, pos.y, pos.z }, chunk, false);

                simpleChunkColliderPool.GetItemFromPoolFor(chunk);
            }
        }

        protected ICompressedMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int position, int lodPower, int chunkSizePower, bool allowOverride)
        {
            if (lodPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
            {
                MarchingCubeChunk chunk = new MarchingCubeChunk();
                //chunk.rebuildShader = rebuildShader;
                //chunk.rebuildTriCounter = triCountBuffer;
                //chunk.rebuildTriResult = triangleBuffer;
                //chunk.rebuildNoiseBuffer = pointsBuffer;
                return GetChunkObjectAt(chunk, position, lodPower, chunkSizePower, allowOverride);
            }
            else
            {
                return GetChunkObjectAt(new CompressedMarchingCubeChunk(), position, lodPower, chunkSizePower, allowOverride);
            }
        }



        //protected bool HasChunkAtPosition(Vector3Int v3)
        //{
        //    ICompressedMarchingCubeChunk _;
        //    return TryGetChunkAtPosition(v3, out _);
        //}

        //public bool TryGetChunkAtPosition(Vector3Int p, out ICompressedMarchingCubeChunk chunk)
        //{
        //    Vector3Int coord = PositionToChunkGroupCoord(p);
        //    IChunkGroupRoot chunkGroup;
        //    chunk = null;
        //    if (chunkGroups.TryGetValue(coord, out chunkGroup))
        //    {
        //        if (/*chunkGroup.HasChild && */chunkGroup.TryGetLeafAtGlobalPosition(p, out chunk))
        //        {
        //            return true;
        //        }
        //    }
        //    return chunk != null;
        //}

        public bool TryGetReadyChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk) => chunkGroup.TryGetReadyChunkAt(p, out chunk);


        //public MarchingCubeChunkNeighbourLODs GetNeighbourLODSFrom(IMarchingCubeChunk chunk)
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
        protected void BuildChunk(ICompressedMarchingCubeChunk chunk, Action WorkOnNoise = null)
        {
            TriangleChunkHeap ts = DispatchAndGetShaderData(chunk, WorkOnNoise);
            chunk.InitializeWithMeshData(ts);
        }

        protected Queue<ICompressedMarchingCubeChunk> readyParallelChunks = new Queue<ICompressedMarchingCubeChunk>();

        protected void BuildChunkParallel(ICompressedMarchingCubeChunk chunk)
        {
            TriangleChunkHeap ts = DispatchAndGetShaderData(chunk);
            channeledChunks++;
            chunk.InitializeWithMeshDataParallel(ts, readyParallelChunks);
        }

        protected void BuildChunkAsync(ICompressedMarchingCubeChunk chunk, Action<ICompressedMarchingCubeChunk> onChunkDone = null)
        {
            DispatchAndGetShaderDataAsync(chunk, (ts) =>
            {
                channeledChunks++;
                chunk.InitializeWithMeshData(ts);
                onChunkDone(chunk);
            });
        }

    

        public void SetEditedNoiseAtPosition(IMarchingCubeChunk chunk, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance)
        {
            //propably remove edit noise shader :/

            //int pointsPerAxis = chunk.PointsPerAxis;
            //float[] result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            //ComputeBuffer pointsBuffer = pointsBufferPool.GetItemFromPool();
            //GenerateNoise(pointsBuffer, chunk.ChunkSizePower, pointsPerAxis, chunk.LOD, chunk.AnchorPos);
            //ApplyNoiseEditing(pointsPerAxis, editPoint, start, end, delta, maxDistance);
            //pointsBuffer.GetData(result, 0, 0, result.Length);
            //pointsBufferPool.ReturnItemToPool(pointsBuffer);
            //chunk.Points = result;
            //storageGroup.Store(chunk.AnchorPos, chunk);
        }

        private void ApplyNoiseEditing(int pointsPerAxis, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance)
        {
            SetNoiseEditProperties(editPoint, start, end, delta, maxDistance);
            int threadsPerAxis = Mathf.CeilToInt(pointsPerAxis / threadGroupSize);
            noiseEditShader.Dispatch(0, threadsPerAxis, threadsPerAxis, threadsPerAxis);
        }

        private void SetNoiseEditProperties(Vector3 editPoint, Vector3 start, Vector3 end, float delta, float maxDistance)
        {
            noiseEditShader.SetVector("clickPoint", editPoint);
            noiseEditShader.SetVector("start", start);
            noiseEditShader.SetVector("end", end);
            noiseEditShader.SetFloat("delta", delta);
            noiseEditShader.SetFloat("maxDistance", maxDistance);
        }

     
        //TODO: Maybe remove pooling theese -> could reduce size of buffer for faster reads
        protected void DispatchMultipleChunks(ICompressedMarchingCubeChunk[] chunks, Action<ICompressedMarchingCubeChunk> callbackPerChunk)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                BuildChunkAsync(chunks[i], callbackPerChunk);
            }
            //trianglesToBuild.SetCounterValue(0);
            //int chunkLength = chunks.Length;
            //for (int i = 0; i < chunkLength; i++)
            //{
            //    ICompressedMarchingCubeChunk c = chunks[i];
            //    GenerateNoise(c.ChunkSizePower, c.PointsPerAxis, c.LOD, c.AnchorPos);
            //    AccumulateCubesFromNoise(c, i);
            //}
            //int[] triCounts = new int[chunkLength];

            //for (int i = 0; i < chunkLength; i++)
            //{
            //    //TODO:check if this reduces wait time from gpu
            //    SetDisplayerOfChunk(chunks[i]);
            //    simpleChunkColliderPool.GetItemFromPoolFor(chunks[i]);
            //}

            //triCountBuffer.GetData(triCounts, 0, 0, chunkLength);
            //TriangleChunkHeap[] result = new TriangleChunkHeap[chunkLength];
            //TriangleBuilder[] allTris = new TriangleBuilder[triCounts[triCounts.Length - 1]];
            //triangleBuffer.GetData(allTris, 0, 0, allTris.Length);
            //int last = 0;
            //for (int i = 0; i < chunkLength; i++)
            //{
            //    int current = triCounts[i];
            //    int length = current - last;
            //    result[i] = new TriangleChunkHeap(allTris, last, length);
            //    last = current;

            //    if (length == 0)
            //    {
            //        chunks[i].FreeSimpleChunkCollider();
            //    }
            //}
            //return result;
        }

        protected void ValidateChunkProperties(ICompressedMarchingCubeChunk chunk)
        {
            if (chunk.ChunkSize % chunk.LOD != 0)
                throw new Exception("Lod must be divisor of chunksize");
        }

        protected void SetLODColliderOfChunk(ICompressedMarchingCubeChunk chunk)
        {
            simpleChunkColliderPool.GetItemFromPoolFor(chunk);
        }

        protected void StoreNoise(ICompressedMarchingCubeChunk chunk, ComputeBuffer pointsBuffer)
        {
            int pointsPerAxis = chunk.PointsPerAxis;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;
            pointsArray = new float[pointsVolume];
            pointsBuffer.GetData(pointsArray);
            if (chunk is IMarchingCubeChunk c)
            {
                c.Points = pointsArray;
                storageGroup.Store(chunk.AnchorPos, chunk as IMarchingCubeChunk, true);
            }
        }

        protected void DetermineIfChunkIsAir(ComputeBuffer pointsBuffer)
        {
            pointsArray = new float[1];
            pointsBuffer.GetData(pointsArray);
            lastChunkWasAir = pointsArray[0] < SURFACE_LEVEL;
        }

        //TODO: Inform about Mesh subset and mesh set vertex buffer
        //Subset may be used to only change parts of the mesh -> dont need multiple mesh displayers with submeshes?
        protected TriangleChunkHeap DispatchAndGetShaderData(ICompressedMarchingCubeChunk chunk, Action WorkOnNoise = null)
        {
            ValidateChunkProperties(chunk);
            ComputeBuffer noiseBuffer = PrepareNoiseForChunk(chunk);
            bool storeNoise = WorkOnNoiseMap(chunk, WorkOnNoise);
            int numTris = ComputeCubesFromNoise(chunk);
            ///Do work for chunk here, before data from gpu is read, to give gpu time to finish
            SetDisplayerOfChunk(chunk);
            SetLODColliderOfChunk(chunk);

            tris = new TriangleBuilder[numTris];
            ///read data from gpu
            ReadCurrentTriangleData(tris);
            if (numTris == 0)
            {
                chunk.FreeSimpleChunkCollider();
                chunk.GiveUnusedDisplayerBack();
            }

            if(storeNoise)
            {
                StoreNoise(chunk, noiseBuffer);
            }
            else if(numTris == 0 && !hasFoundInitialChunk)
            {
                DetermineIfChunkIsAir(noiseBuffer);
            }
            pointsBufferPool.ReturnItemToPool(noiseBuffer);
            return new TriangleChunkHeap(tris, 0, numTris);
        }

        protected void DispatchAndGetShaderDataAsync(ICompressedMarchingCubeChunk chunk, Action<TriangleChunkHeap> OnDataDone, Action WorkOnNoise = null)
        {
            ValidateChunkProperties(chunk);
            ComputeBuffer noiseBuffer = PrepareNoiseForChunk(chunk);
            bool storeNoise = WorkOnNoiseMap(chunk, WorkOnNoise);
            ComputeBuffer trianglesToBuild = DispatchCubesFromNoise(chunk);
            //TODO: create and pool buffers to make async work
            ComputeBuffer triCountBuffer = copyCountPool.GetItemFromPool();
            ComputeBufferExtension.GetLengthOfAppendBufferAsync(trianglesToBuild, triCountBuffer, (numTris) =>
            {
                copyCountPool.ReturnItemToPool(triCountBuffer);
                if (numTris <= 0)
                {
                    if (!hasFoundInitialChunk)
                    {
                        DetermineIfChunkIsAir(noiseBuffer);
                    }
                    preparedTrianglePool.ReturnItemToPool(trianglesToBuild);
                    pointsBufferPool.ReturnItemToPool(noiseBuffer);
                    OnDataDone(new TriangleChunkHeap(Array.Empty<TriangleBuilder>(), 0, numTris));
                }
                else
                {
                    totalTriBuild += numTris;
                    ///Do work for chunk here, before data from gpu is read, to give gpu time to finish
                    SetDisplayerOfChunk(chunk);
                    SetLODColliderOfChunk(chunk);

                    ComputeBuffer trianglesBuffer = new ComputeBuffer(numTris, TriangleBuilder.SIZE_OF_TRI_BUILD);
                    buildPreparedCubes.SetBuffer(0, "triangles", trianglesBuffer);
                    BuildPreparedCubes(chunk, trianglesToBuild, numTris);

                    ///read data from gpu
                    ReadCurrentTriangleDataAsync(trianglesBuffer, (tris) =>
                    {
                        trianglesBuffer.Dispose();
                        preparedTrianglePool.ReturnItemToPool(trianglesToBuild);
                        pointsBufferPool.ReturnItemToPool(noiseBuffer);
                        if (storeNoise)
                        {
                            StoreNoise(chunk, noiseBuffer);
                        }
                        //TODO:Remove toArray!!
                        OnDataDone(new TriangleChunkHeap(tris.ToArray(), 0, numTris));
                    });
                }
            });
        }

        public void ReadCurrentTriangleData(TriangleBuilder[] tris)
        {
            //triangleBuffer.GetData(tris);
        }

        public void ReadCurrentTriangleDataAsync(ComputeBuffer triangleBuffer, Action<NativeArray<TriangleBuilder>> callback)
        {
            ComputeBufferExtension.ReadBufferAsync(triangleBuffer, callback);
        }

        public ComputeBuffer DispatchCubesFromNoise(ICompressedMarchingCubeChunk chunk)
        {
            PrepareChunkToStoreMinDegreesIfNeeded(chunk);

            ComputeBuffer trisToBuild = preparedTrianglePool.GetBufferForShaders();
            trisToBuild.SetCounterValue(0);

            int pointsPerAxis = chunk.PointsPerAxis;
            int numVoxelsPerAxis = pointsPerAxis - 1;

            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / threadGroupSize);

            cubesPrepare.SetInt("numPointsPerAxis", pointsPerAxis);

            cubesPrepare.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
            return trisToBuild;
        }

        public int ComputeCubesFromNoise(ICompressedMarchingCubeChunk chunk)
        {
            ComputeBuffer trisToBuildBuffer = DispatchCubesFromNoise(chunk);
            ComputeBuffer triCountBuffer = copyCountPool.GetItemFromPool();
            int numTris = ComputeBufferExtension.GetLengthOfAppendBuffer(trisToBuildBuffer, triCountBuffer);
            copyCountPool.ReturnItemToPool(triCountBuffer);

            totalTriBuild += numTris;
            BuildPreparedCubes(chunk, trisToBuildBuffer, numTris);

            return numTris;
        }

        //public int ComputeCubesFromNoise(ICompressedMarchingCubeChunk chunk, int lod, bool resetCounter = true)
        //{
        //    int pointsPerAxis = DispatchCubesFromNoise(chunk, lod, resetCounter);
        //    int numTris = ComputeBufferExtension.GetLengthOfAppendBuffer(trianglesToBuild, triCountBuffer);
        //    totalTriBuild += numTris;

        //    BuildPreparedCubes(chunk, lod, pointsPerAxis, numTris);

        //    return numTris;
        //}


        public int GetFeasibleReducedLodForChunk(ICompressedMarchingCubeChunk c, int toLodPower)
        {
            return Mathf.Min(toLodPower, c.LODPower + 1);
        }

        public int GetFeasibleIncreaseLodForChunk(ICompressedMarchingCubeChunk c, int toLodPower)
        {
            return Mathf.Max(toLodPower, c.LODPower - 1);
        }

        public void IncreaseChunkLod(ICompressedMarchingCubeChunk chunk, int toLodPower)
        {
            toLodPower = GetFeasibleIncreaseLodForChunk(chunk, toLodPower);
            int toLod = RoundToPowerOf2(toLodPower);
            if (toLod >= chunk.LOD || chunk.ChunkSize % toLod != 0)
                Debug.LogWarning($"invalid new chunk lod {toLodPower} from lod {chunk.LODPower}");

            int newSizePow = DEFAULT_CHUNK_SIZE_POWER + toLodPower;
            if (newSizePow == chunk.ChunkSizePower || newSizePow == CHUNK_GROUP_SIZE_POWER)
            {
                ExchangeSingleChunkParallel(chunk, chunk.AnchorPos, toLodPower, chunk.ChunkSizePower, true);
            }
            else
            {
                SplitChunkAndIncreaseLod(chunk, toLodPower, newSizePow);
            }
        }

        public void SpawnEmptyChunksAround(ICompressedMarchingCubeChunk c)
        {
            bool[] dirs = c.HasNeighbourInDirection;
            int count = dirs.Length;
            for (int i = 0; i < count; i++)
            {
                if (!dirs[i])
                    continue;
                Vector3Int v3 = VectorExtension.GetDirectionFromIndex(i) * (c.ChunkSize + 1) + c.CenterPos;
                BuildEmptyChunkAt(v3);
            }
        }

        protected ICompressedMarchingCubeChunk ExchangeSingleChunkParallel(ICompressedMarchingCubeChunk from, Vector3Int anchorPos, int lodPow, int sizePow, bool allowOveride)
        {
            from.PrepareDestruction();
            return ExchangeChunkParallel(anchorPos, lodPow, sizePow, allowOveride, (c) => { FinishParallelChunk(from, c); });
        }


        protected void FinishParallelChunk(ICompressedMarchingCubeChunk from, ICompressedMarchingCubeChunk newChunk)
        {
            lock (exchangeLocker)
            {
                worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(from, newChunk));
            }
        }


        protected ICompressedMarchingCubeChunk ExchangeChunkParallel(Vector3Int anchorPos, int lodPow, int sizePow, bool allowOveride, Action<ICompressedMarchingCubeChunk> onChunkDone)
        {
            ICompressedMarchingCubeChunk newChunk = GetThreadedChunkObjectAt(anchorPos, lodPow, sizePow, allowOveride);
            newChunk.InitializeWithMeshDataParallel(DispatchAndGetShaderData(newChunk), onChunkDone);
            return newChunk;
        }

        private void SplitChunkAndIncreaseLod(ICompressedMarchingCubeChunk chunk, int toLodPower, int newSizePow)
        {
            int[][] anchors = chunk.Leaf.GetAllChildGlobalAnchorPosition();
            ICompressedMarchingCubeChunk[] newChunks = new ICompressedMarchingCubeChunk[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3Int v3 = IntArrToVector3(anchors[i]);
                //TODO:use already referenced parent to set children
                newChunks[i] = GetThreadedChunkObjectAt(v3, toLodPower, newSizePow, true);
            }
            chunk.PrepareDestruction();
            object listLock = new object();

            List<ICompressedMarchingCubeChunk> chunks = new List<ICompressedMarchingCubeChunk>();

            Action<ICompressedMarchingCubeChunk> f = (c) =>
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

        public void DecreaseChunkLod(ICompressedMarchingCubeChunk chunk, int toLodPower)
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
                    ExchangeSingleChunkParallel(chunk, chunk.CenterPos, toLodPower, chunk.ChunkSizePower, true);
                }
            }
        }


        public void MergeAndReduceChunkBranch(ICompressedMarchingCubeChunk chunk, int toLodPower)
        {
            List<ICompressedMarchingCubeChunk> oldChunks = new List<ICompressedMarchingCubeChunk>();
            chunk.Leaf.parent.PrepareBranchDestruction(oldChunks);

            ExchangeChunkParallel(chunk.CenterPos, toLodPower, chunk.ChunkSizePower + 1, true, (c) =>
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

        public void SetChunkColliderOf(ICompressedMarchingCubeChunk c)
        {
            simpleChunkColliderPool.GetItemFromPoolFor(c);
        }

        public MarchingCubeMeshDisplayer GetNextMeshDisplayer()
        {
            return displayerPool.GetItemFromPoolFor(null);
        }

        public MarchingCubeMeshDisplayer GetNextInteractableMeshDisplayer(IMarchingCubeChunk chunk)
        {
            return interactableDisplayerPool.GetItemFromPoolFor(chunk);
        }

        protected void SetDisplayerOfChunk(ICompressedMarchingCubeChunk c)
        {
            if (c is IMarchingCubeChunk interactableChunk)
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

        protected void CreateAllBuffersWithSizes()
        {
            int maxTriangleCount = MAX_TRIANGLES_IN_CHUNK;
            maxTriangleCount *= MAX_CHUNKS_PER_ITERATION;

            biomBuffer = new ComputeBuffer(bioms.Length, BiomColor.SIZE);
            var b = bioms.Select(b => new BiomColor(b.visualizationData)).ToArray();
            biomBuffer.SetData(b);

            minDegreesAtCoordBufferPool = new BufferPool(CreateMinDegreeBuffer, "minDegreeAtCoord", buildPreparedCubes);
            copyCountPool = new DisposablePoolOf<ComputeBuffer>(CreateCopyCountBuffer);
            preparedTrianglePool = new BufferPool(CreatePrepareTriangleBuffer, "triangleLocations", cubesPrepare, buildPreparedCubes);
            pointsBufferPool = new BufferPool(CreatePointsBuffer, "points", densityGenerator.densityShader, cubesPrepare, buildPreparedCubes);

            pointBiomIndex = new ComputeBuffer(NOISE_POINTS_IN_DEFAULT_SIZED_CHUNK, sizeof(uint));
            //pointsBuffer = new ComputeBuffer(NOISE_POINTS_IN_DEFAULT_SIZED_CHUNK, sizeof(float));
            savedPointBuffer = new ComputeBuffer(NOISE_POINTS_IN_DEFAULT_SIZED_CHUNK, sizeof(float));
            //trianglesToBuild = new ComputeBuffer(maxTriangleCount, sizeof(int) * 2, ComputeBufferType.Append);
            //triangleBuffer = new ComputeBuffer(maxTriangleCount, TriangleBuilder.SIZE_OF_TRI_BUILD);
            //triCountBuffer = new ComputeBuffer(MAX_CHUNKS_PER_ITERATION, sizeof(int), ComputeBufferType.Raw);
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
            biomBuffer.Dispose();
            pointBiomIndex.Dispose();
            pointsBufferPool.DisposeAll();
            //triangleBuffer.Dispose();
            //pointsBuffer.Dispose();
            //trianglesToBuild.SetCounterValue(0);
            //trianglesToBuild.Dispose();
            savedPointBuffer.Dispose();
            minDegreesAtCoordBufferPool.DisposeAll();
            copyCountPool.DisposeAll();
            preparedTrianglePool.DisposeAll();
            //triCountBuffer.Dispose();
            //triangleBuffer = null;
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

        public void Store(Vector3Int anchorPos, IMarchingCubeChunk chunk, bool overrideNoise = false) => storageGroup.Store(anchorPos, chunk, overrideNoise);

        public bool TryLoadPoints(ICompressedMarchingCubeChunk marchingCubeChunk, out float[] loadedPoints) => storageGroup.TryLoadPoints(marchingCubeChunk,out loadedPoints);

    }
}
