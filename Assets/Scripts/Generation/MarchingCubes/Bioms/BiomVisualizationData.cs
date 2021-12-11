using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public struct BiomColor
    {
        public BiomColor(BiomVisualizationData data)
        {
            steepR = (uint)(data.steepBiomColor.r * 255);    
            steepG = (uint)(data.steepBiomColor.g * 255);
            steepB = (uint)(data.steepBiomColor.b * 255);

            flatR = (uint)(data.flatBiomColor.r * 255);
            flatG = (uint)(data.flatBiomColor.g * 255);
            flatB = (uint)(data.flatBiomColor.b * 255);
        }

        public uint steepR;
        public uint steepG;
        public uint steepB;

        public uint flatR;
        public uint flatG;
        public uint flatB;

        public const int SIZE = sizeof(uint) * 3 * 2;
    }

    [System.Serializable]
    public struct BiomVisualizationData
    {

        public BiomVisualizationData(Color steep, Color flat)
        {
            steepBiomColor = steep;
            flatBiomColor = flat;
        }

        public Color steepBiomColor;

        public Color flatBiomColor;

        public const int SIZE = sizeof(float) * 4 * 2; 

    }
}