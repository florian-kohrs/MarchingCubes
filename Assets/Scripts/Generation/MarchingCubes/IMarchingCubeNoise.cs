using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IMarchingCubeNoise
    {
        float[] Points { get; set; }
    }
}