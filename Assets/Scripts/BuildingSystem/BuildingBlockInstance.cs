using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingBlockInstance : MonoBehaviour, IBlockCombiner
{

    public Vector3 extends;

    public void GetDockOrientation(RaycastHit hit, out Vector3 dockPosition, out Vector3 normal, out Vector3 forward)
    {
        normal = transform.up;
        forward = transform.forward;
        dockPosition = transform.position + Vector3.Scale(hit.normal, extends).normalized;
    }

}
