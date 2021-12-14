using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInstantiatableItem
{

    GameObject CreateInstance(Transform parent);

}
