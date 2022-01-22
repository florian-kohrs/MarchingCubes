using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class BiomScriptableObject : ScriptableObject
    {

        public string biomName;

        public BiomNoiseData biom;

        public BiomVisualizationData visualizationData;

        public BiomEnvirenmentData envirenmentData;

    }
}
