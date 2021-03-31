using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    [System.Serializable]
    public class PlanetMarchingCubeNoise : INoiseBuilder
    {

        public int seed;

        public float radius = 8;

        public float noiseScale = 0.1f;

        public void BuildNoiseArea(Vector4[] points, Vector3Int chunkOffset, Vector3 noiseOffset, int size, Func<Vector3Int, int> CoordToIndex)
        {
            chunkOffset = chunkOffset * MarchingCubeChunkHandler.VoxelsPerChunkAxis;
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
                        Vector3Int offsetV = (v - vShift) + chunkOffset;
                        points[CoordToIndex(v)] = new Vector4(offsetV.x, offsetV.y, offsetV.z, Evaluate(new Vector3(offsetV.x, offsetV.y, offsetV.z), noiseScale, noiseOffset));
                    }
                }
            }
        }
        public static MinMax m = new MinMax();

        public float Evaluate(Vector3 p, float noiseScale, Vector3 offset)
        {
            Vector3 noisePos = p * noiseScale + offset;

            float distance = p.magnitude;
            float progress = Mathf.Clamp01(distance / radius);

            float noise = SimplexNoise.SimplexNoise.CalcPixel3D(noisePos.x, noisePos.y, noisePos.z);
            noise = noise * 2 - 1;
            m.AddValue(noise);
            return progress + noise / 4;

            float radiusProgress = distance / radius;




            //float extra = Mathf.Cos(Mathf.PI * radiusProgress / 2);
            //noise = (noise + extra / 5) * (extra + 1);

            return noise;
        }

    }
}
