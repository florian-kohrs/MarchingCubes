using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangulationIndexTriangleNeighbourData
{
    public TriangulationIndexTriangleNeighbourData()
    {
        outsideNeighbours = new List<OutsideEdgeNeighbourDirection>();
        insideNeighbours = new List<InsideNeighbourConnectionInfo>();
    }

    public List<OutsideEdgeNeighbourDirection> outsideNeighbours;
    public List<InsideNeighbourConnectionInfo> insideNeighbours;
}
