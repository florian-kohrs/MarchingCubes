using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface INoiseBuilder
    {
        void BuildNoiseArea(Vector4[] points, Vector3Int chunkOffset, int chunkSize, Vector3 noiseOffset, int size, Func<Vector3Int, int> CoordToIndex);

        //float Evaluate(Vector3 p, float noiseScale, Vector3 offset);

    }
}