﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace MarchingCubes
{

    ///when saving: save for each chunk if it was initialized empty and a dictionary which modified point has which w
    public class MarchingCubeChunkHandler : MonoBehaviour, IMarchingCubeChunkHandler
    {

        protected int kernelId;

        protected const int threadGroupSize = 8;

        public const int MIN_CHUNK_SIZE = 8;

        public const int MIN_CHUNK_SIZE_POWER = 3;

        public const int CHUNK_GROUP_SIZE = 1024;

        public const int CHUNK_GROUP_SIZE_POWER = 10;

        public const int DEFAULT_CHUNK_SIZE = 32;

        public const int DEFAULT_CHUNK_SIZE_POWER = 5;


        public const int DEFAULT_MIN_CHUNK_LOD_POWER = 0;

        public const int MAX_CHUNK_LOD_POWER = 7;

        public const int MAX_CHUNK_LOD_BIT_REPRESENTATION_SIZE = 3;


        public Dictionary<Vector3Int, IChunkGroupRoot> chunkGroups = new Dictionary<Vector3Int, IChunkGroupRoot>();

        public Dictionary<Vector3Int, IChunkGroupRoot> ChunkGroups => chunkGroups;

        [Range(1, 253)]
        public int blockAroundPlayer = 16;

        private const int maxTrianglesLeft = 5000000;

        public ComputeShader marshShader;

        public const int maxLodAtDistance = 2000;

        public const int maxSizeAtDistance = 2000;

        [Header("Voxel Settings")]
        //public float boundsSize = 8;
        public Vector3 noiseOffset = Vector3.zero;

        //[Range(2, 100)]
        //public int numPointsPerAxis = 30;

        public AnimationCurve lodPowerForDistances;

        public AnimationCurve chunkSizePowerForDistances;


        //public Dictionary<Vector3Int, IMarchingCubeChunk> Chunks => chunks;

        //protected HashSet<BaseMeshChild> inUseDisplayer = new HashSet<BaseMeshChild>();

        protected Stack<BaseMeshDisplayer> unusedDisplayer = new Stack<BaseMeshDisplayer>();

        protected Stack<BaseMeshDisplayer> unusedInteractableDisplayer = new Stack<BaseMeshDisplayer>();

        public void StartWaitForParralelChunkDoneCoroutine(IEnumerator e)
        {
            StartCoroutine(e);
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
            return (int)Mathf.Max(DEFAULT_MIN_CHUNK_LOD_POWER, lodPowerForDistances.Evaluate(distance / maxLodAtDistance));
        }

        public int GetLodPowerAt(Vector3 pos)
        {
            return GetLodPower((pos - startPos).magnitude);
        }

        public int GetSizePowerForDistance(float distance)
        {
            return (int)chunkSizePowerForDistances.Evaluate(distance / maxSizeAtDistance);
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

            densityGenerator.SetPointsBuffer(pointsBuffer);
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

            Debug.Log($"Number of chunks: {ChunkGroups.Count}");
        }

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
                        //TODO: while waiting create mesh displayers!
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
                        while(readyParallelChunks.Count > 0)
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

                if(sqrDist > buildAroundSqrDistance)
                {

                }
                ///only add neighbours if
                if (sqrDist <= buildAroundSqrDistance
                    && !HasChunkAtPosition(v3))
                {
                    closestNeighbours.Enqueue(sqrDist, v3);
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

        //TODO:Use late update to see how much time has passes so far, and if not so much use time to change neighbour chunks

        //protected void SetActivationOfChunks(Vector3Int center)
        //{
        //    int deactivatedChunkSqrDistance = DeactivatedChunkDistance;
        //    deactivatedChunkSqrDistance *= deactivatedChunkSqrDistance;
        //    foreach (KeyValuePair<Vector3Int, IMarchingCubeChunk> kv in chunks)
        //    {
        //        int sqrMagnitude = (kv.Key - center).sqrMagnitude;
        //        kv.Value.SetActive(magnitude <= deactivatedChunkSqrDistance);
        //    }
        //}

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
            BuildChunkParallel(chunk, RoundToPowerOf2(lodPower), careForNeighbours);
        }

        public bool TryGetOrCreateChunkAt(Vector3Int p, out IMarchingCubeChunk chunk)
        {
            if(!TryGetChunkAtPosition(p, out chunk))
            {
                chunk = CreateChunkAt(p);
            }
            if(chunk != null && !chunk.IsReady)
            {
                Debug.LogWarning("Unfinished chunk next to requested chunk. may lead to holes in mesh!");
            }
            return chunk != null;
        }

        protected IMarchingCubeChunk CreateChunkAt(Vector3Int p)
        {
            return CreateChunkAt(p, PositionToChunkGroupCoord(p));
        }

        //TODO:Check if collider can be removed from most chunks.
        //Collision can be approximated by calling noise function for lowest point of object and checking if its noise is larger than surface value

        protected IMarchingCubeChunk CreateChunkAt(Vector3 pos, Vector3Int coord)
        {
            int lodPower;
            int chunkSizePower;
            bool careForNeighbours;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSizePower, out lodPower, out careForNeighbours);
            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(VectorExtension.ToVector3Int(pos), coord, lodPower, chunkSizePower, false);
            BuildChunk(chunk, RoundToPowerOf2(lodPower), careForNeighbours);
            return chunk;
        }


        protected IChunkGroupRoot CreateChunkGroupAtCoordinate(Vector3Int coord)
        {
            IChunkGroupRoot chunkGroup = new ChunkGroupRoot(new int[] { coord.x, coord.y, coord.z });
            chunkGroups.Add(coord, chunkGroup);
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
                    if (/*chunkGroup.HasChild && */chunkGroup.TryGetChunkAtGlobalPosition(p, out chunk))
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

        protected IMarchingCubeChunk GetChunkObjectAt<T>(Vector3Int position, Vector3Int coord, int lodPower, int chunkSizePower, bool allowOverride) where T : IMarchingCubeChunk, new()
        {
            ///Pot racecondition
            IChunkGroupRoot chunkGroup = GetOrCreateChunkGroupAtCoordinate(coord);
            IMarchingCubeChunk chunk = new T();

            chunk.ChunkHandler = this;
            chunk.ChunkSizePower = chunkSizePower;
            chunk.Material = chunkMaterial;
            chunk.SurfaceLevel = surfaceLevel;
            chunk.LODPower = lodPower;

            chunkGroup.SetChunkAtPosition(new int[] { position.x, position.y, position.z }, chunk, allowOverride);

            worldUpdater.AddChunk(chunk);

            return chunk;
        }
        

        protected IMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int pos, int lodPower, int chunkSize, bool allowOverride)
        {
            return GetThreadedChunkObjectAt(pos, PositionToChunkGroupCoord(pos), lodPower, chunkSize, allowOverride);
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


        public int totalTriBuild;

        TriangleBuilder[] tris;// = new TriangleBuilder[CHUNK_VOLUME * 5];
        float[] pointsArray;

        private ComputeBuffer triangleBuffer;
        private ComputeBuffer pointsBuffer;
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


        public void SplitChunkAndRecalculateAll(IMarchingCubeChunk chunk)
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

        protected void BuildChunk(IMarchingCubeChunk chunk, int lod, bool careForNeighbours)
        {
            ApplyChunkDataAndDispatchAndGetShaderData(chunk, lod, careForNeighbours);
            chunk.InitializeWithMeshData(tris, false);
        }

        protected Queue<IThreadedMarchingCubeChunk> readyParallelChunks = new Queue<IThreadedMarchingCubeChunk>();

        protected void BuildChunkParallel(IMarchingCubeChunk chunk, int lod, bool careForNeighbours)
        {
            ApplyChunkDataAndDispatchAndGetShaderData(chunk, lod, careForNeighbours);
            channeledChunks++;
            chunk.InitializeWithMeshDataParallel(tris, readyParallelChunks, false);
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
            int pointsPerAxis = chunk.PointsPerAxis;

            densityGenerator.Generate(pointsPerAxis, chunk.AnchorPos, chunk.LOD);
            float[] result = new float[pointsPerAxis * pointsPerAxis * pointsPerAxis];
            pointsBuffer.GetData(result, 0, 0, result.Length);

            return result;
        }


        protected void ApplyChunkDataAndDispatchAndGetShaderData(IMarchingCubeChunk chunk, int lod, bool careForNeighbours)
        {
            int chunkSize = chunk.ChunkSize;

            if (chunkSize % lod != 0)
                throw new Exception("Lod must be divisor of chunksize");

            int numVoxelsPerAxis = chunkSize / lod;
            int pointsPerAxis = numVoxelsPerAxis + 1;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;

            float spacing = lod;
            Vector3 anchor = chunk.AnchorPos;

            densityGenerator.Generate(pointsPerAxis, anchor, spacing);

            int numTris = RequestCubesFromNoise(chunk, lod);
                
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
        }

        public TriangleBuilder[] GenerateCubesFromNoise(IMarchingCubeChunk chunk, int triCount, float[] noise)
        {
            pointsBuffer.SetData(noise);
            RequestCubesFromNoise(chunk, chunk.LOD, triCount);
            return tris;
        }

        public void GenerateCubesFromNoise(IMarchingCubeChunk chunk, float[] noise, int lod)
        {
            pointsBuffer.SetData(noise);
            RequestCubesFromNoise(chunk, lod);
            chunk.Points = noise;
            chunk.InitializeWithMeshData(tris, true);
        }

        public int RequestCubesFromNoise(IMarchingCubeChunk chunk, int lod, int triCount = -1)
        {
            ComputeCubesFromNoise(chunk.ChunkSize, chunk.AnchorPos, lod);
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

        public void ComputeCubesFromNoise(int chunkSize, Vector3Int anchor, int lod)
        {
            int numVoxelsPerAxis = chunkSize / lod;
            int pointsPerAxis = numVoxelsPerAxis + 1;

            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

            float spacing = lod;

            triangleBuffer.SetCounterValue(0);
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


        public void DecreaseChunkLod(IMarchingCubeChunk chunk, int toLodPower)
        {
            int toLod = RoundToPowerOf2(toLodPower);
            int chunkSize = chunk.ChunkSize;
            if (toLod <= chunk.LOD || chunkSize % toLod != 0)
                throw new Exception("invalid new chunk lod");

            int shrinkFactor = toLod / chunk.LOD;
            int originalPointsPerAxis = chunk.PointsPerAxis;
            float[] points = chunk.Points;

            chunk.LODPower = toLodPower;

            int newPointsPerAxis = chunk.PointsPerAxis;

            int originalPointsPerAxisSqr = originalPointsPerAxis * originalPointsPerAxis;

            //int newPointsPerAxis = (originalPointsPerAxis - 1) / shrinkFactor + 1;


            float[] relevantPoints = new float[newPointsPerAxis * newPointsPerAxis * newPointsPerAxis];

            int addCount = 0;

            //NotifyNeighbourChunksOnLodSwitch(chunk.ChunkAnchorPosition, toLodPower);

            for (int z = 0; z < originalPointsPerAxis; z += shrinkFactor)
            {
                int zPoint = z * originalPointsPerAxisSqr;
                for (int y = 0; y < originalPointsPerAxis; y += shrinkFactor)
                {
                    int yPoint = y * originalPointsPerAxis;
                    for (int x = 0; x < originalPointsPerAxis; x += shrinkFactor)
                    {
                        float p = points[zPoint + yPoint + x];
                        relevantPoints[addCount] = p;
                        addCount++;
                    }
                }
            }

            pointsBuffer.SetData(relevantPoints);

            RequestCubesFromNoise(chunk, toLod);

            //TryGetChunkAtPosition(chunk.CenterPos);
            IMarchingCubeChunk compressedChunk = GetThreadedChunkObjectAt(chunk.AnchorPos, toLodPower, chunkSize, true);
            //compressedChunk.InitializeWithMeshDataParallel(tris, relevantPoints, chunkSize, this, GetNeighbourLODSFrom(chunk), surfaceLevel,
            //    delegate
            //    {
            //        chunk.ResetChunk();
            //    });
            compressedChunk.InitializeWithMeshData(tris, true);
            chunk.ResetChunk();
        }

      
        protected void CreateAllBuffersWithSizes(int numVoxelsPerAxis)
        {
            int points = numVoxelsPerAxis + 1;
            int numPoints = points * points * points;
            int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            int maxTriangleCount = numVoxels * 2;

            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 1);
            triangleBuffer = new ComputeBuffer(maxTriangleCount, TriangleBuilder.SIZE_OF_TRI_BUILD, ComputeBufferType.Append);
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        }


        protected void ApplyShaderProperties()
        {
            marshShader.SetBuffer(0, "points", pointsBuffer);
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
                triCountBuffer.Release();
                triangleBuffer = null;
            }

        }


        void OnDestroy()
        {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }
        }

    }
}