using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// check this for better noise: https://developer.nvidia.com/gpugems/gpugems3/part-i-geometry/chapter-1-generating-complex-procedural-terrains-using-gpu
/// </summary>

[System.Serializable]
public class TerrainNoise : INoiseBuilder
{

    public float amplitude = 2;

    public float frequency = 1;

    [Range(0.001f, 100)]
    public float scale = 1;

    public int octaves = 4;

    [Range(0, 1)]
    public float persistance = 0.5f;

    public float lacunarity = 2;

    public int seed;

    public Vector3 terrainOffset;

    protected Vector2 terrrain2DOffset => new Vector2(terrainOffset.x, terrainOffset.z);

    protected virtual void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
    }

    protected Vector2[] octaveOffsets;

    protected Vector2[] OvtavesOffsets
    {
        get
        {
            if(octaveOffsets == null)
            {
                octaveOffsets = new Vector2[octaves];
                System.Random terrainRNG = new System.Random(seed);

                for (int i = 0; i < octaves; i++)
                {
                    float offsetX = terrainRNG.Next(-100000, 100000) + terrrain2DOffset.x;
                    float offsetZ = terrainRNG.Next(-100000, 100000) + terrrain2DOffset.y;
                    octaveOffsets[i] = new Vector2(offsetX, offsetZ);
                }
            }

            return octaveOffsets;
        }
    }

    public void BuildNoiseAreaUnderWater(Vector4[] points, Vector3Int chunkOffset, Vector3 noiseOffset, int size, Func<Vector3Int, int> CoordToIndex)
    {
        chunkOffset = chunkOffset * MarchingCubeChunkHandler.CHUNK_SIZE;
        Vector3Int v = new Vector3Int();
        int shift = Mathf.FloorToInt((size + 1) / 2);
        Vector3Int vShift = new Vector3Int(shift, shift, shift);
        for (int x = 0; x < size + 1; x++)
        {
            v.x = x;
            for (int y = 0; y < size + 1; y++)
            {
                v.y = y;
                for (int z = 0; z < size + 1; z++)
                {
                    v.z = z;
                    Vector3 offsetV = (v - vShift) + chunkOffset + noiseOffset;


                    float density = -offsetV.y;

                    float amplitude = this.amplitude;
                    float frequency = this.frequency;

                    for (int i = 0; i < octaves; i++)
                    {
                        Vector3 samplePos = offsetV * frequency;

                        float currentDensity = Evaluate(samplePos);
                        currentDensity *= amplitude;
                        density += currentDensity;

                        amplitude *= 0.5f;
                        frequency *= 1.937f;
                    }

                    points[CoordToIndex(v)] = new Vector4(offsetV.x, offsetV.y, offsetV.z, -density);
                }
            }
        }
    }

    public void BuildNoiseAreaLand(Vector4[] points, Vector3Int chunkOffset, Vector3 noiseOffset, int size, Func<Vector3Int, int> CoordToIndex)
    {
        chunkOffset = chunkOffset * MarchingCubeChunkHandler.CHUNK_SIZE;
        Vector3Int v = new Vector3Int();
        int shift = Mathf.FloorToInt((size + 1) / 2);
        Vector3Int vShift = new Vector3Int(shift, shift, shift);
        for (int x = 0; x < size + 1; x++)
        {
            v.x = x;
            for (int y = 0; y < size + 1; y++)
            {
                v.y = y;
                for (int z = 0; z < size + 1; z++)
                {
                    v.z = z;
                    Vector3 offsetV = (v - vShift) + chunkOffset;

                    ///closes on top
                    float height = 30;
                    //float density = -(height + (-Mathf.Abs(offsetV.y - height))); 

                    ///buildsPlanet
                    float radius = 10000;
                    float density = radius - offsetV.magnitude;

                    ///normal floor
                    //float density = -offsetV.y;

                    float amplitude = this.amplitude;
                    float frequency = this.frequency;

                    for (int i = 0; i < octaves; i++)
                    {
                        Vector3 samplePos = offsetV / scale * frequency;

                        float currentDensity = Evaluate(samplePos);
                        currentDensity *= amplitude;
                        density += currentDensity;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    points[CoordToIndex(v)] = new Vector4(offsetV.x, offsetV.y, offsetV.z, -density);
                }
            }
        }
    }

    public float Evaluate(Vector3 p)
    {
        return Mathf.Abs(SimplexNoise.SimplexNoise.Generate(p.x, p.y, p.z));
    }

    public void BuildNoiseArea(Vector4[] points, Vector3Int chunkOffset, Vector3 noiseOffset, int size, Func<Vector3Int, int> CoordToIndex)
    {
        BuildNoiseAreaLand(points, chunkOffset, noiseOffset, size, CoordToIndex);
    }

}
