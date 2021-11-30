using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBlockCombiner
{

    void GetDockOrientation(RaycastHit hit, out Vector3 dockPosition, out Vector3 normal, out Vector3 forward);

}
