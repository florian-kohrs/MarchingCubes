﻿using MeshGPUInstanciation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using IChunkGroupRoot = MarchingCubes.IChunkGroupRoot<MarchingCubes.ICompressedMarchingCubeChunk>;


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

        public const int STORAGE_GROUP_SIZE = 128;

        public const int STORAGE_GROUP_SIZE_POWER = 7;

        public const int CHUNK_GROUP_SIZE = 1024;

        public const int CHUNK_GROUP_SIZE_POWER = 10;

        public const int DEFAULT_CHUNK_SIZE = 32;

        public const int DEFAULT_CHUNK_SIZE_POWER = 5;

        public const int DEFAULT_MIN_CHUNK_LOD_POWER = 0;

        public const int MAX_CHUNK_LOD_POWER = 5;

        public const int MAX_CHUNK_LOD_BIT_REPRESENTATION_SIZE = 3;

        public const int DESTROY_CHUNK_LOD = MAX_CHUNK_LOD_POWER + 2;

        public const int DEACTIVATE_CHUNK_LOD = MAX_CHUNK_LOD_POWER + 1;

        public const int VOXELS_IN_DEFAULT_SIZED_CHUNK = DEFAULT_CHUNK_SIZE * DEFAULT_CHUNK_SIZE * DEFAULT_CHUNK_SIZE;

        public Dictionary<Vector3Int, IChunkGroupRoot> chunkGroups = new Dictionary<Vector3Int, IChunkGroupRoot>();

        [Save]
        public Dictionary<Serializable3DIntVector, StorageTreeRoot> storageGroups = new Dictionary<Serializable3DIntVector, StorageTreeRoot>();

        protected float[] storedNoiseData;

        [Range(1, 253)]
        public int blockAroundPlayer = 16;

        private const int maxTrianglesLeft = 5000000;

        public ComputeShader marshShader;

        public ComputeShader rebuildShader;

        public ComputeShader noiseEditShader;


        [Header("Voxel Settings")]
        //public float boundsSize = 8;
        public Vector3 noiseOffset = Vector3.zero;

        public BiomScriptableObject[] bioms;

        //[Range(2, 100)]
        //public int numPointsPerAxis = 30;

        public AnimationCurve lodPowerForDistances;

        public AnimationCurve chunkSizePowerForDistances;


        //public Dictionary<Vector3Int, IMarchingCubeChunk> Chunks => chunks;

        //protected HashSet<BaseMeshChild> inUseDisplayer = new HashSet<BaseMeshChild>();


        protected MeshDisplayerPool displayerPool;

        protected InteractableMeshDisplayPool interactableDisplayerPool;

        protected SimpleChunkColliderPool simpleChunkColliderPool;



        protected int channeledChunks = 0;

        protected bool hasFoundInitialChunk;


        public int totalTriBuild;

        TriangleBuilder[] tris;// = new TriangleBuilder[CHUNK_VOLUME * 5];
        float[] pointsArray;

        private ComputeBuffer triangleBuffer;
        private ComputeBuffer pointsBuffer;
        private ComputeBuffer pointBiomIndex;
        private ComputeBuffer biomBuffer;
        private ComputeBuffer savedPointBuffer;
        private ComputeBuffer triCountBuffer;

        private DisposablePoolOf<ComputeBuffer> minDegreesAtCoordBufferPool;


        public WorldUpdater worldUpdater;

        public Transform colliderParent;




        public BaseDensityGenerator densityGenerator;

        public bool useTerrainNoise;


        public int deactivateAfterDistance = 40;

        public Material chunkMaterial;

        [Range(0, 1)]
        public float surfaceLevel = 0.45f;

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

        private void Start()
        {
            simpleChunkColliderPool = new SimpleChunkColliderPool(colliderParent);
            displayerPool = new MeshDisplayerPool(transform);
            interactableDisplayerPool = new InteractableMeshDisplayPool(transform);
            CreateAllBuffersWithSizes(32);

            TriangulationTableStaticData.BuildLookUpTables();

            InitializeDensityGenerator();

            ApplyShaderProperties(marshShader);

            ApplyShaderProperties(rebuildShader);
            noiseEditShader.SetBuffer(0, "points", pointsBuffer);

            watch.Start();
            buildAroundSqrDistance = (long)buildAroundDistance * buildAroundDistance;
            startPos = player.position;

            ICompressedMarchingCubeChunk chunk = FindNonEmptyChunkAround(player.position);
            maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;

            BuildRelevantChunksParallelBlockingAround(chunk);
        }

        protected void InitializeDensityGenerator()
        {
            densityGenerator.SetBioms(bioms.Select(b => b.biom).ToArray(), marshShader, rebuildShader);
            densityGenerator.SetBuffer(pointsBuffer, savedPointBuffer, pointBiomIndex);
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
                    isNextInProgress = HasChunkStartedAt(next);
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
                    isNextInProgress = HasChunkStartedAt(next);
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
                        && !HasChunkAtPosition(v3))
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

        protected ICompressedMarchingCubeChunk FindNonEmptyChunkAround(Vector3 pos)
        {
            bool isEmpty = true;
            Vector3Int chunkIndex;
            ICompressedMarchingCubeChunk chunk = null;
            int tryCount = 0;
            //TODO:Remove trycount later
            while (isEmpty && tryCount++ < 100)
            {
                chunkIndex = PositionToChunkGroupCoord(pos);
                chunk = CreateChunkAt(pos, chunkIndex);
                isEmpty = chunk.IsEmpty;
                if (chunk.IsEmpty)
                {
                    //TODO: maybe just read noise points here and completly remove isSolid or Air
                    if (chunk.IsCompletlyAir)
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

        protected void CreateChunkParallelAt(Vector3 pos)
        {
            CreateChunkParallelAt(pos, PositionToChunkGroupCoord(pos));
        }

        protected void CreateChunkParallelAt(Vector3 pos, Vector3Int coord)
        {
            int lodPower;
            int chunkSizePower;
            bool careForNeighbours;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSizePower, out lodPower, out careForNeighbours);

            ICompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(Vector3Int.FloorToInt(pos), coord, lodPower, chunkSizePower, false);
            BuildChunkParallel(chunk, careForNeighbours);
        }

        public bool TryGetOrCreateChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk)
        {
            if (!TryGetChunkAtPosition(p, out chunk))
            {
                chunk = CreateChunkWithProperties(p, PositionToChunkGroupCoord(p), 0, DEFAULT_CHUNK_SIZE_POWER, false, false);
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
            bool hasChunkAtPosition = TryGetChunkAtPosition(p, out chunk);

            if (!hasChunkAtPosition || !chunk.HasStarted)
            {
                if (chunk != null)
                {
                    chunk.DestroyChunk();
                }
                chunk = CreateChunkWithProperties(p, PositionToChunkGroupCoord(p), 0, DEFAULT_CHUNK_SIZE_POWER, false, false,
                    () => {
                        ApplyNoiseEditing(33, editPoint, start, end, delta, maxDistance);
                    });
                createdChunk = true;
            }
            return createdChunk;
        }

        protected ICompressedMarchingCubeChunk CreateChunkAt(Vector3Int p, bool allowOverride = false)
        {
            return CreateChunkAt(p, PositionToChunkGroupCoord(p), allowOverride);
        }

        //TODO:Check if collider can be removed from most chunks.
        //Collision can be approximated by calling noise function for lowest point of object and checking if its noise is larger than surface value

        protected ICompressedMarchingCubeChunk CreateChunkAt(Vector3 pos, Vector3Int coord, bool allowOverride = false)
        {
            int lodPower;
            int chunkSizePower;
            bool careForNeighbours;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSizePower, out lodPower, out careForNeighbours);
            return CreateChunkWithProperties(VectorExtension.ToVector3Int(pos), coord, lodPower, chunkSizePower, careForNeighbours, allowOverride);
        }

        protected ICompressedMarchingCubeChunk CreateChunkWithProperties(Vector3Int pos, Vector3Int coord, int lodPower, int chunkSizePower, bool careForNeighbours, bool allowOverride, Action WorkOnNoise = null)
        {
            ICompressedMarchingCubeChunk chunk = GetThreadedChunkObjectAt(pos, coord, lodPower, chunkSizePower, allowOverride);
            BuildChunk(chunk, careForNeighbours, WorkOnNoise);
            return chunk;
        }


        protected IChunkGroupRoot CreateChunkGroupAtCoordinate(Vector3Int coord)
        {
            IChunkGroupRoot chunkGroup = new ChunkGroupRoot(new int[] { coord.x, coord.y, coord.z });
            chunkGroups.Add(coord, chunkGroup);
            return chunkGroup;
        }

        protected StorageTreeRoot CreateStorageGroupAtCoordinate(Vector3Int coord)
        {
            StorageTreeRoot chunkGroup = new StorageTreeRoot(new int[] { coord.x, coord.y, coord.z });
            storageGroups.Add(coord, chunkGroup);
            return chunkGroup;
        }

        public bool TryGetChunkGroupAt(Vector3Int p, out IChunkGroupRoot chunkGroup)
        {
            Vector3Int coord = PositionToChunkGroupCoord(p);
            return chunkGroups.TryGetValue(coord, out chunkGroup);
        }

        public IChunkGroupRoot GetOrCreateChunkGroupAtCoordinate(Vector3 p)
        {
            return GetOrCreateChunkGroupAtCoordinate(PositionToChunkGroupCoord(p));
        }

        public IChunkGroupRoot GetOrCreateChunkGroupAtCoordinate(Vector3Int coord)
        {
            IChunkGroupRoot chunkGroup;
            if (!chunkGroups.TryGetValue(coord, out chunkGroup))
            {
                chunkGroup = CreateChunkGroupAtCoordinate(coord);
            }
            return chunkGroup;
        }

        public StorageTreeRoot GetOrCreateStorageGroupAtCoordinate(Vector3Int coord)
        {
            StorageTreeRoot chunkGroup;
            if (!storageGroups.TryGetValue(coord, out chunkGroup))
            {
                chunkGroup = CreateStorageGroupAtCoordinate(coord);
            }
            return chunkGroup;
        }

        //TODO: Has to mark mipmaps dirty if any child changes!
        public bool TryGetMipMapAt(Vector3Int pos, int sizePower, out float[] storedNoise, out bool isMipMapComplete)
        {
            if (storageGroups.TryGetValue(PositionToStorageGroupCoord(pos), out StorageTreeRoot chunkGroup))
            {
                return chunkGroup.TryGetMipMapOfChunkSizePower(new int[] { pos.x, pos.y, pos.z }, sizePower, out storedNoise, out isMipMapComplete);
            }
            isMipMapComplete = false;
            storedNoise = null;
            return false;
        }

        public bool TryGetStoredEditsAt(Vector3Int pos, out StoredChunkEdits edits)
        {
            Vector3Int coord = PositionToStorageGroupCoord(pos);
            if (storageGroups.TryGetValue(coord, out StorageTreeRoot chunkGroup))
            {
                if (chunkGroup.HasChild)
                {
                    if (chunkGroup.TryGetLeafAtGlobalPosition(pos, out edits))
                    {
                        return true;
                    }
                }
            }
            edits = null;
            return false;
        }

        protected bool HasChunkAtPosition(Vector3Int v3)
        {
            ICompressedMarchingCubeChunk _;
            return TryGetChunkAtPosition(v3, out _);
        }

        public bool TryGetChunkAtPosition(Vector3Int p, out ICompressedMarchingCubeChunk chunk)
        {
            Vector3Int _;
            return TryGetChunkAtPosition(p, out chunk, out _);
        }

        public bool TryGetChunkAtPosition(Vector3Int p, out ICompressedMarchingCubeChunk chunk, out Vector3Int positionInOtherChunk)
        {
            Vector3Int coord = PositionToChunkGroupCoord(p);
            IChunkGroupRoot chunkGroup;
            if (chunkGroups.TryGetValue(coord, out chunkGroup))
            {
                if (chunkGroup.HasChild)
                {
                    if (/*chunkGroup.HasChild && */chunkGroup.TryGetLeafAtGlobalPosition(p, out chunk))
                    {
                        positionInOtherChunk = p - chunk.AnchorPos;
                        return true;
                    }
                }
                else
                {
                    Debug.LogWarning("Chunk is nt set yet -> may loose neighbours");
                }
            }
            chunk = null;
            positionInOtherChunk = default;
            return false;
        }

        public bool TryGetReadyChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk)
        {
            return TryGetChunkAtPosition(p, out chunk) && chunk.IsReady;
        }

        public bool TryGetReadyChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk, out Vector3Int relativePositionInChunk)
        {
            return TryGetChunkAtPosition(p, out chunk, out relativePositionInChunk) && chunk.IsReady;
        }


        public bool HasChunkStartedAt(Vector3Int p)
        {
            if (TryGetChunkAtPosition(p, out ICompressedMarchingCubeChunk chunk))
            {
                return chunk.HasStarted;
            }
            return false;
        }

        protected ICompressedMarchingCubeChunk GetChunkObjectAt(ICompressedMarchingCubeChunk chunk, Vector3Int position, Vector3Int coord, int lodPower, int chunkSizePower, bool allowOverride)
        {
            ///Pot racecondition
            IChunkGroupRoot chunkGroup = GetOrCreateChunkGroupAtCoordinate(coord);

            chunk.ChunkHandler = this;
            chunk.ChunkSizePower = chunkSizePower;
            chunk.ChunkUpdater = worldUpdater;
            chunk.Material = chunkMaterial;
            chunk.SurfaceLevel = surfaceLevel;
            chunk.LODPower = lodPower;

            chunkGroup.SetLeafAtPosition(new int[] { position.x, position.y, position.z }, chunk, allowOverride);

            return chunk;
        }

        public void BuildEmptyChunkAt(Vector3Int pos)
        {
            IChunkGroupRoot chunkGroup = GetOrCreateChunkGroupAtCoordinate(PositionToChunkGroupCoord(pos));
            if (!chunkGroup.TryGetLeafAtGlobalPosition(pos, out ICompressedMarchingCubeChunk chunk))
            {
                chunk = new CompressedMarchingCubeChunk();

                chunk.ChunkHandler = this;
                chunk.ChunkSizePower = CHUNK_GROUP_SIZE_POWER;
                chunk.ChunkUpdater = worldUpdater;
                chunk.Material = chunkMaterial;
                chunk.SurfaceLevel = surfaceLevel;
                chunk.LODPower = MAX_CHUNK_LOD_POWER + 1;

                chunk.IsSpawner = true;

                chunkGroup.SetLeafAtPosition(new int[] { pos.x, pos.y, pos.z }, chunk, false);

                simpleChunkColliderPool.GetItemFromPoolFor(chunk);
            }
        }


        protected ICompressedMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride)
        {
            return GetThreadedChunkObjectAt(pos, PositionToChunkGroupCoord(pos), lodPower, chunkSizePower, allowOverride);
        }


        protected ICompressedMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int position, Vector3Int coord, int lodPower, int chunkSizePower, bool allowOverride)
        {
            if (lodPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
            {
                MarchingCubeChunk chunk = new MarchingCubeChunk();
                chunk.rebuildShader = rebuildShader;
                chunk.rebuildTriCounter = triCountBuffer;
                chunk.rebuildTriResult = triangleBuffer;
                chunk.rebuildNoiseBuffer = pointsBuffer;
                return GetChunkObjectAt(chunk, position, coord, lodPower, chunkSizePower, allowOverride);
            }
            else
            {
                return GetChunkObjectAt(new CompressedMarchingCubeChunk(), position, coord, lodPower, chunkSizePower, allowOverride);
            }
        }

        protected Vector3Int CoordToPosition(Vector3Int coord)
        {
            return coord * CHUNK_GROUP_SIZE;
        }

        protected Vector3Int PositionToChunkGroupCoord(Vector3 pos)
        {
            return PositionToChunkGroupCoord(pos.x, pos.y, pos.z);
        }

        protected Vector3Int PositionToChunkGroupCoord(Vector3Int pos)
        {
            return PositionToChunkGroupCoord(pos.x, pos.y, pos.z);
        }
        protected Vector3Int PositionToChunkGroupCoord(float x, float y, float z)
        {
            return new Vector3Int(
                Mathf.FloorToInt(x / CHUNK_GROUP_SIZE),
                Mathf.FloorToInt(y / CHUNK_GROUP_SIZE),
                Mathf.FloorToInt(z / CHUNK_GROUP_SIZE));
        }

        protected Vector3Int PositionToStorageGroupCoord(Vector3 pos)
        {
            return PositionToStorageGroupCoord(pos.x, pos.y, pos.z);
        }


        protected Vector3Int PositionToStorageGroupCoord(Vector3Int pos)
        {
            return PositionToStorageGroupCoord(pos.x, pos.y, pos.z);
        }

        protected Vector3Int PositionToStorageGroupCoord(float x, float y, float z)
        {
            return new Vector3Int(
                Mathf.FloorToInt(x / STORAGE_GROUP_SIZE),
                Mathf.FloorToInt(y / STORAGE_GROUP_SIZE),
                Mathf.FloorToInt(z / STORAGE_GROUP_SIZE));
        }


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

        protected void PrepareChunkToStoreMinDegreesIfNeeded(ICompressedMarchingCubeChunk chunk)
        {
            bool storeMinDegree = chunk.LOD <= 2 && !chunk.IsReady;
            marshShader.SetBool("storeMinDegrees", storeMinDegree);
            if (storeMinDegree)
            {
                ComputeBuffer minDegreeBuffer = minDegreesAtCoordBufferPool.GetItemFromPool();
                marshShader.SetBuffer(0, "minDegreeAtCoord", minDegreeBuffer);
                chunk.MinDegreeBuffer = minDegreeBuffer;
            }
        }

        //TODO:Remove keep points
        protected void BuildChunk(ICompressedMarchingCubeChunk chunk, bool careForNeighbours, Action WorkOnNoise = null)
        {
            TriangleChunkHeap ts = DispatchAndGetShaderData(chunk, careForNeighbours, WorkOnNoise);
            chunk.InitializeWithMeshData(ts);
        }

        protected Queue<ICompressedMarchingCubeChunk> readyParallelChunks = new Queue<ICompressedMarchingCubeChunk>();

        protected void BuildChunkParallel(ICompressedMarchingCubeChunk chunk, bool careForNeighbours)
        {
            TriangleChunkHeap ts = DispatchAndGetShaderData(chunk, careForNeighbours);
            channeledChunks++;
            chunk.InitializeWithMeshDataParallel(ts, readyParallelChunks);
        }


        public float[] RequestNoiseForChunk(ICompressedMarchingCubeChunk chunk)
        {
            float[] result;
            if(!TryLoadNoise(chunk.AnchorPos, chunk.ChunkSizePower, out result, out bool _))
            {
                result = GenerateAndGetNoiseForChunk(chunk);
            }
            return result;
        }

        public float[] GenerateAndGetNoiseForChunk(ICompressedMarchingCubeChunk chunk)
        {
            float[] result;
            int pointsPerAxis = chunk.PointsPerAxis;
            GenerateNoise(chunk.ChunkSizePower, pointsPerAxis, chunk.LOD, chunk.AnchorPos);
            result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            pointsBuffer.GetData(result, 0, 0, result.Length);
            return result;
        }


        public bool TryLoadPoints(ICompressedMarchingCubeChunk chunk, out float[] loadedPoints)
        {
            return TryGetMipMapAt(chunk.AnchorPos, chunk.ChunkSizePower, out loadedPoints, out bool complete) && complete;
        }

        public void SetEditedNoiseAtPosition(IMarchingCubeChunk chunk, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance)
        {
            int pointsPerAxis = chunk.PointsPerAxis;
            float[] result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            GenerateNoise(chunk.ChunkSizePower, pointsPerAxis, chunk.LOD, chunk.AnchorPos);
            ApplyNoiseEditing(pointsPerAxis, editPoint, start, end, delta, maxDistance);
            pointsBuffer.GetData(result, 0, 0, result.Length);
            chunk.Points = result;
            Store(chunk.AnchorPos, chunk);
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

        public void GenerateNoise(int sizePow, int pointsPerAxis, int LOD, Vector3Int anchor, bool loadNoiseData = true)
        {
            if (loadNoiseData)
            {
                TryLoadOrGenerateNoise(sizePow, pointsPerAxis, LOD, anchor);
            }
            else
            {
                densityGenerator.Generate(pointsPerAxis, anchor, LOD);
            }
        }

        protected void TryLoadOrGenerateNoise(int sizePow, int pointsPerAxis, int lod, Vector3Int anchor)
        {
            bool hasStoredData = false;
            bool isMipMapComplete = false;
            bool hasToDispatch = pointsPerAxis < DEFAULT_CHUNK_SIZE;
            if (sizePow <= STORAGE_GROUP_SIZE_POWER)
            {
                hasStoredData = TryLoadNoise(anchor, sizePow, out storedNoiseData, out isMipMapComplete);
                if (hasStoredData && (!isMipMapComplete || hasToDispatch))
                {
                    savedPointBuffer.SetData(storedNoiseData);
                }
            }
            if (isMipMapComplete && !hasToDispatch)
            {
                pointsBuffer.SetData(storedNoiseData);
                pointsArray = storedNoiseData;
            }
            else
            {
                densityGenerator.Generate(pointsPerAxis, anchor, lod, hasStoredData);
            }

        }

        protected bool TryLoadNoise(Vector3Int anchor, int sizePow, out float[] noise, out bool isMipMapComplete)
        {
            return TryGetMipMapAt(anchor, sizePow, out noise, out isMipMapComplete) && isMipMapComplete;
        }

        //TODO: Maybe remove pooling theese -> could reduce size of buffer for faster reads
        protected TriangleChunkHeap[] DispatchMultipleChunks(ICompressedMarchingCubeChunk[] chunks)
        {
            triangleBuffer.SetCounterValue(0);
            int chunkLength = chunks.Length;
            for (int i = 0; i < chunkLength; i++)
            {
                ICompressedMarchingCubeChunk c = chunks[i];
                GenerateNoise(c.ChunkSizePower, c.PointsPerAxis, c.LOD, c.AnchorPos);
                AccumulateCubesFromNoise(c, i);
            }
            int[] triCounts = new int[chunkLength];

            for (int i = 0; i < chunkLength; i++)
            {
                //TODO:check if this reduces wait time from gpu
                SetDisplayerOfChunk(chunks[i]);
                simpleChunkColliderPool.GetItemFromPoolFor(chunks[i]);
            }

            triCountBuffer.GetData(triCounts, 0, 0, chunkLength);
            TriangleChunkHeap[] result = new TriangleChunkHeap[chunkLength];
            TriangleBuilder[] allTris = new TriangleBuilder[triCounts[triCounts.Length - 1]];
            triangleBuffer.GetData(allTris, 0, 0, allTris.Length);
            int last = 0;
            for (int i = 0; i < chunkLength; i++)
            {
                int current = triCounts[i];
                int length = current - last;
                result[i] = new TriangleChunkHeap(allTris, last, length);
                last = current;

                if (length == 0)
                {
                    chunks[i].FreeSimpleChunkCollider();
                }
            }
            return result;
        }

        int numTris;

        //TODO: Inform about Mesh subset and mesh set vertex buffer
        //Subset may be used to only change parts of the mesh -> dont need multiple mesh displayers with submeshes?
        protected TriangleChunkHeap DispatchAndGetShaderData(ICompressedMarchingCubeChunk chunk, bool careForNeighbours, Action WorkOnNoise = null)
        {
            int lod = chunk.LOD;
            int chunkSize = chunk.ChunkSize;

            if (chunkSize % lod != 0)
                throw new Exception("Lod must be divisor of chunksize");

            int numVoxelsPerAxis = chunkSize / lod;
            int pointsPerAxis = numVoxelsPerAxis + 1;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;

            GenerateNoise(chunk.ChunkSizePower, pointsPerAxis, lod, chunk.AnchorPos);

            bool storeNoise = false;
            if (WorkOnNoise != null)
            {
                if(!(chunk is IMarchingCubeChunk))
                {
                    throw new ArgumentException("Chunk has to be storeable to be able to store requested noise!");
                }
                WorkOnNoise();
                storeNoise = true;
            }


            ComputeCubesFromNoise(chunk, lod);

            ///Do work for chunk here, before data from gpu is read, to give gpu time to finish

            SetDisplayerOfChunk(chunk);
            simpleChunkColliderPool.GetItemFromPoolFor(chunk);

            ///read data from gpu

            numTris = ReadCurrentTriangleData(out tris);

            if (numTris == 0)
            {
                chunk.FreeSimpleChunkCollider();
                chunk.GiveUnusedDisplayerBack();
            }

            if (storeNoise || (numTris == 0 && !hasFoundInitialChunk) || careForNeighbours)
            {
                if (careForNeighbours || storeNoise)
                {
                    pointsArray = new float[pointsVolume];
                }
                else
                {
                    pointsArray = new float[1];
                }
                pointsBuffer.GetData(pointsArray, 0, 0, pointsArray.Length);
                chunk.Points = pointsArray;
                if (storeNoise)
                {
                    Store(chunk.AnchorPos, chunk as IMarchingCubeChunk, true);

                }
            }
            return new TriangleChunkHeap(tris, 0, numTris);
        }

        public TriangleBuilder[] GenerateCubesFromNoise(ICompressedMarchingCubeChunk chunk, int triCount, float[] noise)
        {
            pointsBuffer.SetData(noise);
            RequestCubesFromNoise(chunk, chunk.LOD, triCount);
            return tris;
        }

        public void AccumulateCubesFromNoise(ICompressedMarchingCubeChunk chunk, int offest)
        {
            ComputeCubesFromNoise(chunk, chunk.LOD, false);
            ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, offest * 4);
        }

        public int RequestCubesFromNoise(ICompressedMarchingCubeChunk chunk, int lod, int triCount = -1)
        {
            ComputeCubesFromNoise(chunk, lod);
            return ReadCurrentTriangleData(out tris, triCount);
        }

        public int ReadCurrentTriangleData(out TriangleBuilder[] tris, int triCount = -1)
        {
            if (triCount < 0)
            {
                ///Get number of triangles in the triangle buffer
                ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
                int[] triCountArray = new int[1];
                triCountBuffer.GetData(triCountArray);
                triCount = triCountArray[0];
            }

            ///Get triangle data from shader

            //TODO: Check if this changes performance
            if (triCount > 0)
            {
                tris = new TriangleBuilder[triCount];
                triangleBuffer.GetData(tris, 0, 0, triCount);
            }
            else
            {
                tris = null;
            }

            totalTriBuild += triCount;
            return triCount;
        }

        public void ComputeCubesFromNoise(ICompressedMarchingCubeChunk chunk, int lod, bool resetCounter = true)
        {
            int chunkSize = chunk.ChunkSize;
            Vector3Int anchor = chunk.AnchorPos;

            PrepareChunkToStoreMinDegreesIfNeeded(chunk);

            int numVoxelsPerAxis = chunkSize / lod;
            int pointsPerAxis = numVoxelsPerAxis + 1;

            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

            float spacing = lod;

            if (resetCounter)
            {
                triangleBuffer.SetCounterValue(0);
            }

            //TODO: Check if this needs to be checkd or if correct value is still set
            marshShader.SetInt("numPointsPerAxis", pointsPerAxis);
            marshShader.SetFloat("spacing", spacing);
            marshShader.SetVector("anchor", new Vector4(anchor.x, anchor.y, anchor.z));

            marshShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
        }

        public Color32 GetColor(PathTriangle t, int steepness)
        {
            float invLerp = (steepness - minSteepness) / ((float)maxSteepness - minSteepness);
            if (invLerp < 0)
                invLerp = 0;
            else if (invLerp > 1)
                invLerp = 1;

            return new Color32(15, 150, 15, (byte)steepness);
        }

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
                ExchangeSingleChunkParallel(chunk, chunk.AnchorPos, toLodPower, chunk.ChunkSizePower, false, true);
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

        protected ICompressedMarchingCubeChunk ExchangeSingleChunkParallel(ICompressedMarchingCubeChunk from, Vector3Int anchorPos, int lodPow, int sizePow, bool careForNeighbours, bool allowOveride)
        {
            from.PrepareDestruction();
            return ExchangeChunkParallel(anchorPos, lodPow, sizePow, careForNeighbours, allowOveride, (c) => { FinishParallelChunk(from, c); });
        }


        protected void FinishParallelChunk(ICompressedMarchingCubeChunk from, ICompressedMarchingCubeChunk newChunk)
        {
            lock (exchangeLocker)
            {
                worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(from, newChunk));
            }
        }


        protected ICompressedMarchingCubeChunk ExchangeChunkParallel(Vector3Int anchorPos, int lodPow, int sizePow, bool careForNeighbours, bool allowOveride, Action<ICompressedMarchingCubeChunk> onChunkDone)
        {
            ICompressedMarchingCubeChunk newChunk = GetThreadedChunkObjectAt(anchorPos, lodPow, sizePow, allowOveride);
            newChunk.InitializeWithMeshDataParallel(DispatchAndGetShaderData(newChunk, careForNeighbours), onChunkDone);
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
                newChunks[i] = GetThreadedChunkObjectAt(v3, PositionToChunkGroupCoord(v3), toLodPower, newSizePow, true);
            }
            chunk.PrepareDestruction();
            TriangleChunkHeap[] tris = DispatchMultipleChunks(newChunks);
            object listLock = new object();
            List<ICompressedMarchingCubeChunk> chunks = new List<ICompressedMarchingCubeChunk>();
            for (int i = 0; i < 8; i++)
            {
                newChunks[i].InitializeWithMeshDataParallel(tris[i], (c) =>
                {
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
                });
            }
        }


        protected Vector3Int IntArrToVector3(int[] arr) => new Vector3Int(arr[0], arr[1], arr[2]);

        protected int NumberOfSavedChunksAt(Vector3Int pos, int sizePow)
        {
            Vector3Int coord = PositionToStorageGroupCoord(pos);
            StorageTreeRoot r;
            if (storageGroups.TryGetValue(coord, out r))
            {
                IStorageGroupOrganizer<StoredChunkEdits> node;
                if (r.TryGetNodeWithSizePower(new int[] { pos.x, pos.y, pos.z }, sizePow, out node))
                {
                    return node.ChildrenWithMipMapReady;
                }
            }
            return 0;
        }

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
                    ExchangeSingleChunkParallel(chunk, chunk.CenterPos, toLodPower, chunk.ChunkSizePower, false, true);
                }
            }
        }


        public void MergeAndReduceChunkBranch(ICompressedMarchingCubeChunk chunk, int toLodPower)
        {
            List<ICompressedMarchingCubeChunk> oldChunks = new List<ICompressedMarchingCubeChunk>();
            chunk.Leaf.parent.PrepareBranchDestruction(oldChunks);

            ExchangeChunkParallel(chunk.CenterPos, toLodPower, chunk.ChunkSizePower + 1, false, true, (c) =>
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

        public void FreeMeshDisplayer(MarchingCubeMeshDisplayer display)
        {
            TakeMeshDisplayerBack(display);
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


        public void GetSizeAndLodPowerForChunkPosition(Vector3 pos, out int sizePower, out int lodPower, out bool careForNeighbours)
        {
            float distance = (startPos - pos).magnitude;
            lodPower = GetLodPower(distance);
            sizePower = GetSizePowerForChunkAtDistance(distance);
            //TODO: check this
            careForNeighbours = GetLodPower(distance + sizePower) > lodPower;
        }

        protected int RoundToPowerOf2(float f)
        {
            int r = (int)Mathf.Pow(2, Mathf.RoundToInt(f));

            return Mathf.Max(1, r);
        }

        protected void CreateAllBuffersWithSizes(int numVoxelsPerAxis)
        {
            int points = numVoxelsPerAxis + 1;
            int numPoints = points * points * points;
            int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            int maxTriangleCount = numVoxels * 2;
            maxTriangleCount *= MAX_CHUNKS_PER_ITERATION;

            biomBuffer = new ComputeBuffer(bioms.Length, BiomColor.SIZE);
            var b = bioms.Select(b => new BiomColor(b.visualizationData)).ToArray();
            biomBuffer.SetData(b);

            var envirenmentBioms = bioms.Select(b => b.envirenmentData).ToArray();

            minDegreesAtCoordBufferPool = new DisposablePoolOf<ComputeBuffer>(CreateMinDegreeBuffer);

            pointBiomIndex = new ComputeBuffer(numPoints, sizeof(uint));
            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
            savedPointBuffer = new ComputeBuffer(numPoints, sizeof(float));
            triangleBuffer = new ComputeBuffer(maxTriangleCount, TriangleBuilder.SIZE_OF_TRI_BUILD, ComputeBufferType.Append);
            triCountBuffer = new ComputeBuffer(MAX_CHUNKS_PER_ITERATION, sizeof(int), ComputeBufferType.Raw);
        }

        protected ComputeBuffer CreateMinDegreeBuffer()
        {
            return new ComputeBuffer(VOXELS_IN_DEFAULT_SIZED_CHUNK, sizeof(float));
        }

        protected const int MAX_CHUNKS_PER_ITERATION = 8;

        protected void ApplyShaderProperties(ComputeShader s)
        {
            s.SetBuffer(0, "points", pointsBuffer);
            s.SetBuffer(0, "pointBiomIndex", pointBiomIndex);
            s.SetBuffer(0, "savedPoints", savedPointBuffer);
            s.SetBuffer(0, "triangles", triangleBuffer);
            s.SetBuffer(0, "biomsViz", biomBuffer);

            s.SetInt("minSteepness", minSteepness);
            s.SetInt("maxSteepness", maxSteepness);
            s.SetFloat("surfaceLevel", surfaceLevel);
        }

        protected void ReleaseBuffers()
        {
            if (triangleBuffer != null)
            {
                biomBuffer.Dispose();
                pointBiomIndex.Dispose();
                triangleBuffer.SetCounterValue(0);
                triangleBuffer.Dispose();
                pointsBuffer.Dispose();
                savedPointBuffer.Dispose();
                minDegreesAtCoordBufferPool.DisposeAll();
                triCountBuffer.Dispose();
                triangleBuffer = null;
            }

        }

        protected override void onDestroy()
        {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }
        }

        //TODO: Dont store when chunk knows he stored before

        public void Store(Vector3Int anchorPos, IMarchingCubeChunk chunk, bool overrideNoise = false)
        {
            if (!TryGetStoredEditsAt(anchorPos, out StoredChunkEdits edits) || overrideNoise)
            {
                edits = new StoredChunkEdits();
                StorageTreeRoot r = GetOrCreateStorageGroupAtCoordinate(PositionToStorageGroupCoord(anchorPos));

                chunk.StoreChunk(edits);

                r.SetLeafAtPosition(anchorPos, edits, true);
                //call all instantiableData from chunk that need to be stored
                //(everything not depending on triangles only, e.g trees )
            }
        }

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
            environmentSpawner.AddEnvironmentForOriginalChunk(environmentChunk);
        }

    }
}
