using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeSeed<T> : BaseTreeRoot where T : Tree
{
    protected override float GatherNutrition()
    {
        throw new System.NotImplementedException();
    }

    protected override float GatherWater()
    {
        throw new System.NotImplementedException();
    }
}
