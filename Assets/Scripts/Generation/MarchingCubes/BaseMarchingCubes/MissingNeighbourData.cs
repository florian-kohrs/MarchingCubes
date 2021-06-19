using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MissingNeighbourData
{

    public OutsideEdgeNeighbourDirection outsideNeighbour;

    public Vector3Int originCubeEntity;


    public MissingNeighbourData(OutsideEdgeNeighbourDirection neighbour, Vector3Int cubeEntity)
    {
        this.outsideNeighbour = neighbour;
        this.originCubeEntity = cubeEntity;
    }

}
