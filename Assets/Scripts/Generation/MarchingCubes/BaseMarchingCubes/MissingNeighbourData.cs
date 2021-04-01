using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MissingNeighbourData
{

    public OutsideEdgeNeighbourDirection neighbour;

    public Vector3Int cubeEntity;


    public MissingNeighbourData(OutsideEdgeNeighbourDirection neighbour, Vector3Int cubeEntity)
    {
        this.neighbour = neighbour;
        this.cubeEntity = cubeEntity;
    }

}
