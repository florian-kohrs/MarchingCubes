using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingBlockInstance : MonoBehaviour, IBlockCombiner
{


    public Vector3 extends;

    public void GetDockOrientation(RaycastHit hit, out Vector3 dockPosition, out Vector3 normal, out Vector3 forward, out Vector3 localDockOrientation)
    {
        normal = transform.up;
        forward = transform.forward;
        localDockOrientation = GetLocalHitDirection(hit);
        dockPosition = transform.position + transform.TransformDirection(Vector3.Scale(localDockOrientation, extends));
    }

    protected Vector3 GetLocalHitDirection(RaycastHit hit)
    {
        Vector3 result;
        if(hit.normal == transform.up)
        {
            result = Vector3.up;
        }
        else if(hit.normal == transform.forward)
        {
            result = Vector3.forward;
        }
        else if (hit.normal == transform.right)
        {
            return Vector3.right;
        }
        else if(hit.normal == -transform.up)
        {
            return -Vector3.up;
        }
        else if (hit.normal == -transform.forward)
        {
            return -Vector3.forward;
        }
        else if (hit.normal == -transform.right)
        {
            return -Vector3.right;
        }
        else
        {
            throw new System.Exception("No direction matched normal!");
        }
        return result;
    }

}
