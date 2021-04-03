using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct OutsideEdgeNeighbourDirection
{

    public int triangleIndex;

    public EdgePair edgePair;

    public EdgePair rotatedEdgePair;

    public Vector3Int offset;



    public OutsideEdgeNeighbourDirection(int triangleIndex, int edgeIndex1, int edgeIndex2, Vector3Int offset) : this(triangleIndex, new EdgePair(edgeIndex1, edgeIndex2), offset)
    {
    }

    public OutsideEdgeNeighbourDirection(int triangleIndex, EdgePair edgePair, Vector3Int offset)
    {
        this.triangleIndex = triangleIndex;
        this.edgePair = edgePair;
        this.offset = offset;
        rotatedEdgePair = new EdgePair(TriangulationTableStaticData.RotateEdgeOn(
                        edgePair.edge1, edgePair.edge2,
                        TriangulationTableStaticData.GetAxisFromDelta(offset)));
    }


    //public bool neighbourOffsetX;
    //public bool neighbourOffsetY;
    //public bool neighbourOffsetZ;

    //public Vector3Int NeighbourOffset
    //{
    //    get
    //    {
    //        Vector3Int v3
    //    }
    //}
}
