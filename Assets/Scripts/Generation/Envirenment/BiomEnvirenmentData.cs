using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BiomEnvirenmentData
{

    public float grassDensity;
    public float treeDensity;

    public const int SIZE = sizeof(float) * 2;

}
