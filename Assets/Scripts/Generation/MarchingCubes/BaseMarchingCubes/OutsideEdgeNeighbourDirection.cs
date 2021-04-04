using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct OutsideEdgeNeighbourDirection
{

    public int triangleIndex;

    //public Vector2Int edgePair;

    public Vector2Int rotatedEdgePair;

    public Vector2Int relevantVertexIndices;

    public Vector3Int offset;

    public OutsideEdgeNeighbourDirection(int triangleIndex, int edgeIndex1, int edgeIndex2, int vertexEdge1, int vertexEdge2, Vector3Int offset) 
        : this(triangleIndex, new Vector2Int(edgeIndex1, edgeIndex2), new Vector2Int(vertexEdge1, vertexEdge2), offset)
    {
    }

    public OutsideEdgeNeighbourDirection(int triangleIndex, Vector2Int edgePair, Vector2Int relevantVertexIndices, Vector3Int offset)
    {
        this.triangleIndex = triangleIndex;
        //this.edgePair = edgePair;
        this.offset = offset;
        this.relevantVertexIndices = relevantVertexIndices;
        rotatedEdgePair = TriangulationTableStaticData.RotateEdgeOn(
                        edgePair.x, edgePair.y,
                        TriangulationTableStaticData.GetAxisFromDelta(offset));
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
