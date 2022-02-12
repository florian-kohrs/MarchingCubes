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
        public ComputeShader buildTrisShader;

        public ComputeBuffer triCountBuffer;
        public ComputeBuffer pointsBuffer;
        public ComputeBuffer savedPointsBuffer;
        public ComputeBuffer preparedTrisBuffer;

        public void ApplyStaticProperties()
        {
            ApplyDensityProperties();
            ApplyPrepareTrisProperties();
            ApplyTriBuilderProperties();
        }

        public ComputeBuffer ApplyTriangleBuffer(int numPoints)
        {
            ComputeBuffer triangles = new ComputeBuffer(numPoints, TriangleBuilder.SIZE_OF_TRI_BUILD);
            buildTrisShader.SetBuffer(0, "triangles", triangles);
            return triangles;
        }

        public void ApplyDensityPropertiesForChunk(ICompressedMarchingCubeChunk chunk, bool tryLoadData = true)
        {
            Vector4 anchor = VectorExtension.RaiseVector3Int(chunk.AnchorPos);
            int pointsPerAxis = chunk.PointsPerAxis;

            densityGeneratorShader.SetVector("anchor", anchor);
            densityGeneratorShader.SetVector("offset", offset);
            densityGeneratorShader.SetInt("numPointsPerAxis", pointsPerAxis);
            densityGeneratorShader.SetFloat("spacing", chunk.LOD);
            densityGeneratorShader.SetBool("tryLoadData", tryLoadData);
        }

        public void ApplyPrepareTrianglesForChunk(ICompressedMarchingCubeChunk chunk)
        {
            prepareTrisShader.SetInt("numPointsPerAxis", chunk.PointsPerAxis);
            preparedTrisBuffer.SetCounterValue(0);
        }

        public void ApplyBuildTrianglesForChunkProperties(ICompressedMarchingCubeChunk chunk, int numTris)
        {
            Vector4 anchor = VectorExtension.RaiseVector3Int(chunk.AnchorPos);
            int pointsPerAxis = chunk.PointsPerAxis;

            bool storeMinDegree = chunk.MinDegreeBuffer != null;

            //TODO: Apply triangle buffer.

            buildTrisShader.SetVector("anchor", anchor);
            buildTrisShader.SetInt("numPointsPerAxis", pointsPerAxis);
            buildTrisShader.SetInt("spacing", chunk.LOD);
            buildTrisShader.SetInt("length", numTris);
            buildTrisShader.SetBool("storeMinDegrees", storeMinDegree);

            if (storeMinDegree)
            {
                buildTrisShader.SetBuffer(0, "minDegreeAtCoord", chunk.MinDegreeBuffer);
            }
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

        protected void ApplyTriBuilderProperties()
        {
            buildTrisShader.SetBuffer(0, "points", pointsBuffer);
            buildTrisShader.SetBuffer(0, "biomsViz", biomsVizBuffer);
            buildTrisShader.SetBuffer(0, "triangleLocations", preparedTrisBuffer);

            buildTrisShader.SetInt("minSteepness", minDegree);
            buildTrisShader.SetInt("maxSteepness", maxDegree);
            ApplyBiomPropertiesToShader(buildTrisShader);
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