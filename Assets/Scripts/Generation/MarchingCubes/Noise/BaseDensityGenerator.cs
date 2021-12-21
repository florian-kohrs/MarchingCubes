using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class BaseDensityGenerator : MonoBehaviour
    {

        protected const int threadGroupSize = 4;

        public ComputeShader densityShader;

        protected List<ComputeBuffer> buffersToRelease = new List<ComputeBuffer>();

        protected BiomNoiseData[] bioms;

        public int biomSize = 500;

        public int biomSpacing = 1;

        public float radius = 1000;

        public int octaves = 9;

        public Vector3 offset;

        public int seed;

        public void SetBioms(BiomNoiseData[] bioms, params ComputeShader[] shaders)
        {
            this.bioms = bioms;
            SetBiomData(shaders);
        }

        public virtual void Generate(int numPointsPerAxis, Vector3 anchor, float spacing, bool tryLoad = false)
        {
            ApplyShaderProperties(numPointsPerAxis, anchor, spacing, tryLoad);

            int numThreadsPerAxis = Mathf.CeilToInt(numPointsPerAxis / (float)threadGroupSize);

            densityShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        }

        public void SetBuffer(ComputeBuffer pointsBuffer, ComputeBuffer savedPointBuffer, ComputeBuffer pointClosestBiomBuffer)
        {
            densityShader.SetBuffer(0, "points", pointsBuffer);
            densityShader.SetBuffer(0, "savedPoints", savedPointBuffer);
            densityShader.SetBuffer(0, "pointBiomIndex", pointClosestBiomBuffer);
        }

        private void Awake()
        {
            GetOctaveOffsetsBuffer();
            densityShader.SetInt("octaves", Mathf.Max(1, octaves));
            densityShader.SetBuffer(0, "octaveOffsets", octaveOffsetsBuffer);
            densityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        }

        private void OnDestroy()
        {
            octaveOffsetsBuffer.Release();
            biomsBuffer.Release();
            octaveOffsetsBuffer = null;
        }

        protected void ApplyShaderProperties(int numPointsPerAxis, Vector3 anchor, float spacing, bool tryLoad)
        {
            densityShader.SetBool("tryLoadData", tryLoad);
            densityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
            densityShader.SetFloat("spacing", spacing);
            densityShader.SetVector("anchor", new Vector4(anchor.x, anchor.y, anchor.z));
        }

        protected ComputeBuffer octaveOffsetsBuffer;

        protected ComputeBuffer biomsBuffer;

        protected void SetBiomData(params ComputeShader[] shaders)
        {
            if (biomsBuffer != null)
                return;

            biomsBuffer = new ComputeBuffer(bioms.Length, BiomNoiseData.SIZE);
            biomsBuffer.SetData(bioms);
            SetShaderBiomProperties(densityShader);

            for (int i = 0; i < shaders.Length; i++)
            {
                SetShaderBiomProperties(shaders[i]);
            }
        }

        protected void SetShaderBiomProperties(ComputeShader s)
        {
            s.SetBuffer(0, "bioms", biomsBuffer);
            s.SetInt("biomSize", biomSize);
            s.SetInt("biomSpacing", biomSpacing);
            s.SetInt("biomsCount", bioms.Length);

            s.SetFloat("radius", radius);
        }

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