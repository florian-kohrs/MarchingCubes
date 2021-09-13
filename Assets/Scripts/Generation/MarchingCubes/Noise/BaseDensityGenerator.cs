using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class BaseDensityGenerator : MonoBehaviour
    {

        protected const int threadGroupSize = 8;

        public ComputeShader densityShader;

        protected List<ComputeBuffer> buffersToRelease = new List<ComputeBuffer>();

        public float amplitude = 2;

        public float frequency = 0.75f;

        [Range(0.001f, 100)]
        public float scale = 1;

        public float radius = 1000;

        public int octaves = 9;

        public Vector3 offset;

        [Range(0, 1)]
        public float persistence = 0.5f;

        public float lacunarity = 3.41f;

        public int seed;


        public virtual void TestGenerateAt(ComputeBuffer pointsBuffer, ComputeBuffer pointsPosBuffer, Vector3 pos, float f1, float f2)
        {
                ApplyShaderProperties(pointsBuffer, pointsPosBuffer, 4, 1f, pos, 1f);

                int numThreadsPerAxis = Mathf.CeilToInt(4 / (float)threadGroupSize);

                densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
                var arr = new float[4*4*4];
                pointsBuffer.GetData(arr);
                if (buffersToRelease != null)
                {
                    foreach (var b in buffersToRelease)
                    {
                        b.Release();
                    }
                    buffersToRelease.Clear();
                }
            }
        

        public virtual void TestGenerate(ComputeBuffer pointsBuffer, ComputeBuffer pointsPosBuffer)
        {
            List<float[]> points = new List<float[]>();
            int num = 129;
            int num2 = 128;
            for (int i = 0; i < 2; i++)
            {
                ApplyShaderProperties(pointsBuffer, pointsPosBuffer, num, 1, new Vector3(i * num2, 65008,0), 1);

                int numThreadsPerAxis = Mathf.CeilToInt(num / (float)threadGroupSize);

                densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
                var arr = new float[num * num * num];
                pointsBuffer.GetData(arr);
                points.Add(arr);
                if (buffersToRelease != null)
                {
                    foreach (var b in buffersToRelease)
                    {
                        b.Release();
                    }
                    buffersToRelease.Clear();
                }
            }

            for (int x = 0; x < num; x++)
            {
                for (int y = 0; y < num; y++)
                {
                    float n1 = 0, n2 = 0;
                   
                    Vector3Int ind1 = new Vector3Int(num2, x, y);
                    Vector3Int ind2 = new Vector3Int(0, x, y);
                    int i1 = ind1.z * num * num + ind1.y * num + ind1.x;
                    int i2 = ind2.z * num * num + ind2.y * num + ind2.x;
                    n1 = points[0][i1];
                    n2 = points[1][i2];

                    Debug.Log("Diff:" + Mathf.Abs(n1 - n2));
                }
            }
            
    }


        public virtual ComputeBuffer Generate(ComputeBuffer pointsBuffer, ComputeBuffer pointsPosBuffer, int numPointsPerAxis, float boundsSize, Vector3 anchor, float spacing)
        {
            ApplyShaderProperties(pointsBuffer, pointsPosBuffer, numPointsPerAxis, boundsSize, anchor, spacing);

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


        protected void ApplyShaderProperties(ComputeBuffer pointsBuffer, ComputeBuffer pointsPosBuffer, int numPointsPerAxis, float boundsSize, Vector3 anchor, float spacing)
        {
            ComputeBuffer octaveOffsetsBuffer = GetOctaveOffsetsBuffer();

            densityShader.SetBuffer(0, "points", pointsBuffer);
            densityShader.SetBuffer(0, "pointsPos", pointsPosBuffer);
            densityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
            densityShader.SetFloat("boundsSize", boundsSize);
            densityShader.SetFloat("spacing", spacing);
            densityShader.SetVector("anchor", new Vector4(anchor.x, anchor.y, anchor.z));
            densityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));

            densityShader.SetInt("octaves", Mathf.Max(1, octaves));
            densityShader.SetFloat("radius", radius);
            densityShader.SetFloat("lacunarity", lacunarity);
            densityShader.SetFloat("persistence", persistence);
            densityShader.SetFloat("noiseScale", frequency);
            densityShader.SetFloat("scale", scale);
            densityShader.SetBuffer(0, "octaveOffsets", octaveOffsetsBuffer);
            densityShader.SetFloat("amplitude", amplitude);

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
}