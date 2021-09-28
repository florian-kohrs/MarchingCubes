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

        public virtual void Generate(int numPointsPerAxis, Vector3 anchor, float spacing)
        {
            ApplyShaderProperties(numPointsPerAxis, anchor, spacing);

            int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);

            densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        }

        public void SetPointsBuffer(ComputeBuffer pointsBuffer)
        {
            densityShader.SetBuffer(0, "points", pointsBuffer);
        }

        private void Awake()
        {
            GetOctaveOffsetsBuffer();
            densityShader.SetInt("octaves", Mathf.Max(1, octaves));
            densityShader.SetFloat("radius", radius);
            densityShader.SetFloat("lacunarity", lacunarity);
            densityShader.SetFloat("persistence", persistence);
            densityShader.SetFloat("noiseScale", frequency);
            densityShader.SetFloat("scale", scale);
            densityShader.SetBuffer(0, "octaveOffsets", octaveOffsetsBuffer);
            densityShader.SetFloat("amplitude", amplitude);
            densityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        }

        private void OnDestroy()
        {
            octaveOffsetsBuffer.Release();
            octaveOffsetsBuffer = null;
        }

        protected void ApplyShaderProperties(int numPointsPerAxis, Vector3 anchor, float spacing)
        {
            densityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
            densityShader.SetFloat("spacing", spacing);
            densityShader.SetVector("anchor", new Vector4(anchor.x, anchor.y, anchor.z));
        }

        protected ComputeBuffer octaveOffsetsBuffer;

        protected ComputeBuffer GetOctaveOffsetsBuffer()
        {
            if (octaveOffsetsBuffer != null)
                return octaveOffsetsBuffer;

            System.Random r = new System.Random(seed);

            Vector3[] offsets = new Vector3[octaves];

            float offsetRange = 1000;

            for (int i = 0; i < octaves; ++i)
            {
                offsets[i] = new Vector3(
                    (float)r.NextDouble() * 2 - 1,
                    (float)r.NextDouble() * 2 - 1,
                    (float)r.NextDouble() * 2 - 1) * offsetRange;
            }

            octaveOffsetsBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
            octaveOffsetsBuffer.SetData(offsets);

            return octaveOffsetsBuffer;
        }

    }
}