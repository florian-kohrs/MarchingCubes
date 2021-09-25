using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface INavField
{

    void SetUsedInPathIteration(int iteration);

    int LastUsedInPathIteration { get; }

}
