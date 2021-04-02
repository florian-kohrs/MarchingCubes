using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeMarchingCube : IMarchingCubeChunkHandler
{

    public TreeMarchingCube(Transform transform, Dictionary<Vector3Int, float> density)
    {
        this.transform = transform;
        this.density = density;
    }

    protected Vector4[] densityArray;

    protected Vector4[] DensityArray
    {
        get
        {
            if (densityArray == null)
            {
                List<Vector4> l = new List<Vector4>();
                foreach (KeyValuePair<Vector3Int, float> pair in density)
                {
                    l.Add(new Vector4(pair.Key.x, pair.Key.y, pair.Key.z, pair.Value));
                }
                densityArray = l.ToArray();
            }
            return densityArray;
        }
    }

    protected Dictionary<Vector3Int, float> density;

    protected Transform transform;

    protected int kernelId;

    protected const int threadGroupSize = 8;

    public const int VoxelsPerChunkAxis = 4;

    public int PointsPerChunkAxis => VoxelsPerChunkAxis + 1;

    public Dictionary<Vector3Int, MarchingCubeChunk> chunks = new Dictionary<Vector3Int, MarchingCubeChunk>();

    public int blockAroundPlayer = 16;

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

    public PlanetMarchingCubeNoise noiseFilter;


    public bool useTerrainNoise;

    public Vector3 offset;

    public int deactivateAfterDistance = 40;

    protected int DeactivatedChunkDistance => Mathf.CeilToInt(deactivateAfterDistance / PointsPerChunkAxis);

    public Material chunkMaterial;

    [Range(0, 1)]
    public float surfaceLevel = 0.45f;

    public Transform player;

    private void Start()
    {
        kernelId = marshShader.FindKernel("March");
        BuildDirectNeighbourChunksAround(PositionToCoord(transform.position));
        //UpdateChunks();
        //StartCoroutine(UpdateChunks());
    }



    protected IEnumerator UpdateChunks()
    {
        yield return null;


        //yield return new WaitForSeconds(3);

        yield return UpdateChunks();
    }



    public void BuildDirectNeighbourChunksAround(Vector3Int chunkIndex)
    {
        CreateBuffers();

        Vector3Int index = new Vector3Int();
        for (int x = -1; x < 2; x++)
        {
            index.x = x;
            for (int y = -1; y < 2; y++)
            {
                index.y = y;
                for (int z = -1; z < 2; z++)
                {
                    index.z = z;
                    Vector3Int shiftedIndex = index + chunkIndex;
                    MarchingCubeChunk c;
                    if (!chunks.TryGetValue(shiftedIndex, out c))
                    {
                        bool empty = CreateChunkAt(shiftedIndex).IsEmpty;
                        if (!empty)
                        {
                            BuildDirectNeighbourChunksAround(shiftedIndex);
                        }
                    }
                }
            }
        }

        ReleaseBuffers();
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
        GameObject g = new GameObject("Chunk" + "(" + p.x + "," + p.y + "," + p.z + ")");
        g.transform.SetParent(transform, false);
        //g.transform.position = p * CHUNK_SIZE;

        MarchingCubeChunk chunk = g.AddComponent<MarchingCubeChunk>();
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

    private ComputeBuffer triangleBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer triCountBuffer;

    protected void BuildChunk(Vector3Int p, MarchingCubeChunk chunk)
    {
        pointsBuffer = new ComputeBuffer(PointsPerChunkAxis * PointsPerChunkAxis * PointsPerChunkAxis, sizeof(float) * 4);
        pointsBuffer.SetData(DensityArray);

        int numVoxelsPerAxis = VoxelsPerChunkAxis;
        int numThreadsPerAxis = Mathf.CeilToInt(numVoxelsPerAxis / (float)threadGroupSize);

        triangleBuffer.SetCounterValue(0);
        marshShader.SetBuffer(0, "points", pointsBuffer);
        marshShader.SetBuffer(0, "triangles", triangleBuffer);
        marshShader.SetInt("numPointsPerAxis", PointsPerChunkAxis);
        marshShader.SetFloat("surfaceLevel", surfaceLevel);

        marshShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        // Get triangle data from shader
        TriangleBuilder[] tris = new TriangleBuilder[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        chunk.InitializeWithMeshData(chunkMaterial, tris, pointsBuffer, this, surfaceLevel);

    }

    void CreateBuffers()
    {
        int numPoints = PointsPerChunkAxis * PointsPerChunkAxis * PointsPerChunkAxis;
        int numVoxelsPerAxis = VoxelsPerChunkAxis - 1;
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
        triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3 + sizeof(int) * 3 + sizeof(int), ComputeBufferType.Append);
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        //}
    }

    void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            pointsBuffer.Release();
            triCountBuffer.Release();
        }
    }

    protected Vector3 CenterFromChunkIndex(Vector3Int v)
    {
        return new Vector3(v.x * VoxelsPerChunkAxis, v.y * VoxelsPerChunkAxis, v.z * VoxelsPerChunkAxis);
    }

    protected float PointSpacing => 1;

    public Dictionary<Vector3Int, MarchingCubeChunk> Chunks => chunks;

    public void EditNeighbourChunksAt(MarchingCubeChunk chunk, Vector3Int p, float delta)
    {
        foreach (Vector3Int v in p.GetAllCombination())
        {
            bool allActiveIndicesHaveOffset = true;
            Vector3Int offsetVector = new Vector3Int();
            for (int i = 0; i < 3 && allActiveIndicesHaveOffset; i++)
            {
                if (v[i] != int.MinValue)
                {
                    //offset is in range -1 to 1
                    int offset = Mathf.CeilToInt((p[i] / (VoxelsPerChunkAxis - 2f)) - 1);
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
                if (chunks.TryGetValue(chunk.chunkOffset + offsetVector, out neighbourChunk))
                {
                    EditNeighbourChunkAt(neighbourChunk, p, offsetVector, delta);
                }
            }
        }
    }

    public void EditNeighbourChunkAt(MarchingCubeChunk chunk, Vector3Int original, Vector3Int offset, float delta)
    {
        Vector3Int newChunkCubeIndex = (original + offset).Map(f => MathExt.FloorMod(f, VoxelsPerChunkAxis));
        MarchingCubeEntity e = chunk.GetEntityAt(newChunkCubeIndex.x, newChunkCubeIndex.y, newChunkCubeIndex.z);
        chunk.EditPointsNextToChunk(chunk, e, offset, delta);
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

}

