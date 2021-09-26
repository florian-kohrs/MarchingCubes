using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INavField
{

    void SetUsedInPathIteration(int iteration);

    void SetUsedInBackwardsPathIteration(int iteration);

    int LastUsedInPathIteration { get; }

    int LastUsedInBackwardsPathIteration { get; }


}
