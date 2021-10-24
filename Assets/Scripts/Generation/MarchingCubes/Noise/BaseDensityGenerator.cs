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

        public Biom[] bioms;

        public int biomSize = 500;

        public int biomSpacing = 1;

        public float radius = 1000;

        public int octaves = 9;

        public Vector3 offset;

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
            SetBiomData();
            densityShader.SetInt("octaves", Mathf.Max(1, octaves));
            densityShader.SetFloat("radius", radius);
            densityShader.SetBuffer(0, "octaveOffsets", octaveOffsetsBuffer);
            densityShader.SetVector("offset", new Vector4(offset.x, offset.y, offset.z));
        }

        private void OnDestroy()
        {
            octaveOffsetsBuffer.Release();
            biomsBuffer.Release();
            octaveOffsetsBuffer = null;
        }

        protected void ApplyShaderProperties(int numPointsPerAxis, Vector3 anchor, float spacing)
        {
            densityShader.SetInt("numPointsPerAxis", numPointsPerAxis);
            densityShader.SetFloat("spacing", spacing);
            densityShader.SetVector("anchor", new Vector4(anchor.x, anchor.y, anchor.z));
        }

        protected ComputeBuffer octaveOffsetsBuffer;

        protected ComputeBuffer biomsBuffer;

        protected void SetBiomData()
        {
            if (biomsBuffer != null)
                return;

            biomsBuffer = new ComputeBuffer(bioms.Length, Biom.SIZE);
            biomsBuffer.SetData(bioms);
            densityShader.SetBuffer(0, "bioms", biomsBuffer);
            densityShader.SetInt("biomSize", biomSize);
            densityShader.SetInt("biomSpacing", biomSpacing);
            densityShader.SetInt("biomsCount", bioms.Length);
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