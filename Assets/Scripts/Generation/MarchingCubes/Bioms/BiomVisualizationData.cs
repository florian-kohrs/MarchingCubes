using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

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

        public BiomVisualizationData ScaleTo255()
        {
            return new BiomVisualizationData(steepBiomColor * 255, flatBiomColor * 255);
        }

        public const int SIZE = sizeof(float) * 4 * 2; 

    }
}