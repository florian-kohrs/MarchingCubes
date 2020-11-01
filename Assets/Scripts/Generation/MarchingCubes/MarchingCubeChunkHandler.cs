using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubeChunkHandler : MonoBehaviour
{


    public const int VoxelsPerChunkAxis = 8;

    public int PointsPerChunkAxis => VoxelsPerChunkAxis + 1;

    public Dictionary<Vector3Int, MarchingCubeChunk> chunks = new Dictionary<Vector3Int, MarchingCubeChunk>();

    public int blockAroundPlayer = 16;


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

    [Range(2,8)]
    public int chunkSize = 2;

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

    private ComputeBuffer pointsBuffer;

    protected void BuildChunk(Vector3Int p, MarchingCubeChunk chunk)
    {
        pointsBuffer = new ComputeBuffer(PointsPerChunkAxis * PointsPerChunkAxis * PointsPerChunkAxis, sizeof(float) * 4);
        
        //float pointSpacing = boundsSize / VoxelsPerChunkAxis;

        densityGenerator.Generate(pointsBuffer, PointsPerChunkAxis, VoxelsPerChunkAxis, CenterFromChunkIndex(p), offset, 1);

        chunk.Initialize(pointsBuffer, chunkMaterial, surfaceLevel, p, offset, this);
       
        //if (useTerrainNoise)
        //{
        //    chunk.Initialize(chunkMaterial, surfaceLevel, p, offset, terrainNoise, this);
        //}
        //else
        //{
        //    chunk.Initialize(chunkMaterial, surfaceLevel, p, offset, noiseFilter, this);
        //}
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

}
