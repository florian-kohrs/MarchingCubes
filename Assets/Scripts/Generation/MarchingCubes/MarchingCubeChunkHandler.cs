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

        public const int ChunkSize = 64;

        public const int CHUNK_VOLUME = ChunkSize * ChunkSize * ChunkSize;

        public const int DEFAULT_MIN_CHUNK_LOD_SIZE = 1;

        public GameObject threadedChunkPrefab;

        public GameObject threadedCompressedChunkPrefab;

        // protected int maxRunningThreads = 0;

        public const int PointsPerChunkAxis = ChunkSize + 1;

        public Dictionary<Vector3Int, IMarchingCubeChunk> chunks = new Dictionary<Vector3Int, IMarchingCubeChunk>();

        public Dictionary<Vector3Int, IMarchingCubeChunk> superChunks = new Dictionary<Vector3Int, IMarchingCubeChunk>();

        [Range(1, 253)]
        public int blockAroundPlayer = 16;

        private const int maxTrianglesLeft = 3000000;

        public ComputeShader marshShader;

        public const int maxLodAtDistance = 1500;

        [Header("Voxel Settings")]
        //public float boundsSize = 8;
        public Vector3 noiseOffset = Vector3.zero;

        //[Range(2, 100)]
        //public int numPointsPerAxis = 30;

        public AnimationCurve lodForDistances;

        public Dictionary<Vector3Int, IMarchingCubeChunk> Chunks => chunks;

        public int GetLod(float sqrDistance)
        {
            return Mathf.Max(DEFAULT_MIN_CHUNK_LOD_SIZE, RoundToPowerOf2(lodForDistances.Evaluate(sqrDistance / maxLodAtDistance)));
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
            maxChunkDistance = buildAroundDistance;

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
                    if (!Chunks.ContainsKey(v3) && (startPos - AnchorFromChunkCoords(v3)).magnitude < maxChunkDistance)
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
        protected float maxChunkDistance;
        protected Queue<Vector3Int> neighbours = new Queue<Vector3Int>();

        protected SortedDictionary<float, List<Vector3Int>> sortedNeighbourds = new SortedDictionary<float, List<Vector3Int>>();

        protected void AddSortedNeighbour(float key, Vector3Int v)
        {
            List<Vector3Int> l;
            if (!sortedNeighbourds.TryGetValue(key, out l))
            {
                l = new List<Vector3Int>();
                sortedNeighbourds[key] = l;
            }
            l.Add(v);
        }

        protected Vector3Int RemoveFirst()
        {
            List<Vector3Int> l = sortedNeighbourds.Values.First();
            Vector3Int r = l[l.Count - 1];
            l.RemoveAt(l.Count - 1);
            if (l.Count == 0)
            {
                sortedNeighbourds.Remove(sortedNeighbourds.Keys.First());
            }
            return r;
        }

        public IEnumerator BuildRelevantChunksParallelAround(IMarchingCubeChunk chunk)
        {
            var e = chunk.NeighbourIndices.GetEnumerator();
            while (e.MoveNext())
            { 
                AddSortedNeighbour(0, e.Current);
            }
            if (sortedNeighbourds.Count > 0)
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
                next = RemoveFirst();
                isNextInProgress = HasChunkStartedAt(next);
            } while (isNextInProgress && sortedNeighbourds.Count > 0);

            if (!isNextInProgress)
            {
                CreateChunkParallelAt(next, OnChunkDoneCallBack);
            }
            if (totalTriBuild < maxTrianglesLeft)
            {
                while ((sortedNeighbourds.Count == 0 && channeledChunks > 0)/* ||channeledChunks > maxRunningThreads*/)
                {
                   // Debug.Log(channeledChunks);
                    yield return null;
                }
                if (sortedNeighbourds.Count > 0)
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
            while(e.MoveNext())
            {
                v3 = e.Current;
                float distance = (startPos - AnchorFromChunkCoords(v3)).magnitude;
                if (!Chunks.ContainsKey(v3) && distance < maxChunkDistance)
                {
                    AddSortedNeighbour(distance, v3);
                }
            }
        }

        protected int channeledChunks = 0;

        public void CheckChunksAround(Vector3 v)
        {
            Vector3Int chunkIndex = PositionToCoord(v);

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
            Vector3Int chunkIndex = PositionToCoord(pos);
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
            int lod = GetLodAt(p);
            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(p, lod);
            BuildChunkParallel(p, chunk, () => OnDone(chunk), lod);
        }

        protected IMarchingCubeChunk CreateChunkAt(Vector3Int p)
        {
            int lod = DEFAULT_MIN_CHUNK_LOD_SIZE;
            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(p, lod);
            BuildChunk(p, chunk, lod);
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

        protected IMarchingCubeChunk GetChunkObjectAt(Vector3Int p, GameObject prefab)
        {
            GameObject g = Instantiate(prefab, transform);
            g.name = $"Chunk({p.x},{p.y},{p.z})";
            //g.transform.position = AnchorFromChunkIndex(p);

            IMarchingCubeChunk chunk = g.GetComponent<IMarchingCubeChunk>();
            chunks.Add(p, chunk);
            chunk.ChunkOffset = p;
            return chunk;
        }

        protected IMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int p, int lod)
        {
            if(lod <= DEFAULT_MIN_CHUNK_LOD_SIZE)
                return GetChunkObjectAt(p, threadedChunkPrefab);
            else
                return GetChunkObjectAt(p, threadedCompressedChunkPrefab);
        }

        protected Vector3Int PositionToCoord(Vector3 pos)
        {
            Vector3Int result = new Vector3Int();

            for (int i = 0; i < 3; i++)
            {
                result[i] = (int)(pos[i] / ChunkSize);
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
                int current;
                IMarchingCubeChunk chunk;
                if (Chunks.TryGetValue(coords[i], out chunk))
                {
                    current = chunk.LOD;
                }
                else
                {
                    current = GetLodAt(coords[i]);
                }
                result[i] = current;
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

        protected void RebuildChunkParallelAt(Vector3Int p, IMarchingCubeChunk chunk, Action OnDone, int lod)
        {
            int numTris = ApplyChunkDataAndDispatchAndGetShaderData(p, chunk, lod);
            channeledChunks++;
            chunk.InitializeWithMeshDataParallel(tris, pointsArray, this, GetNeighbourLODSFrom(p), surfaceLevel, OnDone);
        }

        protected int ApplyChunkDataAndDispatchAndGetShaderData(Vector3Int p, IMarchingCubeChunk chunk, int lod)
        {
            if (ChunkSize % lod != 0)
                throw new Exception("Lod must be divisor of chunksize");

            int numVoxelsPerAxis = ChunkSize / lod;
            int chunkVolume = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            int pointsPerAxis = numVoxelsPerAxis + 1;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;

            CreateBuffersWithSizes(numVoxelsPerAxis);

            float spacing = lod;
            Vector3 anchor = AnchorFromChunkCoords(p);

            chunk.LOD = lod;
            chunk.Material = chunkMaterial;
            chunk.AnchorPos = anchor;

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

        //protected int buffersCreated = 0;

        protected void CreateBuffersWithSizes(int chunkSize)
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
            triangleBuffer = new ComputeBuffer(maxTriangleCount, TriangleBuilder.SIZE_OF_TRI_BUILD, ComputeBufferType.Append);
            pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 1);
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
                        if (neighbourChunk.LOD <= DEFAULT_MIN_CHUNK_LOD_SIZE)
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