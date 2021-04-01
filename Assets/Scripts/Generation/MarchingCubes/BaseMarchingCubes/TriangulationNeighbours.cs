using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangulationNeighbours
{

    public HashSet<IndexNeighbourPair> InternNeighbourPairs = new HashSet<IndexNeighbourPair>();

    public List<OutsideEdgeNeighbourDirection> OutsideNeighbours = new List<OutsideEdgeNeighbourDirection>();

}
