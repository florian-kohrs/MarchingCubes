using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTreeRoot : MonoBehaviour
{

    protected abstract float GatherWater();

    protected abstract float GatherNutrition();
    

    public void GatherResources(Tree tree)
    {
        float nutritions = GatherNutrition();
        float water = GatherWater();
        tree.availableWater += water;
        tree.availableNutrition += nutritions;
    }
}
