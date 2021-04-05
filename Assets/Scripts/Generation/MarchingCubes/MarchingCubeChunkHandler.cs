using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeChunkHandler : MonoBehaviour, IMarchingCubeChunkHandler
    {

        protected int kernelId;

        protected const int threadGroupSize = 8;

        public const int ChunkSize = 50;

        public const int CHUNK_VOLUME = ChunkSize * ChunkSize * ChunkSize;

        public GameObject chunkPrefab;

        public const int PointsPerChunkAxis = ChunkSize + 1;

        public Dictionary<Vector3Int, MarchingCubeChunk> chunks = new Dictionary<Vector3Int, MarchingCubeChunk>();

        [Range(1,253)]
        public int blockAroundPlayer = 16;

        private const int maxTrianglesLeft = 2000000;

        public ComputeShader marshShader;

        [Header("Voxel Settings")]
        //public float boundsSize = 8;
        public Vector3 noiseOffset = Vector3.zero;

        //[Range(2, 100)]
        //public int numPointsPerAxis = 30;


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

        private void Start()
        {
            Debug.Log(SystemInfo.processorCount);
            CreateBuffersIfNeeded();
            kernelId = marshShader.FindKernel("March");
            MarchingCubeChunk chunk = FindNonEmptyChunkAround(player.position);
            BuildRelevantChunksAround(chunk, chunk.chunkOffset, buildAroundDistance);
            ReleaseBuffersIfNeeded();
            Debug.Log($"Number of chunks: {Chunks.Count}");
        }

        private void Update()
        {
            //CheckChunksAround(player.position);
        }

        protected IEnumerator UpdateChunks()
        {
            yield return null;


            //yield return new WaitForSeconds(3);

            yield return UpdateChunks();
        }


        public void BuildRelevantChunksAround(MarchingCubeChunk chunk, Vector3Int startPos, int radius)
        {
            BuildRelevantChunksAround(chunk, startPos * ChunkSize, radius * radius, new Queue<Vector3Int>());
        }

        public void BuildRelevantChunksAround(MarchingCubeChunk chunk, Vector3Int startPos, int sqrRadius, Queue<Vector3Int> neighbours)
        {
            do
            {
                if (chunk.NeighboursReachableFrom.Count <= 0)
                    return;

                foreach (Vector3Int v3 in chunk.NeighbourIndices)
                {
                    if (!Chunks.ContainsKey(v3) && (startPos - v3 * ChunkSize).sqrMagnitude < sqrRadius)
                    {
                        MarchingCubeChunk newChunk = CreateChunkAt(v3);
                        foreach (Vector3Int newV3 in newChunk.NeighbourIndices)
                        {
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
            if(totalTriBuild >= maxTrianglesLeft)
            {
                Debug.Log("Aborted");
            }
            Debug.Log("Total triangles: " + totalTriBuild);
        }



        public void CheckChunksAround(Vector3 v)
        {
            CreateBuffersIfNeeded();

            Vector3Int chunkIndex = PositionToCoord(v);

            SetActivationOfChunks(chunkIndex);

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
                        MarchingCubeChunk c;
                        if (!chunks.TryGetValue(shiftedIndex, out c))
                        {
                            CreateChunkAt(shiftedIndex);
                        }
                    }
                }
            }

            ReleaseBuffersIfNeeded();
        }


        protected MarchingCubeChunk FindNonEmptyChunkAround(Vector3 pos)
        {
            bool isEmpty = true;
            CreateBuffersIfNeeded();
            Vector3Int chunkIndex = PositionToCoord(pos);
            MarchingCubeChunk chunk = null;
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
            ReleaseBuffersIfNeeded();
            return chunk;
        }



        protected void SetActivationOfChunks(Vector3Int center)
        {
            int deactivatedChunkSqrDistance = DeactivatedChunkDistance;
            deactivatedChunkSqrDistance *= deactivatedChunkSqrDistance;
            foreach (KeyValuePair<Vector3Int, MarchingCubeChunk> kv in chunks)
            {
                int sqrMagnitude = (kv.Key - center).sqrMagnitude;
                kv.Value.gameObject.SetActive(sqrMagnitude <= deactivatedChunkSqrDistance);
            }
        }

        protected MarchingCubeChunk CreateChunkAt(Vector3Int p)
        {
            //GameObject g = new GameObject("Chunk" + "(" + p.x + "," + p.y + "," + p.z + ")");
            GameObject g = Instantiate(chunkPrefab, transform);
            g.name = $"Chunk({p.x},{p.y},{p.z})";
            //g.transform.position = p * CHUNK_SIZE;

            MarchingCubeChunk chunk = g.GetComponent<MarchingCubeChunk>();
            chunks.Add(p, chunk);
            chunk.chunkOffset = p;
            BuildChunk(p, chunk);
            return chunk;
        }


        protected Vector3Int PositionToCoord(Vector3 pos)
        {
            Vector3Int result = new Vector3Int();

            for (int i = 0; i < 3; i++)
            {
                result[i] = (int)(pos[i] / PointsPerChunkAxis);
            }

            return result;
        }

        public int totalTriBuild;

        TriangleBuilder[] tris = new TriangleBuilder[CHUNK_VOLUME * 5];
        float[] pointsArray;

        private ComputeBuffer triangleBuffer;
        private ComputeBuffer pointsBuffer;
        private ComputeBuffer triCountBuffer;

        protected void BuildChunk(Vector3Int p, MarchingCubeChunk chunk)
        {
            Vector3 center = CenterFromChunkIndex(p);
            densityGenerator.Generate(pointsBuffer, PointsPerChunkAxis, 0, center, 1);

            int numVoxelsPerAxis = ChunkSize;
            int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

            triangleBuffer.SetCounterValue(0);
            marshShader.SetBuffer(0, "points", pointsBuffer);
            marshShader.SetBuffer(0, "triangles", triangleBuffer);
            marshShader.SetInt("numPointsPerAxis", PointsPerChunkAxis);
            marshShader.SetFloat("surfaceLevel", surfaceLevel);
            marshShader.SetFloat("spacing", 1);
            marshShader.SetVector("centre", new Vector4(center.x, center.y, center.z));

            marshShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

            // Get number of triangles in the triangle buffer
            ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
            int[] triCountArray = { 0 };
            triCountBuffer.GetData(triCountArray);
            int numTris = triCountArray[0];

            // Get triangle data from shader

            triangleBuffer.GetData(tris, 0, 0, numTris);

            pointsArray = new float[CHUNK_VOLUME];
            pointsBuffer.GetData(pointsArray, 0, 0, CHUNK_VOLUME);

            totalTriBuild += numTris;

            chunk.InitializeWithMeshData(chunkMaterial, tris, numTris, pointsArray, this, surfaceLevel);

        }

        protected int buffersCreated = 0;

        void CreateBuffersIfNeeded()
        {
            buffersCreated++;
            if (buffersCreated > 1)
                return;
            int numPoints = PointsPerChunkAxis * PointsPerChunkAxis * PointsPerChunkAxis;
            int numVoxelsPerAxis = ChunkSize - 1;
            int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            int maxTriangleCount = numVoxels * 5;

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

        void ReleaseBuffersIfNeeded()
        {
            buffersCreated--;
            if (buffersCreated == 0)
            {
                if (triangleBuffer != null)
                {
                    triangleBuffer.Release();
                    pointsBuffer.Release();
                    triCountBuffer.Release();
                }
            }
        }

        public static Vector3 CenterFromChunkIndex(Vector3Int v)
        {
            return new Vector3(v.x * ChunkSize, v.y * ChunkSize, v.z * ChunkSize);
        }

        protected float PointSpacing => 1;

        public Dictionary<Vector3Int, MarchingCubeChunk> Chunks => chunks;

        public void EditNeighbourChunksAt(Vector3Int chunkOffset, Vector3Int cubeOrigin, float delta)
        {
            foreach (Vector3Int v in cubeOrigin.GetAllCombination())
            {
                bool allActiveIndicesHaveOffset = true;
                Vector3Int offsetVector = new Vector3Int();
                for (int i = 0; i < 3 && allActiveIndicesHaveOffset; i++)
                {
                    if (v[i] != int.MinValue)
                    {
                        //offset is in range -1 to 1
                        int offset = Mathf.CeilToInt((cubeOrigin[i] / (ChunkSize - 2f)) - 1);
                        allActiveIndicesHaveOffset = offset != 0;
                        offsetVector[i] = offset;
                    }
                    else
                    {
                        offsetVector[i] = 0;
                    }
                }
                if (allActiveIndicesHaveOffset)
                {
                    Debug.Log("Found neighbour with offset " + offsetVector);
                    MarchingCubeChunk neighbourChunk;
                    if (chunks.TryGetValue(chunkOffset + offsetVector, out neighbourChunk))
                    {
                        EditNeighbourChunkAt(neighbourChunk, cubeOrigin, offsetVector, delta);
                    }
                }
            }
        }

        public void EditNeighbourChunkAt(MarchingCubeChunk chunk, Vector3Int original, Vector3Int offset, float delta)
        {
            Vector3Int newChunkCubeIndex = (original + offset).Map(f => MathExt.FloorMod(f, ChunkSize));
            MarchingCubeEntity e = chunk.GetEntityAt(newChunkCubeIndex.x, newChunkCubeIndex.y, newChunkCubeIndex.z);
            chunk.EditPointsNextToChunk(chunk, e, offset, delta);
        }

        void OnDestroy()
        {
            if (Application.isPlaying)
            {
                ReleaseBuffersIfNeeded();
            }
        }

    }
}