using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    public class NoiseData : MonoBehaviour
    {

        protected const int threadGroupSize = 4;

        protected ComputeBuffer octaveOffsetsBuffer;

        protected ComputeBuffer biomsBuffer;

        protected ComputeBuffer biomsColorBuffer;

        protected ComputeBuffer biomEnvironmentData;

        protected BiomNoiseData[] bioms;

        protected BiomEnvirenmentData[] envirenmentBioms;

        protected BiomColor[] biomColorData;

        public BiomScriptableObject[] biomsScriptable;


        public int biomSize = 500;

        public int biomSpacing = 1;

        public float radius = 1000;

        [Range(1,11)]
        public int octaves = 9;

        public Vector3 offset;

        public int seed;

        public void SetBioms()
        {
            envirenmentBioms = biomsScriptable.Select(b => b.envirenmentData).ToArray();
            bioms = biomsScriptable.Select(b => b.biom).ToArray();
            biomColorData = biomsScriptable.Select(b => new BiomColor(b.visualizationData)).ToArray();
            
            SetBiomData();

            ApplyStaticBiomDataToPipeline();
        }

        protected void ApplyStaticBiomDataToPipeline()
        {
            ChunkGenerationGPUData.biomsVizBuffer = biomsColorBuffer;
            ChunkGenerationGPUData.octaveOffsetsBuffer = octaveOffsetsBuffer;
            ChunkGenerationGPUData.biomBuffer = biomsBuffer;
            //ChunkGenerationPipeline.buf = octaveOffsetsBuffer;

            ChunkGenerationGPUData.octaves = octaves;
            ChunkGenerationGPUData.radius = radius;
            ChunkGenerationGPUData.biomsCount = bioms.Length;
            ChunkGenerationGPUData.biomSpacing = biomSpacing;
            ChunkGenerationGPUData.biomSize = biomSize;

        }

        public void ApplyNoiseBiomData()
        { 
            GetOctaveOffsetsBuffer();
            SetBioms();
        }

        private void OnDestroy()
        {
            octaveOffsetsBuffer.Dispose();
            biomsBuffer.Dispose();
            biomEnvironmentData.Dispose();
            octaveOffsetsBuffer = null;
        }

        protected void SetBiomData()
        {
            if (biomsBuffer != null)
                return;

            biomsBuffer = new ComputeBuffer(bioms.Length, BiomNoiseData.SIZE);
            biomsBuffer.SetData(bioms);

            biomsColorBuffer = new ComputeBuffer(bioms.Length, BiomColor.SIZE);
            biomsColorBuffer.SetData(biomColorData);

            biomEnvironmentData = new ComputeBuffer(bioms.Length, BiomEnvirenmentData.SIZE);
            biomEnvironmentData.SetData(envirenmentBioms);
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