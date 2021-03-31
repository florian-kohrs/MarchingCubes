using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree : MonoBehaviour
{

    public BaseTreeRoot root;

    public TreeBranch trunk;

    public float availableWater;

    public float availableGlucose;

    public float availableNutrition;

    public void GatherResources()
    {
        root.GatherResources(this);
        trunk.DoPhotosynthesis(this);
    }

}
