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

        public const int MIN_CHUNK_SIZE = 8;

        public const int MIN_CHUNK_SIZE_POWER = 3;

        public const int CHUNK_GROUP_SIZE = 1024;

        public const int DEFAULT_CHUNK_SIZE = 32;

        public const int DEFAULT_CHUNK_SIZE_POWER = 5;

        //public const int ChunkSize = 128;

        //public const int CHUNK_VOLUME = ChunkSize * ChunkSize * ChunkSize;

        public const int DEFAULT_MIN_CHUNK_LOD_POWER = 0;

        public const int MAX_CHUNK_LOD_POWER = 7;

        public const int MAX_CHUNK_LOD_BIT_REPRESENTATION_SIZE = 3;//(int)(Mathf.Log(2,MAX_CHUNK_LOD_POWER)) + 1;

        // protected int maxRunningThreads = 0;

        //public const int PointsPerChunkAxis = ChunkSize + 1;

        //public Dictionary<Vector3Int, IMarchingCubeChunk> chunks = new Dictionary<Vector3Int, IMarchingCubeChunk>(new Vector3EqualityComparer());

        public Dictionary<Vector3Int, IChunkGroupRoot> chunkGroups = new Dictionary<Vector3Int, IChunkGroupRoot>(new Vector3EqualityComparer());

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


        public int GetSizeForChunkAtPosition(Vector3 position)
        {
            return GetSizeForChunkAtDistance((position - startPos).magnitude);
        }

        public int GetSizeForChunkAtDistance(float distance)
        {
            return (int)Mathf.Pow(2, MIN_CHUNK_SIZE_POWER + GetSizePowerForDistance(distance));
        }


        public void GetSizeAndLodPowerForChunkPosition(Vector3 pos, out int size, out int lodPower)
        {
            float distance = (startPos - pos).magnitude;
            lodPower = GetLodPower(distance);
            size = GetSizeForChunkAtDistance(distance);
        }

        protected int RoundToPowerOf2(float f)
        {
            int r = (int)Mathf.Pow(2, Mathf.RoundToInt(f));

            return Mathf.Max(1, r);
        }



        //protected int NeededChunkAmount
        //{
        //    get
        //    {
        //        int amount = Mathf.CeilToInt(blockAroundPlayer / PointsPerChunkAxis);
        //        if (amount % 2 == 1)
        //        {
        //            amount += 1;
        //        }
        //        return amount;
        //    }
        //}

        //public PlanetMarchingCubeNoise noiseFilter;

        //public TerrainNoise terrainNoise;

        public BaseDensityGenerator densityGenerator;

        public bool useTerrainNoise;


        public int deactivateAfterDistance = 40;

        //protected int DeactivatedChunkDistance => Mathf.CeilToInt(deactivateAfterDistance / PointsPerChunkAxis);

        public Material chunkMaterial;

        [Range(0, 1)]
        public float surfaceLevel = 0.45f;

        public Transform player;

        public int buildAroundDistance = 2;

        protected long buildAroundSqrDistance;

        DateTime start;
        DateTime end;

        private void Start()
        {
            start = DateTime.Now;
            buildAroundSqrDistance = (long)buildAroundDistance * buildAroundDistance;
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

        protected Vector3 startPos;
        protected float maxSqrChunkDistance;
        protected Queue<Vector3Int> neighbours = new Queue<Vector3Int>();

        protected BinaryHeap<float, Vector3Int> closestNeighbours = new BinaryHeap<float, Vector3Int>(float.MinValue, float.MaxValue, 200);

        //public void BuildRelevantChunksAround(IMarchingCubeChunk chunk)
        //{
        //    Queue<IMarchingCubeChunk> neighboursToBuild = new Queue<IMarchingCubeChunk>();
        //    neighboursToBuild.Enqueue(chunk);
        //    while (neighboursToBuild.Count > 0)
        //    {
        //        IMarchingCubeChunk current = neighboursToBuild.Dequeue();
        //        foreach (var v3 in current.NeighbourIndices)
        //        {
        //            if (!HasChunkAtPosition(v3) && (startPos - AnchorFromChunkCoords(v3)).magnitude < buildAroundDistance)
        //            {
        //                neighboursToBuild.Enqueue(CreateChunkAt(v3));
        //                if (totalTriBuild >= maxTrianglesLeft)
        //                {
        //                    Debug.Log("Aborted");
        //                    neighboursToBuild.Clear();
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    end = DateTime.Now;
        //    Debug.Log("Total millis: " + (end - start).TotalMilliseconds);

        //    Debug.Log("Total triangles: " + totalTriBuild);

        //    Debug.Log($"Number of chunkgroups: {ChunkGroups.Count}");
        //}



        public IEnumerator BuildRelevantChunksParallelAround(IMarchingCubeChunk chunk)
        {
            List<Vector3Int> neighboours = chunk.NeighbourIndices;
            for (int i = 0; i < neighboours.Count; i++)
            {
                closestNeighbours.Enqueue(0, neighboours[i]);
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

            Debug.Log($"Number of chunks: {ChunkGroups.Count}");
        }

        private IEnumerator BuildRelevantChunksParallelAround()
        {
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
                    CreateChunkParallelAt(next, OnChunkDoneCallBack);
                }
                if (totalTriBuild < maxTrianglesLeft)
                {
                    while ((closestNeighbours.size == 0 && channeledChunks > 0) /*|| channeledChunks > maxRunningThreads*/)
                    {
                        yield return null;
                    }
                }
            }
        }

        protected void OnChunkDoneCallBack(IMarchingCubeChunk chunk)
        {
            channeledChunks--;

            float orgSqrDistance = (startPos - chunk.CenterPos).sqrMagnitude;
            Vector3Int v3;
            List<Vector3Int> dirs = chunk.NeighbourIndices;
            for (int i = 0; i < dirs.Count; i++)
            {
                v3 = dirs[i];
                float sqrDist = (startPos - v3).sqrMagnitude;

                ///only add neighbours if
                if (sqrDist <= buildAroundSqrDistance
                    && !HasChunkAtPosition(v3))
                {
                    closestNeighbours.Enqueue(sqrDist, v3);
                }
            }
        }

        protected int channeledChunks = 0;

        //public void CheckChunksAround(Vector3 v)
        //{
        //    Vector3Int chunkIndex = PositionToNormalCoord(v);

        //    // SetActivationOfChunks(chunkIndex);

        //    Vector3Int index = new Vector3Int();
        //    for (int x = -NeededChunkAmount / 2; x < NeededChunkAmount / 2 + 1; x++)
        //    {
        //        index.x = x;
        //        for (int y = Mathf.Max(-NeededChunkAmount / 2, -NeededChunkAmount / 2); y < NeededChunkAmount / 2 + 1; y++)
        //        {
        //            index.y = y;
        //            for (int z = -NeededChunkAmount / 2; z < NeededChunkAmount / 2 + 1; z++)
        //            {
        //                index.z = z;
        //                Vector3Int shiftedIndex = index + chunkIndex;
        //                if (!chunks.ContainsKey(shiftedIndex))
        //                {
        //                    CreateChunkAt(shiftedIndex);
        //                }
        //            }
        //        }
        //    }
        //}


        protected IMarchingCubeChunk FindNonEmptyChunkAround(Vector3 pos)
        {
            bool isEmpty = true;
            Vector3Int chunkIndex = PositionToChunkGroupCoord(pos);
            IMarchingCubeChunk chunk = null;
            while (isEmpty)
            {
                chunkIndex = PositionToChunkGroupCoord(pos);
                chunk = CreateChunkAt(pos, chunkIndex);
                isEmpty = chunk.IsEmpty;
                if (chunk.IsEmpty)
                {
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

        protected void CreateChunkParallelAt(Vector3 pos, Action<IMarchingCubeChunk> OnDone)
        {
            CreateChunkParallelAt(pos, PositionToChunkGroupCoord(pos), OnDone);
        }

        protected void CreateChunkParallelAt(Vector3 pos, Vector3Int coord, Action<IMarchingCubeChunk> OnDone)
        {
            int lodPower;
            int chunkSize;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSize, out lodPower);
            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(VectorExtension.ToVector3Int(pos), coord, lodPower, chunkSize);
            BuildChunkParallel(chunk, RoundToPowerOf2(lodPower), () => OnDone(chunk));
        }

        protected IMarchingCubeChunk CreateChunkAt(Vector3Int p)
        {
            return CreateChunkAt(CoordToPosition(p));
        }

        protected IMarchingCubeChunk CreateChunkAt(Vector3 pos, Vector3Int coord)
        {
            int lodPower;
            int chunkSize;
            GetSizeAndLodPowerForChunkPosition(pos, out chunkSize, out lodPower);
            IMarchingCubeChunk chunk = GetThreadedChunkObjectAt(VectorExtension.ToVector3Int(pos), coord, lodPower, chunkSize);
            BuildChunk(chunk, RoundToPowerOf2(lodPower));
            return chunk;
        }

        protected IChunkGroupRoot CreateChunkGroupAtPosition(Vector3Int p)
        {
            return CreateChunkGroupAtCoordinate(PositionToChunkGroupCoord(p));
        }

        protected IChunkGroupRoot CreateChunkGroupAtCoordinate(Vector3Int coord)
        {
            IChunkGroupRoot chunkGroup = new ChunkGroupRoot(coord);
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
                Vector3Int restPosition = p - chunkGroup.GroupAnchorPosition;
                if (chunkGroup.TryGetChunkAtLocalPosition(restPosition, out chunk))
                {
                    positionInOtherChunk = p - chunk.AnchorPos;
                    return true;
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
                Vector3Int restPosition = chunk.AnchorPos - chunkGroup.GroupAnchorPosition;
                chunkGroup.RemoveChunkAtGlobalPosition(restPosition);
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


        protected IMarchingCubeChunk GetChunkObjectAt<T>(Vector3Int position, Vector3Int coord, int lodPower, int chunkSize) where T : IMarchingCubeChunk, new()
        {
            IChunkGroupRoot chunkGroup = GetOrCreateChunkGroupAtCoordinate(coord);
            IMarchingCubeChunk chunk = new T();
            
            chunk.ChunkHandler = this;
            chunk.ChunkSize = chunkSize;
            chunk.Material = chunkMaterial;
            chunk.SurfaceLevel = surfaceLevel;
            chunk.LODPower = lodPower;

            chunkGroup.SetChunkAtPosition(position, chunk);

            return chunk;
        }

        protected IMarchingCubeChunk GetThreadedChunkObjectAt(Vector3Int position, Vector3Int coord, int lodPower, int chunkSize)
        {
            if (lodPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
                return GetChunkObjectAt<MarchingCubeChunkThreaded>(position, coord, lodPower, chunkSize);
            else
                return GetChunkObjectAt<CompressedMarchingCubeChunkThreaded>(position, coord, lodPower, chunkSize);
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


        protected void CompareNeighboursNoise(IMarchingCubeChunk chunk)
        {
            try
            {
                CreateAllBuffersWithSizes(4);
                Vector3Int[] coords = VectorExtension.GetAllAdjacentDirections;
                for (int i = 0; i < coords.Length; i++)
                {
                    IMarchingCubeChunk other;
                    Vector3Int neighbourPos = chunk.CenterPos + chunk.ChunkSize * coords[i];
                    if (TryGetReadyChunkAt(neighbourPos, out other))
                    {
                        CompareNoiseValues(coords[i], chunk, other);
                    }
                }
            }
            catch (Exception xx)
            {
                Debug.Log("why?");
            }
            finally{
                ReleaseBuffers();
            }
        }

        public void CompareNoiseValues(Vector3Int offset, IMarchingCubeChunk chunk, IMarchingCubeChunk other)
        {
            Vector3Int ind1 = default, ind2 = default;
            var p = other.Points;
            if (other.ChunkSize == chunk.ChunkSize)
            {
                for (int x = 0; x < chunk.ChunkSize; x++)
                {
                    for (int y = 0; y < chunk.ChunkSize; y++)
                    {
                        float n1 = 0, n2 = 0;
                        if (offset.x < 0)
                        {
                            ind1 = new Vector3Int(0, x, y);
                            ind2 = new Vector3Int(chunk.ChunkSize, x, y);
                        }
                        else if (offset.x > 0)
                        {
                            ind1 = new Vector3Int(chunk.ChunkSize, x, y);
                            ind2 = new Vector3Int(0, x, y);
                        }
                        else if (offset.y < 0)
                        {
                            ind1 = new Vector3Int(x, 0, y);
                            ind2 = new Vector3Int(x, chunk.ChunkSize, y);
                        }
                        else if (offset.y > 0)
                        {
                            ind1 = new Vector3Int(x, chunk.ChunkSize, y);
                            ind2 = new Vector3Int(x, 0, y);
                        }
                        else if (offset.z < 0)
                        {
                            ind1 = new Vector3Int(x, y, 0);
                            ind2 = new Vector3Int(x, y, chunk.ChunkSize);
                        }
                        else if (offset.z > 0)
                        {
                            ind1 = new Vector3Int(x, y, chunk.ChunkSize);
                            ind2 = new Vector3Int(x, y, 0);
                        }

                        n1 = chunk.Points[chunk.PointIndexFromCoord(ind1)];
                        n2 = other.Points[other.PointIndexFromCoord(ind2)];
                        float diff = Mathf.Abs(n1 - n2);
                        if (diff > 0)
                        {
                            densityGenerator.TestGenerateAt(pointsBuffer, chunk.AnchorPos + ind1, n1, n2);
                            Debug.Log("Diff:" + diff);
                            return;
                        }
                        //else
                        //{
                        //    Debug.Log("All good");
                        //}


                    }
                }
            }
        }

        protected MarchingCubeChunkNeighbourLODs GetNeighbourLODSFrom(IMarchingCubeChunk chunk)
        {
            MarchingCubeChunkNeighbourLODs result = new MarchingCubeChunkNeighbourLODs();
            Vector3Int[] coords = VectorExtension.GetAllAdjacentDirections;
            for (int i = 0; i < coords.Length; i++)
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

        protected void BuildChunk(IMarchingCubeChunk chunk, int lod)
        {
            int numTris = ApplyChunkDataAndDispatchAndGetShaderData(chunk, lod);
            chunk.InitializeWithMeshData(tris, pointsArray, GetNeighbourLODSFrom(chunk));
        }

        protected void BuildChunkParallel(IMarchingCubeChunk chunk, int lod, Action OnDone)
        {
            int numTris = ApplyChunkDataAndDispatchAndGetShaderData(chunk, lod);
            channeledChunks++;
            chunk.InitializeWithMeshDataParallel(tris, pointsArray, GetNeighbourLODSFrom(chunk), OnDone);
            CompareNeighboursNoise(chunk);
        }

        //protected void RebuildChunkParallelAt(Vector3Int p, Action OnDone, int lod)
        //{
        //    RebuildChunkParallelAt(chunks[p]);
        //}


        protected int ApplyChunkDataAndDispatchAndGetShaderData(IMarchingCubeChunk chunk, int lod)
        {
            ///Maybe read border noiose points from neighbour?

            int chunkSize = chunk.ChunkSize;
            if (chunkSize % lod != 0)
                throw new Exception("Lod must be divisor of chunksize");

            int extraSize = lod;
            extraSize = 1;


            int numVoxelsPerAxis = chunkSize / lod * extraSize;
            //int chunkVolume = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            int pointsPerAxis = numVoxelsPerAxis + 1;
            int pointsVolume = pointsPerAxis * pointsPerAxis * pointsPerAxis;

            CreateAllBuffersWithSizes(128);

            float spacing = lod;
            Vector3 anchor = chunk.AnchorPos;

            //chunk.SizeGrower = extraSize;

            //densityGenerator.TestGenerate(pointsBuffer);
            //return -1;
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
            throw new NotImplementedException();

            //int toLod = RoundToPowerOf2(toLodPower);
            //if (toLod <= chunk.LOD || chunk.ChunkSize % toLod != 0)
            //    throw new Exception("invalid new chunk lod");

            //int shrinkFactor = toLod / chunk.LOD;

            //int numVoxelsPerAxis = chunk.ChunkSize / toLod;

            //CreateAllBuffersWithSizes(numVoxelsPerAxis);

            //float spacing = toLod;

            //int originalPointsPerAxis = chunk.PointsPerAxis;

            //int newPointsPerAxis = (originalPointsPerAxis - 1) / shrinkFactor + 1;

            //float[] points = chunk.Points;

            //float[] relevantPoints = new float[newPointsPerAxis * newPointsPerAxis * newPointsPerAxis];

            //int addCount = 0;

            //NotifyNeighbourChunksOnLodSwitch(chunk.ChunkAnchorPosition, toLodPower);

            //for (int z = 0; z < originalPointsPerAxis; z += shrinkFactor)
            //{
            //    int zPoint = z * originalPointsPerAxis * originalPointsPerAxis;
            //    for (int y = 0; y < originalPointsPerAxis; y += shrinkFactor)
            //    {
            //        int yPoint = y * originalPointsPerAxis;
            //        for (int x = 0; x < originalPointsPerAxis; x += shrinkFactor)
            //        {
            //            relevantPoints[addCount] = points[zPoint + yPoint + x];
            //            addCount++;
            //        }
            //    }
            //}

            //pointsBuffer.SetData(relevantPoints);
            //Vector3 anchor = chunk.ChunkAnchorPosition;

            //int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

            //triangleBuffer.SetCounterValue(0);
            //marshShader.SetBuffer(0, "points", pointsBuffer);
            //marshShader.SetBuffer(0, "triangles", triangleBuffer);
            //marshShader.SetInt("numPointsPerAxis", newPointsPerAxis);
            //marshShader.SetFloat("surfaceLevel", surfaceLevel);
            //marshShader.SetFloat("spacing", spacing);
            //marshShader.SetVector("anchor", new Vector4(anchor.x, anchor.y, anchor.z));

            //marshShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

            //// Get number of triangles in the triangle buffer
            //ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
            //int[] triCountArray = { 0 };
            //triCountBuffer.GetData(triCountArray);
            //int numTris = triCountArray[0];

            //// Get triangle data from shader

            //tris = new TriangleBuilder[numTris];
            //triangleBuffer.GetData(tris, 0, 0, numTris);

            //totalTriBuild += numTris;
            //ReleaseBuffers();

            ////TryGetChunkAtPosition(chunk.CenterPos);
            //IMarchingCubeChunk compressedChunk = GetThreadedChunkObjectAt(chunk.AnchorPos, toLodPower);
            //compressedChunk.InitializeWithMeshDataParallel(tris, relevantPoints, ChunkSize, this, GetNeighbourLODSFrom(chunk), surfaceLevel,
            //    delegate
            //    {
            //        chunk.ResetChunk();
            //    });
        }

        //protected void NotifyNeighbourChunksOnLodSwitch(Vector3Int changedIndex, int newLodPower)
        //{
        //    Vector3Int[] neighbourPositions = changedIndex.GetAllAdjacentDirections();
        //    for (int i = 0; i < neighbourPositions.Length; i++)
        //    {
        //        Vector3Int v3 = neighbourPositions[i];
        //        IMarchingCubeChunk c;
        //        if (TryGetReadyChunkAt(changedIndex + v3, out c))
        //        {
        //            c.ChangeNeighbourLodTo(newLodPower, v3);
        //        }
        //    }
        //}

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


        //public static Vector3 GetCenterPosition(Vector3Int v)
        //{
        //    return AnchorFromChunkIndex(v) + Vector3.one ChunkSize * spacing / 2;
        //}

        public void EditNeighbourChunksAt(Vector3Int chunkOffset, Vector3Int cubeOrigin, float delta)
        {
            throw new NotImplementedException();
            //Vector3Int[] combs = cubeOrigin.GetAllCombination();
            //Vector3Int v;
            //for (int i = 0; i < combs.Length; i++)
            //{
            //    v = combs[i];
            //    bool allActiveIndicesHaveOffset = true;
            //    Vector3Int offsetVector = new Vector3Int();
            //    for (int x = 0; x < 3 && allActiveIndicesHaveOffset; x++)
            //    {
            //        if (v[x] != int.MinValue)
            //        {
            //            //offset is in range -1 to 1
            //            int offset = Mathf.CeilToInt((cubeOrigin[x] / (chunkSize - 2f)) - 1);
            //            allActiveIndicesHaveOffset = offset != 0;
            //            offsetVector[x] = offset;
            //        }
            //        else
            //        {
            //            offsetVector[x] = 0;
            //        }
            //    }
            //    if (allActiveIndicesHaveOffset)
            //    {
            //        //Debug.Log("Found neighbour with offset " + offsetVector);
            //        IMarchingCubeChunk neighbourChunk;
            //        if (TryGetOrCreateChunk(chunkOffset + offsetVector, out neighbourChunk))
            //        {
            //            if (neighbourChunk.LODPower <= DEFAULT_MIN_CHUNK_LOD_POWER)
            //            {
            //                EditNeighbourChunkAt(neighbourChunk, cubeOrigin, offsetVector, delta);
            //            }
            //            else
            //            {
            //                Debug.LogWarning("Cant edit a neighbour mesh with higher lod! Upgrade neighbour lods if player gets too close.");
            //            }
            //        }
            //    }
            //}
        }

        public void EditNeighbourChunkAt(IMarchingCubeChunk chunk, Vector3Int original, Vector3Int offset, float delta)
        {
            throw new NotImplementedException();
            //if (chunk is IMarchingCubeInteractableChunk interactable)
            //{
            //    Vector3Int newChunkCubeIndex = (original + offset).Map(f => MathExt.FloorMod(f, ChunkSize));
            //    interactable.EditPointsNextToChunk(chunk, newChunkCubeIndex, offset, delta);
            //}
            //else
            //{
            //    Debug.LogWarning("Neighbour chunk is not interactable!");
            //}
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