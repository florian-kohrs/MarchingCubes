using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using IChunkGroupRoot = MarchingCubes.IChunkGroupRoot<MarchingCubes.IMarchingCubeChunk>;


namespace MarchingCubes
{

    [System.Serializable]
    public class MarchingCubeChunkHandler : SaveableMonoBehaviour, IMarchingCubeChunkHandler
    {

        protected int kernelId;

        protected const int threadGroupSize = 8;

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

        public Dictionary<Vector3Int, IChunkGroupRoot> chunkGroups = new Dictionary<Vector3Int, IChunkGroupRoot>();

        [Save]
        public Dictionary<Serializable3DIntVector, StorageTreeRoot> storageGroups = new Dictionary<Serializable3DIntVector, StorageTreeRoot>();

        protected float[] storedNoiseData;

        public Dictionary<Vector3Int, IChunkGroupRoot> ChunkGroups => chunkGroups;

        [Range(1, 253)]
        public int blockAroundPlayer = 16;

        private const int maxTrianglesLeft = 5000000;

        public ComputeShader marshShader;


        [Header("Voxel Settings")]
        //public float boundsSize = 8;
        public Vector3 noiseOffset = Vector3.zero;

        //[Range(2, 100)]
        //public int numPointsPerAxis = 30;

        public AnimationCurve lodPowerForDistances;

        public AnimationCurve chunkSizePowerForDistances;


        //public Dictionary<Vector3Int, IMarchingCubeChunk> Chunks => chunks;

        //protected HashSet<BaseMeshChild> inUseDisplayer = new HashSet<BaseMeshChild>();


        public void StartWaitForParralelChunkDoneCoroutine(IEnumerator e)
        {
            StartCoroutine(e);
        }


        protected Stack<BaseMeshDisplayer> unusedDisplayer = new Stack<BaseMeshDisplayer>();

        protected Stack<BaseMeshDisplayer> unusedInteractableDisplayer = new Stack<BaseMeshDisplayer>();

        protected Stack<ChunkLodCollider> freechunkCollider = new Stack<ChunkLodCollider>();


        public void SetChunkColliderOf(IMarchingCubeChunk c)
        {
            ChunkLodCollider collider;
            if (freechunkCollider.Count > 0)
            {
                collider = freechunkCollider.Pop();
                collider.chunk = c;
                collider.transform.position = c.CenterPos;
                c.ChunkSimpleCollider = collider;
                collider.coll.enabled = true;
            }
            else
            {
                BuildAndSetLodColliderForChunk(c);
            }
        }

        public void FreeCollider(ChunkLodCollider c)
        {
            c.coll.enabled = false;
            //c.chunk = null;
            freechunkCollider.Push(c);
        }

        public BaseMeshDisplayer GetNextMeshDisplayer()
        {
            BaseMeshDisplayer displayer;
            if (unusedDisplayer.Count > 0)
            {
                displayer = unusedDisplayer.Pop();
            }
            else
            {
                displayer = new BaseMeshDisplayer(transform);
            }
            return displayer;
        }

        public BaseMeshDisplayer GetNextInteractableMeshDisplayer(IMarchingCubeInteractableChunk forChunk)
        {
            BaseMeshDisplayer displayer;
            if (unusedInteractableDisplayer.Count > 0)
            {
                displayer = unusedInteractableDisplayer.Pop();
                displayer.SetInteractableChunk(forChunk);
            }
            else if (unusedDisplayer.Count > 0)
            {
                displayer = unusedDisplayer.Pop();
                displayer.SetInteractableChunk(forChunk);
            }
            else
            {
                displayer = new BaseMeshDisplayer(forChunk, transform);
            }
            return displayer;
        }


        public void FreeMeshDisplayer(BaseMeshDisplayer display)
        {
            if (display.HasCollider)
            {
                unusedInteractableDisplayer.Push(display);
            }
            else
            {
                unusedDisplayer.Push(display);
            }
            display.Reset();
        }

        public void FreeAllDisplayers(List<BaseMeshDisplayer> displayers)
        {
            for (int i = 0; i < displayers.Count; ++i)
            {
                FreeMeshDisplayer(displayers[i]);
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
            careForNeighbours = GetLodPower(distance + sizePower) > lodPower;
        }

        protected int RoundToPowerOf2(float f)
        {
            int r = (int)Mathf.Pow(2, Mathf.RoundToInt(f));

            return Mathf.Max(1, r);
        }

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

        //TODO:GPU instancing from script generated meshes and add simple colliders as game objects

        private void Start()
        {
            CreateAllBuffersWithSizes(65);

            TriangulationTableStaticData.BuildLookUpTables();

            flatR = (int)(flatColor.r * 255);
            flatG = (int)(flatColor.g * 255);
            flatB = (int)(flatColor.b * 255);

            steepR = (int)(steepColor.r * 255);
            steepG = (int)(steepColor.g * 255);
            steepB = (int)(steepColor.b * 255);

            densityGenerator.SetBuffer(pointsBuffer, savedPointBuffer);
            ApplyShaderProperties();

            watch.Start();
            buildAroundSqrDistance = (long)buildAroundDistance * buildAroundDistance;
            kernelId = marshShader.FindKernel("March");
            startPos = player.position;
            IMarchingCubeChunk chunk = FindNonEmptyChunkAround(player.position);
            maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;
            //BuildRelevantChunksAround(chunk);
            BuildRelevantChunksParallelBlockingAround(chunk);
            //StartCoroutine(BuildRelevantChunksParallelAround(chunk));
        }



        private void Update()
        {
            //CheckChunksAround(player.position);
        }

        protected IEnumerator UpdateChunks()
        {
            yield return null;


            yield return new WaitForSeconds(3);

            yield return UpdateChunks();
        }

        protected int marshCounter;

        protected Vector3 startPos;
        protected float maxSqrChunkDistance;
        protected Queue<Vector3Int> neighbours = new Queue<Vector3Int>();

        protected BinaryHeap<float, Vector3Int> closestNeighbours = new BinaryHeap<float, Vector3Int>(float.MinValue, float.MaxValue, 200);

        public void BuildRelevantChunksAround(IMarchingCubeChunk chunk)
        {
            Queue<IMarchingCubeChunk> neighboursToBuild = new Queue<IMarchingCubeChunk>();
            neighboursToBuild.Enqueue(chunk);
            while (neighboursToBuild.Count > 0)
            {
                IMarchingCubeChunk current = neighboursToBuild.Dequeue();
                bool[] dirs = current.HasNeighbourInDirection;
                int count = dirs.Length;
                for (int i = 0; i < count; i++)
                {
                    if (!dirs[i])
                        continue;
                    Vector3Int v3 = VectorExtension.GetDirectionFromIndex(i) * (current.ChunkSize + 1) + current.CenterPos;
                    if (!HasChunkAtPosition(v3) && (startPos - v3).magnitude < buildAroundDistance)
                    {
                        neighboursToBuild.Enqueue(CreateChunkAt(v3));
                        if (totalTriBuild >= maxTrianglesLeft)
                        {
                            Debug.Log("Aborted");
                            neighboursToBuild.Clear();
                            break;
                        }
                    }
                }
            }

            watch.Stop();
            Debug.Log("Total millis: " + watch.Elapsed.TotalMilliseconds);

            Debug.Log("Total triangles: " + totalTriBuild);

            Debug.Log($"Number of chunkgroups: {ChunkGroups.Count}");
        }


        public void BuildRelevantChunksParallelBlockingAround(IMarchingCubeChunk chunk)
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
            List<Exception> x = MarchingCubeChunkThreaded.xs;
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
                        //TODO: while waiting create mesh displayers! -> led to worse performance?
                        while (readyParallelChunks.Count > 0)
                        {
                            OnParallelChunkDoneCallBack(readyParallelChunks.Dequeue());
                        }
                    }
                }
            }
        }


        public IEnumerator BuildRelevantChunksParallelAround(IMarchingCubeChunk chunk)
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

            Debug.Log($"Number of chunks: {ChunkGroups.Count}");
        }

        private IEnumerator BuildRelevantChunksParallelAround()
        {
            List<Exception> x = MarchingCubeChunkThreaded.xs;
            Vector3Int next;
            bool isNextInProgress = false;
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

        protected void OnParallelChunkDoneCallBack(IThreadedMarchingCubeChunk chunk)
        {
            channeledChunks--;

            if (chunk == null)
            {
                Debug.Log("Chunk is null?");
                return;
            }

            chunk.IsInOtherThread = false;
            if(chunk.IsEmpty)
            {
                chunk.ResetChunk();
            }
            else
            {
                chunk.BuildAllMeshes();
                chunk.IsReady = true;

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

        protected int channeledChunks = 0;

        protected bool hasFoundInitialChunk;


        protected IMarchingCubeChunk FindNonEmptyChunkAround(Vector3 pos)
        {
            bool isEmpty = true;
            Vector3Int chunkIndex;
            IMarchingCubeChunk chunk = null;
            while (isEmpty)
            {
                chunkIndex = PositionToChunkGroupCoord(pos);
                chunk = CreateChunkAt(pos, chunkIndex);
                isEmpty = chunk.IsEmpty;
                if (chunk.IsEmpty)
                {
                    //TODO: maybe just read noise points here and completly remove isSolid or Air
                    if (chunk.IsCompletlySolid)
                    {
                        pos.y += chunk.ChunkSize;
                    }
                    else
                    {
                        pos.y -= chunk.ChunkSize;
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

            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(Vector3Int.FloorToInt(pos), coord, lodPower, chunkSizePower, false);
            BuildChunkParallel(chunk, careForNeighbours);
        }

        public bool TryGetOrCreateChunkAt(Vector3Int p, out IMarchingCubeChunk chunk)
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

        protected IMarchingCubeChunk CreateChunkAt(Vector3Int p, bool allowOverride = false)
        {
            return CreateChunkAt(p, PositionToChunkGroupCoord(p), allowOverride);
        }

        //TODO:Check if collider can be removed from most chunks.
        //Collision can be approximated by calling noise function for lowest point of object and checking if its noise is larger than surface value

        protected IMarchingCubeChunk CreateChunkAt(Vector3 pos, Vector3Int coord, bool allowOverride = false)
        {
            int lodPower;
            int chunkSizePower;
            bool careForNeighbours;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSizePower, out lodPower, out careForNeighbours);
            return CreateChunkWithProperties(VectorExtension.ToVector3Int(pos), coord, lodPower, chunkSizePower, careForNeighbours, allowOverride);
        }

        protected IMarchingCubeChunk CreateChunkWithProperties(Vector3Int pos, Vector3Int coord, int lodPower, int chunkSizePower, bool careForNeighbours, bool allowOverride)
        {
            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(pos, coord, lodPower, chunkSizePower, allowOverride);
            BuildChunk(chunk, careForNeighbours);
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

        public bool TryGetMipMapAt(Vector3Int pos, int sizePower, out float[] storedNoise, out bool isMipMapComplete)
        {
            StorageTreeRoot chunkGroup;
            if (storageGroups.TryGetValue(PositionToStorageGroupCoord(pos), out chunkGroup))
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
            StorageTreeRoot chunkGroup;
            if (storageGroups.TryGetValue(coord, out chunkGroup))
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
            IMarchingCubeChunk _;
            return TryGetChunkAtPosition(v3, out _);
        }

        public bool TryGetChunkAtPosition(Vector3Int p, out IMarchingCubeChunk chunk)
        {
            Vector3Int _;
            return TryGetChunkAtPosition(p, out chunk, out _);
        }

        public bool TryGetChunkAtPosition(Vector3Int p, out IMarchingCubeChunk chunk, out Vector3Int positionInOtherChunk)
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

        public void RemoveChunk(IMarchingCubeChunk chunk)
        {
            Vector3Int coord = PositionToChunkGroupCoord(chunk.AnchorPos);
            IChunkGroupRoot chunkGroup;
            if (chunkGroups.TryGetValue(coord, out chunkGroup))
            {
                chunkGroup.RemoveChunkAtGlobalPosition(chunk.AnchorPos);
            }
        }

        public bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk)
        {
            return TryGetChunkAtPosition(p, out chunk) && chunk.IsReady;
        }

        public bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk, out Vector3Int relativePositionInChunk)
        {
            return TryGetChunkAtPosition(p, out chunk, out relativePositionInChunk) && chunk.IsReady;
        }

        /// <summary>
        /// gets or creates a chunk at position. Fails if at position a chunk is being created
        /// </summary>
        /// <param name="p"></param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public bool TryGetOrCreateChunk(Vector3Int p, out IMarchingCubeChunk chunk)
        {
            if (TryGetChunkAtPosition(p, out chunk))
            {
                if (chunk.IsReady)
                {
                    return true;
                }
                else
                {
                    chunk = null;
                    return false;
                }
            }
            else
            {
                chunk = CreateChunkAt(p);
                return true;
            }
        }

        public bool HasChunkStartedAt(Vector3Int p)
        {
            IMarchingCubeChunk chunk;
            if (TryGetChunkAtPosition(p, out chunk))
            {
                return chunk.HasStarted;
            }
            return false;
        }

        public WorldUpdater worldUpdater;

        public Transform colliderParent;


        protected IMarchingCubeChunk GetChunkObjectAt<T>(Vector3Int position, Vector3Int coord, int lodPower, int chunkSizePower, bool allowOverride) where T : IMarchingCubeChunk, new()
        {
            ///Pot racecondition
            IChunkGroupRoot chunkGroup = GetOrCreateChunkGroupAtCoordinate(coord);
            IMarchingCubeChunk chunk = new T();

            chunk.ChunkHandler = this;
            chunk.ChunkSizePower = chunkSizePower;
            chunk.ChunkUpdater = worldUpdater;
            chunk.Material = chunkMaterial;
            chunk.SurfaceLevel = surfaceLevel;
            chunk.LODPower = lodPower;

            chunkGroup.SetLeafAtPosition(new int[] { position.x, position.y, position.z }, chunk, allowOverride);

            worldUpdater.AddChunk(chunk);

            return chunk;
        }

        protected void BuildEmptyChunkAt(Vector3Int pos)
        {
            IChunkGroupRoot chunkGroup = GetOrCreateChunkGroupAtCoordinate(PositionToChunkGroupCoord(pos));
            IMarchingCubeChunk chunk;

            if (!chunkGroup.TryGetLeafAtGlobalPosition(pos, out chunk))
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

                SetChunkColliderOf(chunk);

                worldUpdater.AddChunk(chunk);
            }
        }

        //Do this as work after compute shader dispatch and before data read
        //also give put object back into stack for empty chunks and read from stack
        protected void BuildAndSetLodColliderForChunk(IMarchingCubeChunk c)
        {
            GameObject g = new GameObject();
            SphereCollider sphere = g.AddComponent<SphereCollider>();
            sphere.radius = 16;

            sphere.isTrigger = true;

            g.transform.position = c.CenterPos;
            ChunkLodCollider coll = g.AddComponent<ChunkLodCollider>();
            coll.coll = sphere;
            coll.chunk = c;
            c.ChunkSimpleCollider = coll;

            g.layer = 6;
            g.transform.SetParent(colliderParent, true);
        }

        protected IMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int pos, int lodPower, int chunkSizePower, bool allowOverride)
        {
            return GetThreadedChunkObjectAt(pos, PositionToChunkGroupCoord(pos), lodPower, chunkSizePower, allowOverride);
        }


        protected IMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int position, Vector3Int coord, int lodPower, int chunkSizePower, bool allowOverride)
        {
            if (lodPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
                return GetChunkObjectAt<MarchingCubeChunkThreaded>(position, coord, lodPower, chunkSizePower, allowOverride);
            else
                return GetChunkObjectAt<CompressedMarchingCubeChunkThreaded>(position, coord, lodPower, chunkSizePower, allowOverride);
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


        public int totalTriBuild;

        TriangleBuilder[] tris;// = new TriangleBuilder[CHUNK_VOLUME * 5];
        float[] pointsArray;

        private ComputeBuffer triangleBuffer;
        private ComputeBuffer pointsBuffer;
        private ComputeBuffer savedPointBuffer;
        private ComputeBuffer triCountBuffer;


        //TODO: Check to iterate loops in parallel(https://michaelscodingspot.com/array-iteration-vs-parallelism-in-c-net/)

        protected void SplitArray(int halfSize, float[] splitThis,
           float[] frontBotLeft, float[] frontBotRight, float[] frontTopLeft, float[] frontTopRight,
           float[] backBotLeft, float[] backBotRight, float[] backTopLeft, float[] backTopRight)
        {
            //ThreadPool.GetAvailableThreads(out availableThreads, out availableSyncThreads);
            //if (availableThreads >= 8)
            //{
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 0, halfSize, 0, 0, 0, splitThis, frontBotLeft));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 1, halfSize, halfSize, 0, 0, splitThis, frontBotRight));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 2, halfSize, 0, halfSize, 0, splitThis, frontTopLeft));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 3, halfSize, halfSize, halfSize, 0, splitThis, frontTopRight));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 4, halfSize, 0, 0, halfSize, splitThis, backBotLeft));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 5, halfSize, halfSize, 0, halfSize, splitThis, backBotRight));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done, 6, halfSize, 0, halfSize, halfSize, splitThis, backTopLeft));
            //    ThreadPool.QueueUserWorkItem((o) => SplitArrayAtParallel(done,7,halfSize, halfSize, halfSize, halfSize, splitThis, backTopRight));
            //}
            //while (done.Contains(false))
            //{

            //}
            SplitArrayAt(halfSize, 0, 0, 0, splitThis, frontBotLeft);
            SplitArrayAt(halfSize, halfSize, 0, 0, splitThis, frontBotRight);
            SplitArrayAt(halfSize, 0, halfSize, 0, splitThis, frontTopLeft);
            SplitArrayAt(halfSize, halfSize, halfSize, 0, splitThis, frontTopRight);
            SplitArrayAt(halfSize, 0, 0, halfSize, splitThis, backBotLeft);
            SplitArrayAt(halfSize, halfSize, 0, halfSize, splitThis, backBotRight);
            SplitArrayAt(halfSize, 0, halfSize, halfSize, splitThis, backTopLeft);
            SplitArrayAt(halfSize, halfSize, halfSize, halfSize, splitThis, backTopRight);

        }



        public float[][] GetSplittedNoiseArray(IMarchingCubeChunk chunk)
        {
            float[] points = chunk.Points;
            int halfSize = chunk.ChunkSize / 2;
            int halfPlus = halfSize + 1;
            int size = halfPlus * halfPlus * halfPlus;
            float[] frontBotLeft = new float[size];
            float[] frontBotRight = new float[size];
            float[] frontTopLeft = new float[size];
            float[] frontTopRight = new float[size];
            float[] backBotLeft = new float[size];
            float[] backBotRight = new float[size];
            float[] backTopLeft = new float[size];
            float[] backTopRight = new float[size];
            SplitArray(halfSize, points, frontBotLeft, frontBotRight, frontTopLeft, frontTopRight, backBotLeft, backBotRight, backTopLeft, backTopRight);

            return new float[][] { frontBotLeft, backTopLeft, frontBotRight, backBotRight, frontTopLeft, backTopLeft, frontTopRight, backTopRight };
        }


        public MarchingCubeChunkNeighbourLODs GetNeighbourLODSFrom(IMarchingCubeChunk chunk)
        {
            MarchingCubeChunkNeighbourLODs result = new MarchingCubeChunkNeighbourLODs();
            Vector3Int[] coords = VectorExtension.GetAllAdjacentDirections;
            for (int i = 0; i < coords.Length; ++i)
            {
                MarchingCubeNeighbour neighbour = new MarchingCubeNeighbour();
                Vector3Int neighbourPos = chunk.CenterPos + chunk.ChunkSize * coords[i];
                if (!TryGetChunkAtPosition(neighbourPos, out neighbour.chunk))
                {
                    //change name to extectedLodPower
                    neighbour.estimatedLodPower = GetLodPowerAt(neighbourPos);
                }
                result[i] = neighbour;
            }

            return result;
        }

        //TODO:Remove keep points
        protected void BuildChunk(IMarchingCubeChunk chunk, bool careForNeighbours)
        {
            TriangleChunkHeap ts = DispatchAndGetShaderData(chunk, careForNeighbours);
            chunk.InitializeWithMeshData(ts);
        }

        protected Queue<IThreadedMarchingCubeChunk> readyParallelChunks = new Queue<IThreadedMarchingCubeChunk>();

        protected void BuildChunkParallel(IMarchingCubeChunk chunk, bool careForNeighbours)
        {
            TriangleChunkHeap ts = DispatchAndGetShaderData(chunk, careForNeighbours);
            channeledChunks++;
            chunk.InitializeWithMeshDataParallel(ts, readyParallelChunks);
        }

        public int minSteepness = 15;
        public int maxSteepness = 50;
        public Color flatColor = new Color(0, 255 / 255f, 0, 1);
        public Color steepColor = new Color(75 / 255f, 44 / 255f, 13 / 255f, 1);

        private int steepR;
        private int steepG;
        private int steepB;

        private int flatR;
        private int flatG;
        private int flatB;

        public float[] RequestNoiseForChunk(IMarchingCubeChunk chunk)
        {
            return RequestNoiseFor(chunk.ChunkSizePower, chunk.PointsPerAxis, chunk.LOD, chunk.AnchorPos);
        }

        public float[] RequestNoiseFor(int sizePow, int pointsPerAxis, int LOD, Vector3Int anchor)
        {
            float[] result;
            GenerateNoise(sizePow, pointsPerAxis, LOD, anchor);
            result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            pointsBuffer.GetData(result, 0, 0, result.Length);
            return result;
        }

        public float[] GetNoiseRawFor(int pointsPerAxis, int LOD, Vector3Int anchor)
        {
            densityGenerator.Generate(pointsPerAxis, anchor, LOD);
            float[] result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            pointsBuffer.GetData(result, 0, 0, result.Length);
            return result;
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
                hasStoredData = TryGetMipMapAt(anchor, sizePow, out storedNoiseData, out isMipMapComplete);
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

        protected TriangleChunkHeap[] DispatchMultipleChunks(IMarchingCubeChunk[] chunks)
        {
            triangleBuffer.SetCounterValue(0);
            int chunkLength = chunks.Length;
            for (int i = 0; i < chunkLength; i++)
            {
                IMarchingCubeChunk c = chunks[i];
                GenerateNoise(c.ChunkSizePower, c.PointsPerAxis, c.LOD, c.AnchorPos);
                AccumulateCubesFromNoise(c, i);
            }
            int[] triCounts = new int[chunkLength];

            for (int i = 0; i < chunkLength; i++)
            {
                SetChunkColliderOf(chunks[i]);
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
                    chunks[i].FreeSimpleCollider();
                }
            }
            return result;
        }


        //TODO: Inform about Mesh subset and mesh set vertex buffer
        //Subset may be used to only change parts of the mesh -> dont need multiple mesh displayers with submeshes?
        protected TriangleChunkHeap DispatchAndGetShaderData(IMarchingCubeChunk chunk, bool careForNeighbours)
        {
            int lod = chunk.LOD;
            int chunkSize = chunk.ChunkSize;

            if (chunkSize % lod != 0)
                throw new Exception("Lod must be divisor of chunksize");

            int numVoxelsPerAxis = chunkSize / lod;
            int pointsPerAxis = numVoxelsPerAxis + 1;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;

            GenerateNoise(chunk.ChunkSizePower, pointsPerAxis, lod, chunk.AnchorPos);

            ComputeCubesFromNoise(chunk.ChunkSize, chunk.AnchorPos, lod);

            ///Do work for chunk here, before data from gpu is read, to give gpu time to finish

            SetChunkColliderOf(chunk);

            ///read data from gpu

            int numTris = ReadCurrentTriangleData();

            if (numTris == 0)
            {
                chunk.FreeSimpleCollider();
            }

            if ((numTris == 0 && !hasFoundInitialChunk) || careForNeighbours)
            {
                if (careForNeighbours)
                {
                    pointsArray = new float[pointsVolume];
                }
                else
                {
                    pointsArray = new float[1];
                }
                pointsBuffer.GetData(pointsArray, 0, 0, pointsArray.Length);
                chunk.Points = pointsArray;
            }
            return new TriangleChunkHeap(tris, 0, numTris);
        }

        public TriangleBuilder[] GenerateCubesFromNoise(IMarchingCubeChunk chunk, int triCount, float[] noise)
        {
            pointsBuffer.SetData(noise);
            RequestCubesFromNoise(chunk, chunk.LOD, triCount);
            return tris;
        }

        public void AccumulateCubesFromNoise(IMarchingCubeChunk chunk, int offest)
        {
            ComputeCubesFromNoise(chunk.ChunkSize, chunk.AnchorPos, chunk.LOD, false);
            ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, offest * 4);
        }

        public int RequestCubesFromNoise(IMarchingCubeChunk chunk, int lod, int triCount = -1)
        {
            ComputeCubesFromNoise(chunk.ChunkSize, chunk.AnchorPos, lod);
            return ReadCurrentTriangleData(triCount);
        }

        protected int ReadCurrentTriangleData(int triCount = -1)
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

            tris = new TriangleBuilder[triCount];
            //TODO: Check if this changes performance
            if (triCount > 0)
            {
                triangleBuffer.GetData(tris, 0, 0, triCount);
            }

            totalTriBuild += triCount;
            return triCount;
        }

        public void ComputeCubesFromNoise(int chunkSize, Vector3Int anchor, int lod, bool resetCounter = true)
        {
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

        public int[] GetColor(PathTriangle t, int steepness)
        {
            float invLerp = (steepness - minSteepness) / ((float)maxSteepness - minSteepness);
            if (invLerp < 0)
                invLerp = 0;
            else if (invLerp > 1)
                invLerp = 1;

            return new int[] {
                (int)(invLerp * steepR + (1 - invLerp) * flatR),
                (int)(invLerp * steepG + (1 - invLerp) * flatG),
                (int)(invLerp * steepB  + (1 - invLerp) * flatB)};
        }

        public int GetFeasibleReducedLodForChunk(IMarchingCubeChunk c, int toLodPower)
        {
            return Mathf.Min(toLodPower, c.LODPower + 1);
        }

        public int GetFeasibleIncreaseLodForChunk(IMarchingCubeChunk c, int toLodPower)
        {
            return Mathf.Max(toLodPower, c.LODPower - 1);
        }

        public void IncreaseChunkLod(IMarchingCubeChunk chunk, int toLodPower)
        {
            toLodPower = GetFeasibleIncreaseLodForChunk(chunk, toLodPower);
            int oldLodPow = chunk.LODPower;
            int toLod = RoundToPowerOf2(toLodPower);
            if (toLod >= chunk.LOD || chunk.ChunkSize % toLod != 0)
                Debug.LogWarning($"invalid new chunk lod {toLodPower} from lod {chunk.LODPower}");

            int newSizePow = DEFAULT_CHUNK_SIZE_POWER + toLodPower;
            if (newSizePow == chunk.ChunkSizePower || newSizePow == CHUNK_GROUP_SIZE_POWER)
            {
                Debug.Log("Simple decrease");
                    //if previous chunk was border chunk, build spawners at neighbours
                IMarchingCubeChunk current = ExchangeSingleChunkParallel(chunk, chunk.AnchorPos, toLodPower, chunk.ChunkSizePower, false, true);
            }
            else
            {
                Debug.Log("split");
                SplitChunkAndIncreaseLod(chunk, toLodPower, newSizePow);
            }
        }

        public void SpawnEmptyChunksAround(IMarchingCubeChunk c)
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

        protected IMarchingCubeChunk ExchangeSingleChunkParallel(IMarchingCubeChunk from, Vector3Int anchorPos, int lodPow, int sizePow, bool careForNeighbours, bool allowOveride)
        {
            from.PrepareDestruction();
            return ExchangeChunkParallel(anchorPos, lodPow, sizePow, careForNeighbours, allowOveride, (c) => { FinishParallelChunk(from, c); });
        }

        public static object exchangeLocker = new object();

        protected void FinishParallelChunk(IMarchingCubeChunk from, IThreadedMarchingCubeChunk newChunk)
        {
            lock (exchangeLocker)
            {
                worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(from, newChunk));
            }
        }


        protected IMarchingCubeChunk ExchangeChunkParallel(Vector3Int anchorPos, int lodPow, int sizePow, bool careForNeighbours, bool allowOveride, Action<IThreadedMarchingCubeChunk> onChunkDone)
        {
            IMarchingCubeChunk newChunk = GetThreadedChunkObjectAt(anchorPos, lodPow, sizePow, allowOveride);
            newChunk.InitializeWithMeshDataParallel(DispatchAndGetShaderData(newChunk, careForNeighbours), onChunkDone);
            return newChunk;
        }

        private void SplitChunkAndIncreaseLod(IMarchingCubeChunk chunk, int toLodPower, int newSizePow)
        {
            int[][] anchors = chunk.GetLeaf().GetAllChildGlobalAnchorPosition();
            IMarchingCubeChunk[] newChunks = new IMarchingCubeChunk[8];
            for (int i = 0; i < 8; i++)
            {
                Vector3Int v3 = IntVecToVector3(anchors[i]);
                newChunks[i] = GetThreadedChunkObjectAt(v3, PositionToChunkGroupCoord(v3), toLodPower, newSizePow, true);
            }
            chunk.PrepareDestruction();
            TriangleChunkHeap[] tris = DispatchMultipleChunks(newChunks);
            object listLock = new object();
            List<IThreadedMarchingCubeChunk> chunks = new List<IThreadedMarchingCubeChunk>();
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


        protected Vector3Int IntVecToVector3(int[] arr) => new Vector3Int(arr[0], arr[1], arr[2]);

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

        public void DecreaseChunkLod(IMarchingCubeChunk chunk, int toLodPower)
        {
            if (toLodPower > MAX_CHUNK_LOD_POWER)
            {
                chunk.ResetChunk(false);
            }
            else
            {
                toLodPower = GetFeasibleReducedLodForChunk(chunk, toLodPower);
                int toLod = RoundToPowerOf2(toLodPower);
                if (toLod <= chunk.LOD || chunk.ChunkSize % toLod != 0)
                    Debug.LogWarning($"invalid new chunk lod {toLodPower} from lod {chunk.LODPower}");

                if (chunk.GetLeaf().AllSiblingsAreLeafsWithSameTargetLod())
                {
                    MergeAndReduceChunkBranch(chunk, toLodPower, toLod);
                }
                else
                {
                    DecreaseSingleChunkLod(chunk, toLodPower, toLod);
                }
            }
        }

        public void DecreaseSingleChunkLod(IMarchingCubeChunk chunk, int toLodPower)
        {
            toLodPower = GetFeasibleReducedLodForChunk(chunk, toLodPower);
            int toLod = RoundToPowerOf2(toLodPower);
            if (toLod <= chunk.LOD || chunk.ChunkSize % toLod != 0)
                Debug.LogWarning($"invalid new chunk lod {toLodPower} from lod {chunk.LODPower}");

            DecreaseSingleChunkLod(chunk, toLodPower, toLod);
        }

        protected void DecreaseSingleChunkLod(IMarchingCubeChunk chunk, int toLodPower, int toLod)
        {
            ExchangeSingleChunkParallel(chunk, chunk.CenterPos, toLodPower, chunk.ChunkSizePower, false, true);
        }

        public float[] GetNoiseForMergingChunkAt(IMarchingCubeChunk chunk, int toLod)
        {
            int[] pos = chunk.GetLeaf().parent.GroupAnchorPosition;
            return RequestNoiseFor(chunk.ChunkSizePower, chunk.PointsPerAxis, toLod, new Vector3Int(pos[0], pos[1], pos[2]));
        }

        public void MergeAndReduceChunkBranch(IMarchingCubeChunk chunk, int toLodPower, int toLod)
        {
            ChunkGroupTreeLeaf[] leafs = chunk.GetLeaf().parent.GetLeafs();
            for (int i = 0; i < 8; i++)
            {
                ChunkGroupTreeLeaf l = leafs[i];
                if (l == null)
                    continue;
                l.leaf.PrepareDestruction();
            }
            ExchangeChunkParallel(chunk.CenterPos, toLodPower, chunk.ChunkSizePower + 1, false, true, (c) =>
            {
                List<IMarchingCubeChunk> oldChunks = new List<IMarchingCubeChunk>();
                for (int i = 0; i < 8; i++)
                {
                    ChunkGroupTreeLeaf l = leafs[i];
                    if (l == null)
                        continue;
                    oldChunks.Add(l.leaf);
                }
                lock (exchangeLocker)
                {
                    worldUpdater.readyExchangeChunks.Push(new ReadyChunkExchange(oldChunks, c));
                }
            });
        }

        protected void TransferPointsInto(float[] originalPoints, float[] writeInHere, int originalPointsPerAxis, int originalPointsPerAxisSqr, int shrinkFactor)
        {
            int addCount = 0;

            for (int z = 0; z < originalPointsPerAxis; z += shrinkFactor)
            {
                int zPoint = z * originalPointsPerAxisSqr;
                for (int y = 0; y < originalPointsPerAxis; y += shrinkFactor)
                {
                    int yPoint = y * originalPointsPerAxis;
                    for (int x = 0; x < originalPointsPerAxis; x += shrinkFactor)
                    {
                        writeInHere[addCount] = originalPoints[zPoint + yPoint + x];
                        addCount++;
                    }
                }
            }
        }


        protected void CombinePointsInto(int[] startIndex, float[] originalPoints, float[] writeInHere, int pointsPerAxis, int pointsPerAxisSqr, int shrinkFactor, int toLod)
        {
            int halfSize = pointsPerAxis / 2;
            int halfSizeCeil = halfSize;
            int halfFrontJump = pointsPerAxis * halfSizeCeil;

            int writeIndex = startIndex[0] / toLod + startIndex[1] / toLod * pointsPerAxis + startIndex[2] / toLod * pointsPerAxisSqr;
            int readIndex;

            for (int z = 0; z < pointsPerAxis; z += shrinkFactor)
            {
                int zPoint = z * pointsPerAxisSqr;
                for (int y = 0; y < pointsPerAxis; y += shrinkFactor)
                {
                    int yPoint = y * pointsPerAxis;
                    readIndex = zPoint + yPoint;
                    for (int x = 0; x < pointsPerAxis; x += shrinkFactor)
                    {
                        float val = originalPoints[readIndex + x];
                        writeInHere[writeIndex] = val;
                        writeIndex++;
                    }
                    writeIndex += halfSizeCeil;
                }
                writeIndex += halfFrontJump;
            }
        }

        protected void SplitArrayAt(int halfSize, int startIndexX, int startIndexY, int startIndexZ, float[] points, float[] writeInHere)
        {
            int pointsPerAxis = 2 * halfSize + 1;
            int halfFrontJump = pointsPerAxis * halfSize;
            int readIndex = 0;
            int counter = 0;
            int endX = startIndexX + halfSize;
            int endY = startIndexY + halfSize;
            int endZ = startIndexZ + halfSize;
            for (int z = startIndexZ; z <= endZ; z++)
            {
                for (int y = startIndexY; y <= endY; y++)
                {
                    for (int x = startIndexX; x <= endX; x++)
                    {
                        writeInHere[counter] = points[readIndex];
                        readIndex += 1;
                        counter++;
                    }
                    readIndex += halfSize;
                }
                readIndex += halfFrontJump;
            }
        }


        protected void CreateAllBuffersWithSizes(int numVoxelsPerAxis)
        {
            int points = numVoxelsPerAxis + 1;
            int numPoints = points * points * points;
            int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            int maxTriangleCount = numVoxels * 2;
            maxTriangleCount *= MAX_CHUNKS_PER_ITERATION;

            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 1);
            savedPointBuffer = new ComputeBuffer(numPoints, sizeof(float) * 1);
            triangleBuffer = new ComputeBuffer(maxTriangleCount, TriangleBuilder.SIZE_OF_TRI_BUILD, ComputeBufferType.Append);
            triCountBuffer = new ComputeBuffer(MAX_CHUNKS_PER_ITERATION, sizeof(int), ComputeBufferType.Raw);
        }

        protected const int MAX_CHUNKS_PER_ITERATION = 15;

        protected void ApplyShaderProperties()
        {
            marshShader.SetBuffer(0, "points", pointsBuffer);
            marshShader.SetBuffer(0, "savedPoints", savedPointBuffer);
            marshShader.SetBuffer(0, "triangles", triangleBuffer);

            marshShader.SetInt("minSteepness", minSteepness);
            marshShader.SetInt("maxSteepness", maxSteepness);
            marshShader.SetInts("flatColor", Mathf.RoundToInt(flatColor.r * 255), Mathf.RoundToInt(flatColor.g * 255), Mathf.RoundToInt(flatColor.b * 255));
            marshShader.SetInts("steepColor", Mathf.RoundToInt(steepColor.r * 255), Mathf.RoundToInt(steepColor.g * 255), Mathf.RoundToInt(steepColor.b * 255));
            marshShader.SetFloat("surfaceLevel", surfaceLevel);
        }

        protected void ReleaseBuffers()
        {
            if (triangleBuffer != null)
            {
                triangleBuffer.Release();
                pointsBuffer.Release();
                savedPointBuffer.Release();
                triCountBuffer.Release();
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

        public void Store(Vector3Int anchorPos, float[] noise)
        {
            StoredChunkEdits edits;
            if (!TryGetStoredEditsAt(anchorPos, out edits))
            {
                edits = new StoredChunkEdits();
                StorageTreeRoot r = GetOrCreateStorageGroupAtCoordinate(PositionToStorageGroupCoord(anchorPos));
                r.SetLeafAtPosition(anchorPos, edits, true);
            }
            edits.vals = noise;
        }
    }
}
