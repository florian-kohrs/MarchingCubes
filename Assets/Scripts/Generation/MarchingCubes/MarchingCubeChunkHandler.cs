using System;
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

        public const int ChunkSize = 128;

        public const int SuperChunkSize = 512;

        public const int CHUNK_VOLUME = ChunkSize * ChunkSize * ChunkSize;

        public const int DEFAULT_MIN_CHUNK_LOD_POWER = 0;

        // protected int maxRunningThreads = 0;

        public const int PointsPerChunkAxis = ChunkSize + 1;

        public Dictionary<Vector3Int, IMarchingCubeChunk> chunks = new Dictionary<Vector3Int, IMarchingCubeChunk>(new Vector3EqualityComparer());

        public Dictionary<Vector3Int, MarchingCubeChunkGroup> superChunks = new Dictionary<Vector3Int, MarchingCubeChunkGroup>(new Vector3EqualityComparer());

        [Range(1, 253)]
        public int blockAroundPlayer = 16;

        private const int maxTrianglesLeft = 3000000;

        public ComputeShader marshShader;

        public const int maxLodAtDistance = 1000;

        [Header("Voxel Settings")]
        //public float boundsSize = 8;
        public Vector3 noiseOffset = Vector3.zero;

        //[Range(2, 100)]
        //public int numPointsPerAxis = 30;

        public AnimationCurve lodForDistances;

        public Dictionary<Vector3Int, IMarchingCubeChunk> Chunks => chunks;

        //protected HashSet<BaseMeshChild> inUseDisplayer = new HashSet<BaseMeshChild>();

        protected Stack<BaseMeshDisplayer> unusedDisplayer = new Stack<BaseMeshDisplayer>();

        protected Stack<BaseMeshDisplayer> unusedInteractableDisplayer = new Stack<BaseMeshDisplayer>();

        public void StartWaitForParralelChunkDoneCoroutine(IEnumerator e)
        {
            StartCoroutine(e);
        }


        public BaseMeshDisplayer GetNextMeshDisplayer()
        {
            BaseMeshDisplayer displayer = null;
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
            for (int i = 0; i < displayers.Count; i++)
            {
                FreeMeshDisplayer(displayers[i]);
            }
        }

        public int GetLod(float distance)
        {
            return RoundToPowerOf2(Mathf.Max(DEFAULT_MIN_CHUNK_LOD_POWER, lodForDistances.Evaluate(distance / maxLodAtDistance)));
        }

        public int GetLodPower(float distance)
        {
            return (int)Mathf.Max(DEFAULT_MIN_CHUNK_LOD_POWER, lodForDistances.Evaluate(distance / maxLodAtDistance));
        }

        public int GetLodPowerAt(Vector3Int v3)
        {
            return GetLodPower((startPos - AnchorFromChunkCoords(v3)).magnitude);
        }


        protected int RoundToPowerOf2(float f)
        {
            int r = (int)Mathf.Pow(2, Mathf.RoundToInt(f));

            return Mathf.Max(1, r);
        }

        public int GetLodAt(Vector3Int v3)
        {
            return GetLod((startPos - AnchorFromChunkCoords(v3)).magnitude);
        }

        protected int NeededChunkAmount
        {
            get
            {
                int amount = Mathf.CeilToInt(blockAroundPlayer / PointsPerChunkAxis);
                if (amount % 2 == 1)
                {
                    amount += 1;
                }
                return amount;
            }
        }

        //public PlanetMarchingCubeNoise noiseFilter;

        //public TerrainNoise terrainNoise;

        public BaseDensityGenerator densityGenerator;

        public bool useTerrainNoise;


        public int deactivateAfterDistance = 40;

        protected int DeactivatedChunkDistance => Mathf.CeilToInt(deactivateAfterDistance / PointsPerChunkAxis);

        public Material chunkMaterial;

        [Range(0, 1)]
        public float surfaceLevel = 0.45f;

        public Transform player;

        public int buildAroundDistance = 2;

        DateTime start;
        DateTime end;

        private void Start()
        {
            start = DateTime.Now;
            //Debug.Log("Max threadpool threads:" + ThreadPool.thread());
            kernelId = marshShader.FindKernel("March");
            startPos = player.position;
            IMarchingCubeChunk chunk = FindNonEmptyChunkAround(player.position);
            maxSqrChunkDistance = buildAroundDistance * buildAroundDistance;
            StartCoroutine(BuildRelevantChunksParallelAround(chunk));
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


        public void BuildRelevantChunksAround(IMarchingCubeChunk chunk)
        {
            if (chunk.NeighbourCount <= 0)
                return;

            Vector3Int v3;
            Vector3Int newV3;
            do
            {
                var outerEnum = chunk.NeighbourIndices.GetEnumerator();
                while (outerEnum.MoveNext())
                {
                    v3 = outerEnum.Current;
                    if (!Chunks.ContainsKey(v3) && (startPos - AnchorFromChunkCoords(v3)).sqrMagnitude < maxSqrChunkDistance)
                    {
                        IMarchingCubeChunk newChunk = CreateChunkAt(v3);
                        var innerEnum = newChunk.NeighbourIndices.GetEnumerator();
                        while (innerEnum.MoveNext())
                        {
                            newV3 = innerEnum.Current;
                            if (!Chunks.ContainsKey(newV3))
                            {
                                neighbours.Enqueue(newV3);
                            }
                        }
                    }
                }
                chunk = null;
                if (neighbours.Count > 0)
                {
                    Vector3Int next;
                    bool hasNext = false;
                    do
                    {
                        next = neighbours.Dequeue();
                        hasNext = !Chunks.ContainsKey(next);
                    } while (!hasNext && neighbours.Count > 0);

                    if (hasNext)
                    {
                        chunk = CreateChunkAt(next);
                    }
                }
            } while (chunk != null && totalTriBuild < maxTrianglesLeft);
            end = DateTime.Now;
            Debug.Log("Total millis: " + (end - start).TotalMilliseconds);
            if (totalTriBuild >= maxTrianglesLeft)
            {
                Debug.Log("Aborted");
            }
            Debug.Log("Total triangles: " + totalTriBuild);

            Debug.Log($"Number of chunks: {Chunks.Count}");
        }

        protected Vector3 startPos;
        protected float maxSqrChunkDistance;
        protected Queue<Vector3Int> neighbours = new Queue<Vector3Int>();

        protected BinaryHeap<float, Vector3Int> closestNeighbours = new BinaryHeap<float, Vector3Int>(float.MinValue, float.MaxValue, 200);

        public IEnumerator BuildRelevantChunksParallelAround(IMarchingCubeChunk chunk)
        {
            var e = chunk.NeighbourIndices.GetEnumerator();
            while (e.MoveNext())
            {
                closestNeighbours.Enqueue(0, e.Current);
            }
            if (closestNeighbours.size > 0)
            {
                yield return BuildRelevantChunksParallelAround();
            }
            end = DateTime.Now;
            Debug.Log("Total millis: " + (end - start).TotalMilliseconds);
            if (totalTriBuild >= maxTrianglesLeft)
            {
                Debug.Log("Aborted");
            }
            Debug.Log("Total triangles: " + totalTriBuild);

            Debug.Log($"Number of chunks: {Chunks.Count}");
        }

        private IEnumerator BuildRelevantChunksParallelAround()
        {
            Vector3Int next;
            bool isNextInProgress = false;

            do
            {
                next = closestNeighbours.Dequeue();
                isNextInProgress = HasChunkStartedAt(next);
            } while (isNextInProgress && closestNeighbours.size > 0);

            if (!isNextInProgress)
            {
                CreateChunkParallelAt(next, OnChunkDoneCallBack);
            }
            if (totalTriBuild < maxTrianglesLeft)
            {
                while ((closestNeighbours.size == 0 && channeledChunks > 0)/* ||channeledChunks > maxRunningThreads*/)
                {
                    // Debug.Log(channeledChunks);
                    yield return null;
                }
                if (closestNeighbours.size > 0)
                {
                    //yield return new WaitForSeconds(0.03f);
                    yield return BuildRelevantChunksParallelAround();
                }
            }
        }


        protected void OnChunkDoneCallBack(IMarchingCubeChunk chunk)
        {
            channeledChunks--;
            var e = chunk.NeighbourIndices.GetEnumerator();
            Vector3Int v3;
            while (e.MoveNext())
            {
                v3 = e.Current;
                float distance = (startPos - AnchorFromChunkCoords(v3)).magnitude;
                if (!Chunks.ContainsKey(v3) && distance < buildAroundDistance)
                {
                    closestNeighbours.Enqueue(distance, v3);
                }
            }
        }

        protected int channeledChunks = 0;

        public void CheckChunksAround(Vector3 v)
        {
            Vector3Int chunkIndex = PositionToNormalCoord(v);

            // SetActivationOfChunks(chunkIndex);

            Vector3Int index = new Vector3Int();
            for (int x = -NeededChunkAmount / 2; x < NeededChunkAmount / 2 + 1; x++)
            {
                index.x = x;
                for (int y = Mathf.Max(-NeededChunkAmount / 2, -NeededChunkAmount / 2); y < NeededChunkAmount / 2 + 1; y++)
                {
                    index.y = y;
                    for (int z = -NeededChunkAmount / 2; z < NeededChunkAmount / 2 + 1; z++)
                    {
                        index.z = z;
                        Vector3Int shiftedIndex = index + chunkIndex;
                        if (!chunks.ContainsKey(shiftedIndex))
                        {
                            CreateChunkAt(shiftedIndex);
                        }
                    }
                }
            }
        }


        protected IMarchingCubeChunk FindNonEmptyChunkAround(Vector3 pos)
        {
            bool isEmpty = true;
            Vector3Int chunkIndex = PositionToNormalCoord(pos);
            IMarchingCubeChunk chunk = null;
            while (isEmpty)
            {
                chunk = CreateChunkAt(chunkIndex);
                isEmpty = chunk.IsEmpty;
                if (chunk.IsEmpty)
                {
                    if (chunk.IsCompletlySolid)
                    {
                        chunkIndex.y += 1;
                    }
                    else
                    {
                        chunkIndex.y -= 1;
                    }
                }
            }
            return chunk;
        }



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

        protected void CreateChunkParallelAt(Vector3Int p, Action<IMarchingCubeChunk> OnDone)
        {
            int lodPower = GetLodPowerAt(p);
            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(p, lodPower);
            BuildChunkParallel(p, chunk, () => OnDone(chunk), RoundToPowerOf2(lodPower));
        }

        protected IMarchingCubeChunk CreateChunkAt(Vector3Int p)
        {
            int lodPower = DEFAULT_MIN_CHUNK_LOD_POWER;
            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(p, lodPower);
            BuildChunk(p, chunk, RoundToPowerOf2(lodPower));
            return chunk;
        }

        public bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk)
        {
            if (chunks.TryGetValue(p, out chunk))
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
            return false;
        }

        /// <summary>
        /// gets or creates a chunk at position. Fails if at position a chunk is being created asynchronously
        /// </summary>
        /// <param name="p"></param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public bool TryGetOrCreateChunk(Vector3Int p, out IMarchingCubeChunk chunk)
        {
            if (chunks.TryGetValue(p, out chunk))
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
                //StartCoroutine(BuildRelevantChunksParallelAround(chunk));
                //chunk = GetChunkObjectAt(p);
                //chunk.LOD = GetLodAt(p);
                //chunk.Material = chunkMaterial;
                //chunk.AnchorPos = AnchorFromChunkCoords(p);
                //chunk.InitializeEmpty(this, GetNeighbourLODSFrom(p), surfaceLevel);
            }
            return true;
        }

        public bool HasChunkStartedAt(Vector3Int p)
        {
            IMarchingCubeChunk chunk;
            if (chunks.TryGetValue(p, out chunk))
            {
                return chunk.HasStarted;
            }
            return false;
        }

        protected IMarchingCubeChunk GetChunkObjectAt<T>(Vector3Int p, int lodPower) where T : IMarchingCubeChunk, new()
        {
            IMarchingCubeChunk chunk = new T();
            chunks.Add(p, chunk);
            chunk.ChunkOffset = p;
            chunk.AnchorPos = AnchorFromChunkCoords(p);
            chunk.Material = chunkMaterial;
            chunk.LODPower = lodPower;
            return chunk;
        }

        protected IMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int p, int lodPower)
        {
            if (lodPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
                return GetChunkObjectAt<MarchingCubeChunkThreaded>(p, lodPower);
            else
                return GetChunkObjectAt<CompressedMarchingCubeChunkThreaded>(p, lodPower);
        }

        protected Vector3Int PositionToNormalCoord(Vector3 pos)
        {
            Vector3Int result = new Vector3Int();

            for (int i = 0; i < 3; i++)
            {
                result[i] = (int)(pos[i] / ChunkSize);
            }

            return result;
        }

        protected Vector3Int PositionToSuperChunkCoord(Vector3 pos)
        {
            Vector3Int result = new Vector3Int();

            for (int i = 0; i < 3; i++)
            {
                result[i] = (int)(pos[i] / SuperChunkSize);
            }

            return result;
        }

        public const int CHUNK_SIZE_DIFF = SuperChunkSize / ChunkSize;

        public MarchingCubeChunk GetChunkAt(Vector3 pos)
        {
            MarchingCubeChunk result = null;
            Vector3Int superChunkPos = new Vector3Int();
            Vector3Int chunkPos = new Vector3Int();
            for (int i = 0; i < 3; i++)
            {
                chunkPos[i] = (int)(pos[i] / ChunkSize);
                superChunkPos[i] = chunkPos[i] / CHUNK_SIZE_DIFF;
                chunkPos[i] = chunkPos[i] % CHUNK_SIZE_DIFF;
            }

            return result;
        }

        public int totalTriBuild;

        TriangleBuilder[] tris;// = new TriangleBuilder[CHUNK_VOLUME * 5];
        float[] pointsArray;

        private ComputeBuffer triangleBuffer;
        private ComputeBuffer pointsBuffer;
        private ComputeBuffer triCountBuffer;

        protected MarchingCubeChunkNeighbourLODs GetNeighbourLODSFrom(Vector3Int coord)
        {
            MarchingCubeChunkNeighbourLODs result = new MarchingCubeChunkNeighbourLODs();
            Vector3Int[] coords = coord.GetAllDirectNeighbours();
            for (int i = 0; i < coords.Length; i++)
            {
                MarchingCubeNeighbour neighbour = new MarchingCubeNeighbour();
                if (!Chunks.TryGetValue(coords[i], out neighbour.chunk))
                {
                    neighbour.estimatedLodPower = GetLodPowerAt(coords[i]);
                }
                result[i] = neighbour;
            }
            return result;
        }

       

        protected void BuildChunk(Vector3Int p, IMarchingCubeChunk chunk, int lod)
        {
            int numTris = ApplyChunkDataAndDispatchAndGetShaderData(p, chunk, lod);
            chunk.InitializeWithMeshData(tris, pointsArray, this, GetNeighbourLODSFrom(p), surfaceLevel);
        }

        protected void BuildChunkParallel(Vector3Int p, IMarchingCubeChunk chunk, Action OnDone, int lod)
        {
            int numTris = ApplyChunkDataAndDispatchAndGetShaderData(p, chunk, lod);
            channeledChunks++;
            chunk.InitializeWithMeshDataParallel(tris, pointsArray, this, GetNeighbourLODSFrom(p), surfaceLevel, OnDone);
        }

        //protected void RebuildChunkParallelAt(Vector3Int p, Action OnDone, int lod)
        //{
        //    RebuildChunkParallelAt(chunks[p]);
        //}


        protected int ApplyChunkDataAndDispatchAndGetShaderData(Vector3Int p, IMarchingCubeChunk chunk, int lod)
        {
            if (ChunkSize % lod != 0)
                throw new Exception("Lod must be divisor of chunksize");

            int extraSize = lod;
            extraSize = 1;


            int numVoxelsPerAxis = ChunkSize / lod * extraSize;
            //int chunkVolume = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            int pointsPerAxis = numVoxelsPerAxis + 1;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;

            CreateAllBuffersWithSizes(numVoxelsPerAxis);

            float spacing = lod;
            Vector3 anchor = chunk.AnchorPos;

            //chunk.SizeGrower = extraSize;

            densityGenerator.Generate(pointsBuffer, pointsPerAxis, 0, anchor, spacing);

            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

            triangleBuffer.SetCounterValue(0);
            marshShader.SetBuffer(0, "points", pointsBuffer);
            marshShader.SetBuffer(0, "triangles", triangleBuffer);
            marshShader.SetInt("numPointsPerAxis", pointsPerAxis);
            marshShader.SetFloat("surfaceLevel", surfaceLevel);
            marshShader.SetFloat("spacing", spacing);
            marshShader.SetVector("anchor", new Vector4(anchor.x, anchor.y, anchor.z));

            marshShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

            // Get number of triangles in the triangle buffer
            ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
            int[] triCountArray = { 0 };
            triCountBuffer.GetData(triCountArray);
            int numTris = triCountArray[0];

            // Get triangle data from shader

            tris = new TriangleBuilder[numTris];
            triangleBuffer.GetData(tris, 0, 0, numTris);

            pointsArray = new float[pointsVolume];

            pointsBuffer.GetData(pointsArray, 0, 0, pointsArray.Length);

            totalTriBuild += numTris;
            ReleaseBuffers();

            return numTris;
        }

        public void DecreaseChunkLod(IMarchingCubeChunk chunk, int toLodPower)
        {
            int toLod = RoundToPowerOf2(toLodPower);
            if (toLod <= chunk.LOD || ChunkSize % toLod != 0)
                throw new Exception("invalid new chunk lod");

            int shrinkFactor = toLod / chunk.LOD;

            int numVoxelsPerAxis = ChunkSize / toLod;

            CreateAllBuffersWithSizes(numVoxelsPerAxis);

            float spacing = toLod;

            int originalPointsPerAxis = chunk.PointsPerAxis;

            int newPointsPerAxis = (originalPointsPerAxis - 1) / shrinkFactor + 1;

            float[] points = chunk.Points;

            float[] relevantPoints = new float[newPointsPerAxis * newPointsPerAxis * newPointsPerAxis];

            int addCount = 0;

            NotifyNeighbourChunksOnLodSwitch(chunk.ChunkOffset, toLodPower);

            for (int z = 0; z < originalPointsPerAxis; z += shrinkFactor)
            {
                int zPoint = z * originalPointsPerAxis * originalPointsPerAxis;
                for (int y = 0; y < originalPointsPerAxis; y += shrinkFactor)
                {
                    int yPoint = y * originalPointsPerAxis;
                    for (int x = 0; x < originalPointsPerAxis; x += shrinkFactor)
                    {
                        relevantPoints[addCount] = points[zPoint + yPoint + x];
                        addCount++;
                    }
                }
            }

            pointsBuffer.SetData(relevantPoints);
            Vector3 anchor = AnchorFromChunkCoords(chunk.ChunkOffset);

            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

            triangleBuffer.SetCounterValue(0);
            marshShader.SetBuffer(0, "points", pointsBuffer);
            marshShader.SetBuffer(0, "triangles", triangleBuffer);
            marshShader.SetInt("numPointsPerAxis", newPointsPerAxis);
            marshShader.SetFloat("surfaceLevel", surfaceLevel);
            marshShader.SetFloat("spacing", spacing);
            marshShader.SetVector("anchor", new Vector4(anchor.x, anchor.y, anchor.z));

            marshShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

            // Get number of triangles in the triangle buffer
            ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
            int[] triCountArray = { 0 };
            triCountBuffer.GetData(triCountArray);
            int numTris = triCountArray[0];

            // Get triangle data from shader

            tris = new TriangleBuilder[numTris];
            triangleBuffer.GetData(tris, 0, 0, numTris);

            totalTriBuild += numTris;
            ReleaseBuffers();

            chunks.Remove(chunk.ChunkOffset);
            IMarchingCubeChunk compressedChunk = GetThreadedChunkObjectAt(chunk.ChunkOffset, toLodPower);
            compressedChunk.InitializeWithMeshDataParallel(tris, relevantPoints, this, GetNeighbourLODSFrom(chunk.ChunkOffset), surfaceLevel,
                delegate
                {
                    chunk.ResetChunk();
                });
        }

        protected void NotifyNeighbourChunksOnLodSwitch(Vector3Int changedIndex, int newLodPower)
        {
            Vector3Int[] neighbourPositions = changedIndex.GetAllDirectNeighbours();
            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                Vector3Int v3 = neighbourPositions[i];
                IMarchingCubeChunk c;
                if(TryGetReadyChunkAt(changedIndex + v3, out c))
                {
                    c.ChangeNeighbourLodTo(newLodPower, v3);
                }
            }
        }

        //protected int buffersCreated = 0;

        protected void CreateAllBuffersWithSizes(int chunkSize)
        {
            int points = chunkSize + 1;
            int numPoints = points * points * points;
            int numVoxels = chunkSize * chunkSize * chunkSize;
            int maxTriangleCount = numVoxels * 4;

            // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
            // Otherwise, only create if null or if size has changed
            //if (!Application.isPlaying || (pointsBuffer == null || numPoints != pointsBuffer.count))
            //{
            //    if (Application.isPlaying)
            //    {
            //        ReleaseBuffers();
            //    }
            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 1);
            triangleBuffer = new ComputeBuffer(maxTriangleCount, TriangleBuilder.SIZE_OF_TRI_BUILD, ComputeBufferType.Append);
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);


            //}
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

        public Vector3 AnchorFromChunkCoords(Vector3Int v)
        {
            return new Vector3(v.x * ChunkSize, v.y * ChunkSize, v.z * ChunkSize);
        }

        public Vector3Int SuperChunkCoordFromNormalChunkCoord(Vector3Int v)
        {
            return new Vector3Int(
                Mathf.FloorToInt(v.x * ChunkSize / SuperChunkSize),
                Mathf.FloorToInt(v.y * ChunkSize / SuperChunkSize),
                Mathf.FloorToInt(v.z * ChunkSize / SuperChunkSize)
                );
        }

        public Vector3 SuperAnchorFromSuperChunkCoords(Vector3Int v)
        {
            return new Vector3(v.x * SuperChunkSize, v.y * SuperChunkSize, v.z * SuperChunkSize);
        }


        //public static Vector3 GetCenterPosition(Vector3Int v)
        //{
        //    return AnchorFromChunkIndex(v) + Vector3.one ChunkSize * spacing / 2;
        //}

        public void EditNeighbourChunksAt(Vector3Int chunkOffset, Vector3Int cubeOrigin, float delta)
        {
            Vector3Int[] combs = cubeOrigin.GetAllCombination();
            Vector3Int v;
            for (int i = 0; i < combs.Length; i++)
            {
                v = combs[i];
                bool allActiveIndicesHaveOffset = true;
                Vector3Int offsetVector = new Vector3Int();
                for (int x = 0; x < 3 && allActiveIndicesHaveOffset; x++)
                {
                    if (v[x] != int.MinValue)
                    {
                        //offset is in range -1 to 1
                        int offset = Mathf.CeilToInt((cubeOrigin[x] / (ChunkSize - 2f)) - 1);
                        allActiveIndicesHaveOffset = offset != 0;
                        offsetVector[x] = offset;
                    }
                    else
                    {
                        offsetVector[x] = 0;
                    }
                }
                if (allActiveIndicesHaveOffset)
                {
                    //Debug.Log("Found neighbour with offset " + offsetVector);
                    IMarchingCubeChunk neighbourChunk;
                    if (TryGetOrCreateChunk(chunkOffset + offsetVector, out neighbourChunk))
                    {
                        if (neighbourChunk.LODPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
                        {
                            EditNeighbourChunkAt(neighbourChunk, cubeOrigin, offsetVector, delta);
                        }
                        else
                        {
                            Debug.LogWarning("Cant edit a neighbour mesh with higher lod! Upgrade neighbour lods if player gets too close.");
                        }
                    }
                }
            }
        }

        public void EditNeighbourChunkAt(IMarchingCubeChunk chunk, Vector3Int original, Vector3Int offset, float delta)
        {
            if (chunk is IMarchingCubeInteractableChunk interactable)
            {
                Vector3Int newChunkCubeIndex = (original + offset).Map(f => MathExt.FloorMod(f, ChunkSize));
                interactable.EditPointsNextToChunk(chunk, newChunkCubeIndex, offset, delta);
            }
            else
            {
                Debug.LogWarning("Neighbour chunk is not interactable!");
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