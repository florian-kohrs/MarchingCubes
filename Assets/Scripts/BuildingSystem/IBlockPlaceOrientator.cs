using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBlockPlaceOrientator
{
    
    Vector3 NormalFromRay(RaycastHit hit);

}
