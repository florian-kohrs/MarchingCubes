using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubeChunkHandler : MonoBehaviour
{
   
    protected int kernelId;

    protected const int threadGroupSize = 8;

    public const int VoxelsPerChunkAxis = 12;
    
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

    public TerrainNoise terrainNoise;

    public BaseDensityGenerator densityGenerator;

    public bool useTerrainNoise;

    public Vector3 offset;

    public int deactivateAfterDistance = 40;

    protected int DeactivatedChunkDistance => Mathf.CeilToInt(deactivateAfterDistance / PointsPerChunkAxis);

    public Material chunkMaterial;

    [Range(0,1)]
    public float surfaceLevel = 0.45f;

    private void OnValidate()
    {

        //if(transform.childCount > chunks.Count)
        //{
        //    foreach (Transform t in transform)
        //    {
        //        if(t != null)
        //        {
        //            Destroy(t.gameObject);
        //        }
        //    }
        //    chunks.Clear();
        //}

        //if(chunks.Count > 0 && chunks.Values.ToList()[0] == null)
        //{
        //    chunks.Clear();
        //}
        //if(chunkSize % 2 == 1)
        //{
        //    chunkSize += 1;
        //}
        //Start();
    }

    public Transform player;

    private void Start()
    {
        kernelId = marshShader.FindKernel("March");
        CheckChunksAround(player.position);
        //UpdateChunks();
        //StartCoroutine(UpdateChunks());
    }

    private void Update()
    {
        CheckChunksAround(player.position);
    }

    protected IEnumerator UpdateChunks()
    {
        yield return null;
       

        //yield return new WaitForSeconds(3);

        yield return UpdateChunks();
    }

    public void CheckChunksAround(Vector3 v)
    {
        CreateBuffers();

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

    protected void CreateChunkAt(Vector3Int p)
    {
        GameObject g = new GameObject("Chunk" + "(" + p.x + "," + p.y + "," + p.z + ")");
        g.transform.SetParent(transform,false);
        //g.transform.position = p * CHUNK_SIZE;

        MarchingCubeChunk chunk = g.AddComponent<MarchingCubeChunk>();
        chunks.Add(p, chunk);
        chunk.chunkOffset = p;
        BuildChunk(p, chunk);
        ConnectNeighboursAround(chunk);
    }

    protected void ConnectNeighboursAround(MarchingCubeChunk chunk)
    {
        foreach (Vector3Int v3 in GetNeighbourPositionsOf(chunk))
        {
            MarchingCubeChunk neighbour;
            if (chunks.TryGetValue(v3, out neighbour))
            {
                chunk.ConnectWithNeighbour(neighbour);
            }
        }
    }

    protected IEnumerable<Vector3Int> GetNeighbourPositionsOf(MarchingCubeChunk chunk)
    {
        Vector3Int r = chunk.chunkOffset;
        yield return new Vector3Int(r.x - 1, r.y, r.z);
        yield return new Vector3Int(r.x + 1, r.y, r.z);
        yield return new Vector3Int(r.x, r.y - 1, r.z);
        yield return new Vector3Int(r.x, r.y + 1, r.z);
        yield return new Vector3Int(r.x, r.y, r.z - 1);
        yield return new Vector3Int(r.x, r.y, r.z + 1);
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

        densityGenerator.Generate(pointsBuffer, PointsPerChunkAxis, 0, CenterFromChunkIndex(p), offset, 1);

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
        Triangle[] tris = new Triangle[numTris];
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
        return v.Map(i => i * VoxelsPerChunkAxis);
    }

    protected float PointSpacing => 1;

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
        MarchingCubeEntity e = chunk.CubeEntities[newChunkCubeIndex.x, newChunkCubeIndex.y, newChunkCubeIndex.z];
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
