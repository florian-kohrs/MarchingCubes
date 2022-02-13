﻿using MeshGPUInstanciation;
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

        public ComputeShader prepareAroundShader;

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

        public static HashSet<ICompressedMarchingCubeChunk> channeledChunks = new HashSet<ICompressedMarchingCubeChunk>();

        protected bool hasFoundInitialChunk;


        public int totalTriBuild;

        float[] pointsArray;

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

        protected Vector3 startPos;
        protected float maxSqrChunkDistance;

        protected BinaryHeap<float, Vector3Int> closestNeighbours = new BinaryHeap<float, Vector3Int>(float.MinValue, float.MaxValue, 200);


        //TODO:GPU instancing from script generated meshes and add simple colliders as game objects

        //TODO: Changing lods on rapid moving character not really working. also mesh vertices error thrown sometimes

        //TODO: Handle chunks spawn with too low lod outside of next level lod collider -> no call to reduce lod

        //Test
        public SpawnGrassForMarchingCube grass;

        public EnvironmentSpawner environmentSpawner;

        public ChunkGPUDataRequest chunkGPURequest;

        private void Start()
        {
            mainCam = Camera.main;
            noiseData.ApplyNoiseBiomData();

            CreatePools();

            chunkGPURequest = new ChunkGPUDataRequest(chunkPipelinePool, storageGroup, minDegreesAtCoordBufferPool);

            TriangulationTableStaticData.BuildLookUpTables();


            watch.Start();
            buildAroundSqrDistance = (long)buildAroundDistance * buildAroundDistance;
            startPos = player.position;


            ICompressedMarchingCubeChunk chunk = FindNonEmptyChunkAround(player.position);
            maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;
            //BuildRelevantChunksParallelBlockingAround(chunk);
            BuildRelevantChunksParallelWithAsyncGpuAround(chunk);

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

        int count = 0;
        Camera mainCam;

        protected IEnumerator WaitTillAsynGenerationDone()
        {
            bool repeat = true;
            while (repeat)
            {
                List<Exception> x = CompressedMarchingCubeChunk.xs;
                Vector3Int next;
                bool isNextInProgress;


                if (totalTriBuild < maxTrianglesLeft)
                {

                    //TODO: while waiting create mesh displayers! -> leads to worse performance?
                    while (readyParallelChunks.Count > 0)
                    {
                        count--;
                        OnParallelChunkDoneCallBack(readyParallelChunks.Dequeue());
                    }
                }

                while (closestNeighbours.size > 0)
                {
                    do
                    {
                        next = closestNeighbours.Dequeue();
                        isNextInProgress = chunkGroup.HasChunkStartedAt(next);
                    } while (isNextInProgress && closestNeighbours.size > 0);

                    if (!isNextInProgress)
                    {
                        count++;
                        CreateChunkWithAsyncGPUReadbackParallel(next);
                    }
                }

                channeledChunks.RemoveWhere(c => c == null);
                if (hasFoundInitialChunk && /*count <= x.Count && */channeledChunks.Count <= 0 /*|| channeledChunks > maxRunningThreads*/)
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

        public void BuildRelevantChunksParallelWithAsyncGpuAround(ICompressedMarchingCubeChunk chunk)
        {
            mainCam.enabled = false;
            Time.timeScale = 0;


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
                BuildRelevantChunksParallelWithAsyncGpuAround();
                StartCoroutine(WaitTillAsynGenerationDone());
            }

            // Debug.Log($"Number of chunks: {ChunkGroups.Count}");
        }

        //Todo: try do this work on compute shader already
        private void BuildRelevantChunksParallelWithAsyncGpuAround()
        {
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
                    CreateChunkWithAsyncGPUReadbackParallel(next);
                }
                
            }
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
                    while ((closestNeighbours.size == 0 && channeledChunks.Count > x.Count) /*|| channeledChunks > maxRunningThreads*/)
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
                    while ((closestNeighbours.size == 0 && channeledChunks.Count > x.Count) /*|| channeledChunks > maxRunningThreads*/)
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
            channeledChunks.Remove(chunk);

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

        protected bool lastChunkWasAir = true;

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
            Time.timeScale = 0;
            mainCam.enabled = false;
            FindNonEmptyChunkAroundAsync(pos, callback, 0);
        }

        protected void FindNonEmptyChunkAroundAsync(Vector3 pos, Action<ICompressedMarchingCubeChunk> callback, int tryCount)
        {
            //TODO:Remove trycount later
            if (tryCount++ >= 100)
                return;

            CreateChunkWithAsyncGPUReadback(pos, (c) => CheckChunk(c, callback, tryCount, ref pos));
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
        public void CreateChunkWithNoiseEdit(Vector3Int p, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance, out ICompressedMarchingCubeChunk chunk)
        {
            bool hasChunkAtPosition = chunkGroup.TryGetGroupItemAt(p, out chunk);

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

        protected ICompressedMarchingCubeChunk CreateChunkWithProperties(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride, Action<ComputeBuffer> WorkOnNoise = null)
        {
            ICompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(pos, lodPower, chunkSizePower, allowOverride);
            BuildChunk(chunk, WorkOnNoise);
            return chunk;
        }

        protected void CreateChunkWithAsyncGPUReadback(Vector3 pos, Action<ICompressedMarchingCubeChunk> callback, bool allowOverride = false)
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

        protected void CreateChunkWithAsyncGPUReadbackParallel(Vector3 pos, bool allowOverride = false)
        {
            int lodPower;
            int chunkSizePower;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSizePower, out lodPower);
            ICompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(VectorExtension.ToVector3Int(pos), lodPower, chunkSizePower, allowOverride);
            BuildChunkAsyncParallel(chunk);
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

        protected void SetChunkComponents(ICompressedMarchingCubeChunk chunk)
        {
            SetDisplayerOfChunk(chunk);
            SetLODColliderOfChunk(chunk);
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
        protected void BuildChunk(ICompressedMarchingCubeChunk chunk, Action<ComputeBuffer> WorkOnNoise = null)
        {
            TriangleChunkHeap ts = chunkGPURequest.DispatchAndGetShaderData(chunk, SetChunkComponents, WorkOnNoise);
            chunk.InitializeWithMeshData(ts);
        }

        protected Queue<ICompressedMarchingCubeChunk> readyParallelChunks = new Queue<ICompressedMarchingCubeChunk>();

        protected void BuildChunkParallel(ICompressedMarchingCubeChunk chunk)
        {
            channeledChunks.Add(chunk);
            TriangleChunkHeap ts = chunkGPURequest.DispatchAndGetShaderData(chunk, SetChunkComponents);
            chunk.InitializeWithMeshDataParallel(ts, readyParallelChunks);
        }

        protected void BuildChunkAsync(ICompressedMarchingCubeChunk chunk, Action<ICompressedMarchingCubeChunk> onChunkDone)
        {
            chunkGPURequest.DispatchAndGetShaderDataAsync(chunk, SetChunkComponents, (ts) =>
            {
                //chunk.InitializeWithMeshDataParallel(ts, readyParallelChunks);

                chunk.InitializeWithMeshData(ts);
                onChunkDone(chunk);
            });
        }

        protected void BuildChunkAsyncParallel(ICompressedMarchingCubeChunk chunk)
        {
            channeledChunks.Add(chunk);
            chunkGPURequest.DispatchAndGetShaderDataAsync(chunk, SetChunkComponents, (ts) =>
            {
                //chunk.InitializeWithMeshDataParallel(ts,(c)=>
                //{
                //    OnParallelChunkDoneCallBack(c);
                //    onChunkDone(c);
                //});

                chunk.InitializeWithMeshDataParallel(ts, readyParallelChunks);
            });
        }

        protected void BuildChunkAsyncParallel(ICompressedMarchingCubeChunk chunk, Action<ICompressedMarchingCubeChunk> onChunkDone)
        {
            channeledChunks.Add(chunk);
            chunkGPURequest.DispatchAndGetShaderDataAsync(chunk, SetChunkComponents, (ts) =>
            {
                chunk.InitializeWithMeshDataParallel(ts, (c) =>
                {
                    onChunkDone(c);
                });
            });
        }

        protected void OnChunkDataDone(TriangleChunkHeap chunkHeap)
        {
            if(chunkHeap.triCount == 0)
            {
                if(!hasFoundInitialChunk)
                {
                    lastChunkWasAir = true;
                    //DetermineIfChunkIsAir(noiseBuffer);
                }
            }
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
        protected void DispatchMultipleChunks(ICompressedMarchingCubeChunk[] chunks, Action<ICompressedMarchingCubeChunk> callbackPerChunk)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                BuildChunkAsyncParallel(chunks[i], callbackPerChunk);
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


        protected void DetermineIfChunkIsAir(ComputeBuffer pointsBuffer)
        {
            pointsArray = new float[1];
            pointsBuffer.GetData(pointsArray);
            lastChunkWasAir = pointsArray[0] < SURFACE_LEVEL;
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
                ExchangeSingleChunkAsyncParallel(chunk, chunk.AnchorPos, toLodPower, chunk.ChunkSizePower, true);
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

        protected void ExchangeSingleChunkAsyncParallel(ICompressedMarchingCubeChunk from, Vector3Int anchorPos, int lodPow, int sizePow, bool allowOveride)
        {
            from.PrepareDestruction();
            ExchangeChunkAsyncParallel(anchorPos, lodPow, sizePow, allowOveride, (c) => { FinishParallelChunk(from, c); });
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
            newChunk.InitializeWithMeshDataParallel(chunkGPURequest.DispatchAndGetShaderData(newChunk, SetChunkComponents), onChunkDone);
            return newChunk;
        }

        protected void ExchangeChunkAsyncParallel(Vector3Int anchorPos, int lodPow, int sizePow, bool allowOveride, Action<ICompressedMarchingCubeChunk> onChunkDone)
        {
            ICompressedMarchingCubeChunk newChunk = GetThreadedChunkObjectAt(anchorPos, lodPow, sizePow, allowOveride);
            BuildChunkAsyncParallel(newChunk, onChunkDone);
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
                    ExchangeSingleChunkAsyncParallel(chunk, chunk.CenterPos, toLodPower, chunk.ChunkSizePower, true);
                }
            }
        }


        public void MergeAndReduceChunkBranch(ICompressedMarchingCubeChunk chunk, int toLodPower)
        {
            List<ICompressedMarchingCubeChunk> oldChunks = new List<ICompressedMarchingCubeChunk>();
            chunk.Leaf.parent.PrepareBranchDestruction(oldChunks);

            ExchangeChunkAsyncParallel(chunk.CenterPos, toLodPower, chunk.ChunkSizePower + 1, true, (c) =>
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

        protected void CreatePools()
        {
            chunkPipelinePool = new ChunkGenerationPipelinePool(CreateChunkPipeline);
            minDegreesAtCoordBufferPool = new BufferPool(CreateMinDegreeBuffer, "minDegreeAtCoord", buildPreparedCubes);

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
            result.buildTrisShader = Instantiate(buildPreparedCubes);

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

        public void Store(Vector3Int anchorPos, IMarchingCubeChunk chunk, bool overrideNoise = false) => storageGroup.Store(anchorPos, chunk, overrideNoise);

        public bool TryLoadPoints(ICompressedMarchingCubeChunk marchingCubeChunk, out float[] loadedPoints) => storageGroup.TryLoadPoints(marchingCubeChunk,out loadedPoints);

        public float[] RequestNoiseForChunk(ICompressedMarchingCubeChunk chunk)
        {
            return chunkGPURequest.RequestNoiseForChunk(chunk);
        }

        public TriangleBuilder[] DispatchRebuildAround(IMarchingCubeChunk chunk, Action removeCubes, Vector3Int clickedIndex, Vector3 startVec, Vector3 endVec, float marchSquare)
        {
            prepareAroundShader.SetVector("editPoint", new Vector4(clickedIndex.x, clickedIndex.y, clickedIndex.z, 0));
            prepareAroundShader.SetVector("start", startVec);
            prepareAroundShader.SetVector("end", endVec);
            prepareAroundShader.SetVector("anchor", VectorExtension.ToVector4(chunk.AnchorPos));
            prepareAroundShader.SetInt("numPointsPerAxis", chunk.PointsPerAxis);
            prepareAroundShader.SetFloat("spacing", 1);
            prepareAroundShader.SetFloat("sqrRebuildRadius", marchSquare);


            Vector3Int threadsPerAxis = new Vector3Int(
               Mathf.CeilToInt((1 + (endVec.x - startVec.x)) / REBUILD_SHADER_THREAD_GROUP_SIZE),
               Mathf.CeilToInt((1 + (endVec.y - startVec.y)) / REBUILD_SHADER_THREAD_GROUP_SIZE),
               Mathf.CeilToInt((1 + (endVec.z - startVec.z)) / REBUILD_SHADER_THREAD_GROUP_SIZE)
               );

            return chunkGPURequest.DispatchRebuildAround(chunk, prepareAroundShader, removeCubes, threadsPerAxis);
        }

    }
}
