using MarchingCubes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkGenerationGPUData : IDisposable
    {

        #region static shader properties

        public static int minDegree;
        public static int maxDegree;
        public static int octaves;

        public static float radius;

        public static int biomsCount;
        public static int biomSpacing;
        public static int biomSize;

        public static Vector3 offset;

        public static ComputeBuffer octaveOffsetsBuffer;
        public static ComputeBuffer biomsVizBuffer;
        public static ComputeBuffer biomBuffer;


        #endregion

        public ComputeShader densityGeneratorShader;
        public ComputeShader prepareTrisShader;

        public ComputeShader buildMeshDataShader;

        public ComputeBuffer triCountBuffer;
        public ComputeBuffer pointsBuffer;
        //TODO: Pool theese seperatly somewhere else
        public ComputeBuffer savedPointsBuffer;
        public ComputeBuffer preparedTrisBuffer;

        public void ApplyStaticProperties()
        {
            ApplyDensityProperties();
            ApplyPrepareTrisProperties();
            ApplyTriBuilderProperties(buildMeshDataShader);
        }


        public void ApplyDensityPropertiesForChunk(CompressedMarchingCubeChunk chunk, bool tryLoadData = true)
        {
            Vector4 anchor = VectorExtension.ToVector4(chunk.AnchorPos);
            int pointsPerAxis = chunk.PointsPerAxis;

            densityGeneratorShader.SetVector("anchor", anchor);
            densityGeneratorShader.SetVector("offset", offset);
            densityGeneratorShader.SetInt("numPointsPerAxis", pointsPerAxis);
            densityGeneratorShader.SetFloat("spacing", chunk.LOD);
            densityGeneratorShader.SetBool("tryLoadData", tryLoadData);
        }

        public void ApplyPrepareTrianglesForChunk(CompressedMarchingCubeChunk chunk)
        {
            int pointSpacing = (int)Mathf.Pow(2,chunk.LODPower - (chunk.ChunkSizePower - MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER));
            //prepareTrisShader.SetInt("pointSpacing", chunk.PointsPerAxis);

            prepareTrisShader.SetInt("numPointsPerAxis", chunk.PointsPerAxis);
            preparedTrisBuffer.SetCounterValue(0);
        }

        public void ApplyBuildMeshDataPropertiesForChunk(CompressedMarchingCubeChunk chunk, int numTris, out ComputeBuffer verts, out ComputeBuffer colors)
        {
            verts = new ComputeBuffer(numTris * 3, sizeof(float) * 3);
            colors = new ComputeBuffer(numTris * 3, sizeof(uint));

            buildMeshDataShader.SetBuffer(0, "verts", verts);
            buildMeshDataShader.SetBuffer(0, "colors", colors);

            ApplyBuildGenericTrianglesForChunkProperties(buildMeshDataShader, chunk, numTris);
        }

        protected void ApplyBuildGenericTrianglesForChunkProperties(ComputeShader forShader, CompressedMarchingCubeChunk chunk, int numTris)
        {

            bool storeMinDegree = chunk.MinDegreeBuffer != null;

            forShader.SetBool("storeMinDegrees", storeMinDegree);

            if (storeMinDegree)
            {
                forShader.SetBuffer(0, "minDegreeAtCoord", chunk.MinDegreeBuffer);
            }
            else
            {
                forShader.SetBuffer(0, "minDegreeAtCoord", ChunkGPUDataRequest.emptyMinDegreeBuffer);
            }

            Vector4 anchor = VectorExtension.ToVector4(chunk.AnchorPos);
            int pointsPerAxis = chunk.PointsPerAxis;

            forShader.SetVector("anchor", anchor);
            forShader.SetFloat("spacing", chunk.LOD);
            forShader.SetInt("numPointsPerAxis", pointsPerAxis);
            forShader.SetInt("length", numTris);

        }


        protected void ApplyDensityProperties()
        {
            densityGeneratorShader.SetBuffer(0, "points", pointsBuffer);
            densityGeneratorShader.SetBuffer(0, "savedPoints", savedPointsBuffer);
            densityGeneratorShader.SetBuffer(0, "octaveOffsets", octaveOffsetsBuffer);
            densityGeneratorShader.SetInt("octaves", octaves);
            ApplyBiomPropertiesToShader(densityGeneratorShader);
        }

        protected void ApplyPrepareTrisProperties()
        {
            prepareTrisShader.SetBuffer(0, "points", pointsBuffer);
            prepareTrisShader.SetBuffer(0, "triangleLocations", preparedTrisBuffer);
        }

        protected void ApplyTriBuilderProperties(ComputeShader s)
        {
            s.SetBuffer(0, "points", pointsBuffer);
            s.SetBuffer(0, "biomsViz", biomsVizBuffer);
            s.SetBuffer(0, "triangleLocations", preparedTrisBuffer);

            s.SetInt("minSteepness", minDegree);
            s.SetInt("maxSteepness", maxDegree);
            ApplyBiomPropertiesToShader(s);
        }


        protected void ApplyBiomPropertiesToShader(ComputeShader s)
        {
            s.SetBuffer(0, "bioms", biomBuffer);
            s.SetFloat("radius", radius);
            s.SetInt("biomsCount", biomsCount);
            s.SetInt("biomSize", biomSize);
            s.SetInt("biomSpacing", biomSpacing);
        }

        public void Dispose()
        {
            triCountBuffer.Dispose();
            pointsBuffer.Dispose();
            preparedTrisBuffer.Dispose();
            savedPointsBuffer.Dispose();
        }


    }
}