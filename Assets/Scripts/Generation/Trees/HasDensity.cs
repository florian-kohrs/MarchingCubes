using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class HasDensity : MonoBehaviour
{

    public abstract void AddDensitiy(Dictionary<Vector3Int, float> densityMap);

}
