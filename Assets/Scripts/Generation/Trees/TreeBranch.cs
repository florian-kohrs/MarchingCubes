using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreeBranch : MonoBehaviour
{

    public float AccumulatedLight => leaves.Aggregate(0f, (x, b) => x + b.LightAccess);

    protected IList<TreeLeaf> leaves;

    protected IList<TreeBranch> branches;

    public void DoPhotosynthesis(Tree tree)
    {
        tree.availableGlucose += AccumulatedLight * Simulation.SimulationTickRate;
        foreach (TreeBranch branch in branches)
        {
            branch.DoPhotosynthesis(tree);
        }
    }
}
