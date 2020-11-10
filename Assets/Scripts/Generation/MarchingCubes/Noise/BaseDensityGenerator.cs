using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseDensityGenerator : MonoBehaviour
{

    protected const int threadGroupSize = 8;

    public ComputeShader densityShader;

    protected List<ComputeBuffer> buffersToRelease = new List<ComputeBuffer>();

    public float amplitude = 2;

    public float frequency = 0.75f;

    [Range(0.001f, 100)]
    public float scale = 1;

    public int octaves = 9;

    [Range(0, 1)]
    public float persistence = 0.5f;

    public float lacunarity = 3.41f;

    public int seed;

    public virtual ComputeBuffer Generate(ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 centre, Vector3 offset, float spacing)
    {
        ApplyShaderProperties(pointsBuffer, numPointsPerAxis, boundsSize, centre, offset, spacing);

        int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);
        
        densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        if (buffersToRelease != null)
        {
            foreach (var b in buffersToRelease)
            {
                b.Release();
            }
            buffersToRelease.Clear();
        }

        // Return voxel data buffer so it can be used to generate mesh
        return pointsBuffer;
    }


    protected void ApplyShaderProperties(ComputeBuffer pointsBuffer, int numPointsPerAxis, float boundsSize, Vector3 centre, Vector3 offset, float spacing)
    {
        ComputeBuffer octaveOffsetsBuffer = GetOctaveOffsetsBuffer();

        densityShader.SetBuffer(0, "points", pointsBuffer);
        densityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
        densityShader.SetFloat("boundsSize", boundsSize);
        densityShader.SetVector("centre", new Vector4(centre.x, centre.y, centre.z));
        densityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));

        densityShader.SetInt("octaves", Mathf.Max(1, octaves));
        densityShader.SetFloat("lacunarity", lacunarity);
        densityShader.SetFloat("persistence", persistence);
        densityShader.SetFloat("noiseScale", frequency);
        densityShader.SetBuffer(0, "octaveOffsets", octaveOffsetsBuffer);
        densityShader.SetFloat("amplitude", amplitude);
        densityShader.SetFloat("spacing", spacing);

        //buffersToRelease.Add(pointsBuffer);
    }


    protected ComputeBuffer GetOctaveOffsetsBuffer()
    {
        System.Random r = new System.Random(seed);

        Vector3[] offsets = new Vector3[octaves];

        float offsetRange = 1000;

        for (int i = 0; i < octaves; i++)
        {
            offsets[i] = new Vector3(
                (float)r.NextDouble() * 2 - 1, 
                (float)r.NextDouble() * 2 - 1, 
                (float)r.NextDouble() * 2 - 1) * offsetRange;
        }

        ComputeBuffer offsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
        offsetsBuffer.SetData(offsets);
        buffersToRelease.Add(offsetsBuffer);

        return offsetsBuffer;
    }

}
