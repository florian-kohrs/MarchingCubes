using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarchingCubeChunkHandler : MonoBehaviour
{

    public const int CHUNK_SIZE = 8;

    public Dictionary<Vector3Int, MarchingCubeChunk> chunks = new Dictionary<Vector3Int, MarchingCubeChunk>();

    public int blockAroundPlayer = 25;

    public PlanetMarchingCubeNoise noiseFilter;

    public Vector3 offset;

    [Range(2,8)]
    public int chunkSize = 2;

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


    private void Start()
    {
        for (int x = -chunkSize / 2; x < chunkSize / 2 + 1; x++)
        {
            for (int y = -chunkSize / 2; y < chunkSize / 2 + 1; y++)
            {
                for (int z = -chunkSize / 2; z < chunkSize / 2 + 1; z++)
                {
                    MarchingCubeChunk c;
                    if (chunks.TryGetValue(new Vector3Int(x, y, z), out c))
                    {
                        UpdateChunk(new Vector3Int(x,y,z), c);
                    }
                    else 
                    {
                        CreateChunkAt(new Vector3Int(x, y, z));
                    } 
                }
            }
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
        UpdateChunk(p, chunk);
    }

    protected void UpdateChunk(Vector3Int p,MarchingCubeChunk chunk)
    {
        chunk.Initialize(chunkMaterial, surfaceLevel, p, offset, noiseFilter, this);
    }

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
                    int offset = Mathf.CeilToInt((p[i] / (CHUNK_SIZE - 2f)) - 1);
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
        Vector3Int newChunkCubeIndex = (original + offset).Map(f => MathExt.FloorMod(f, CHUNK_SIZE));
        MarchingCubeEntity e = chunk.CubeEntities[newChunkCubeIndex.x, newChunkCubeIndex.y, newChunkCubeIndex.z];
        chunk.EditPointsNextToChunk(chunk, e, offset, delta);
    }

}
