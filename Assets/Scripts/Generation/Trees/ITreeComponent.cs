using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITreeComponent 
{
    TreeResource ResourcesNeededToStayAlive { get; }

    TreeResource ResourcesNeededToGrow { get; }

}
