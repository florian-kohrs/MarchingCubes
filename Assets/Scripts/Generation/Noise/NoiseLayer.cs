using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    [System.Serializable]
    public class NoiseLayer
    {

        public bool enabled = true;

        public UseFirstLayerAs useFirstLayerAs;

        public NoiseSettings noiseSettings;

        public enum UseFirstLayerAs { Mask, InverseMask, Nothing }

    }
}