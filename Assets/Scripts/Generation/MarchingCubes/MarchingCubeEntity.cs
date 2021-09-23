using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeEntity //: ICubeEntity
    {

        public PathTriangle GetTriangleWithNormal(Vector3 normal)
        {
            return triangles[GetTriangleIndexWithNormal(normal)];
        }

        public int GetTriangleIndexWithNormal(Vector3 normal)
        {
            int triCount = triangles.Count;
            for (int i = 0; i < triCount; ++i)
            {
                if (triangles[i].Normal == normal)
                {
                    return i;
                }
            }
            throw new System.Exception("No triangle with same normal found!");
        }

        public int GetTriangleIndexWithNormalOrClosest(Vector3 normal, Vector3 point)
        {
            float closestSqr = float.MaxValue;
            int closestIndex = -1;
            for (int i = 0; i < triangles.Count; ++i)
            {
                if (triangles[i].Normal == normal)
                {
                    return i;
                }
                else
                {
                    float distance = (triangles[i].MiddlePoint - point).sqrMagnitude;
                    if (distance < closestSqr)
                    {
                        closestSqr = distance;
                        closestIndex = i;
                    }
                }
            }
            return closestIndex;
        }

        public PathTriangle GetTriangleWithNormalOrClosest(Vector3 normal, Vector3 point)
        {
            return triangles[GetTriangleIndexWithNormalOrClosest(normal, point)];
        }

        public List<PathTriangle> triangles = new List<PathTriangle>();

        public Vector3Int origin;

        public int triangulationIndex;

        protected TriangulationNeighbours neighbourData;

        protected TriangulationNeighbours NeighbourData
        {
            get
            {
                if (neighbourData == null)
                {
                    neighbourData = TriangulationTableStaticData.GetNeighbourData(triangulationIndex);
                }
                return neighbourData;
            }
        }

        /// <summary>
        /// generate a matrix in the beginning, saying which triangulation has neighbours in which triangulation,
        /// also with offsets at exactly one difference with the abs value of 1 in a single axis 
        /// (see for edgeindex reference:http://paulbourke.net/geometry/polygonise/)
        /// </summary>
        public void BuildInternNeighbours()
        {
            IndexNeighbourPair item;
            List<IndexNeighbourPair> neighbours = NeighbourData.InternNeighbourPairs;
            int count = neighbours.Count;
            for (int i = 0; i < count; ++i)
            {
                item = neighbours[i];
                triangles[item.first].SoftSetNeighbourTwoWay(triangles[item.second], item.firstEdgeIndices, item.sndEdgeIndice);
            }
        }


        public bool BuildNeighbours(bool isBorderPoint, Func<Vector3Int, MarchingCubeEntity> GetCube, Func<Vector3Int, bool> IsInBounds, List<MissingNeighbourData> addHere, bool overrideNeighbours = false)
        {
            bool hasNeighbourOutOfBounds = true;
            OutsideEdgeNeighbourDirection neighbour;
            List<OutsideEdgeNeighbourDirection> edgeDirs = neighbourData.OutsideNeighbours;
            int count = edgeDirs.Count;
            for (int i = 0; i < count; ++i)
            {
                neighbour = edgeDirs[i];
                Vector3Int newPos = origin + neighbour.offset;

                if (!isBorderPoint || IsInBounds(newPos))
                {
                    MarchingCubeEntity neighbourCube = GetCube(newPos);

                    ///save offset in dict to not need outside neighbours

                    //OutsideNeighbourConnectionInfo info = TriangulationTableStaticData.GetIndexWithEdges(neighbourCube.triangulationIndex, neighbour.rotatedEdgePair);
                    OutsideNeighbourConnectionInfo info;
                    if (TriangulationTableStaticData.TryGetNeighbourTriangleIndex(
                        triangulationIndex,
                        neighbourCube.triangulationIndex,
                        neighbour.triangleIndex * 3,
                        neighbour.relevantVertexIndices.x,
                        neighbour.relevantVertexIndices.y,
                        out info))
                    {
                        if (overrideNeighbours)
                        {
                            triangles[neighbour.triangleIndex].OverrideNeighbourTwoWay(
                                neighbourCube.triangles[info.otherTriangleIndex], 
                                neighbour.relevantVertexIndices.x, 
                                neighbour.relevantVertexIndices.y, 
                                info.outsideNeighbourEdgeIndicesX, 
                                info.outsideNeighbourEdgeIndicesY);
                        }
                        else
                        {
                            triangles[neighbour.triangleIndex].SoftSetNeighbourTwoWay(
                                neighbourCube.triangles[info.otherTriangleIndex], 
                                neighbour.relevantVertexIndices.x, 
                                neighbour.relevantVertexIndices.y, 
                                info.outsideNeighbourEdgeIndicesX, 
                                info.outsideNeighbourEdgeIndicesY);
                        }
                    }
                }
                else
                {
                    hasNeighbourOutOfBounds = false;
                    addHere.Add(new MissingNeighbourData(neighbour, origin));
                }

                
            }
            neighbourData = null;
            return hasNeighbourOutOfBounds;
        }


        public void BuildSpecificNeighbourInNeighbour(MarchingCubeEntity e, PathTriangle tri, Vector2Int myEdgeIndices, Vector2Int rotatedEdge)
        {
            if(e == null)
            {
                Debug.LogError("was null");
            }
            OutsideNeighbourConnectionInfo info = TriangulationTableStaticData.GetIndexWithEdges(e.triangulationIndex, rotatedEdge);
            tri.SoftSetNeighbourTwoWay(e.triangles[info.otherTriangleIndex], myEdgeIndices.x, myEdgeIndices.y, info.outsideNeighbourEdgeIndicesX, info.outsideNeighbourEdgeIndicesY);
        }

        public bool FindMissingNeighbours(Func<Vector3Int, bool> IsInBounds, List<MissingNeighbourData> addHere)
        {
            bool hasNeighbourOutOfBounds = true;
            OutsideEdgeNeighbourDirection neighbour;
            int count = neighbourData.OutsideNeighbours.Count;
            for (int i = 0; i < count; ++i)
            {
                neighbour = neighbourData.OutsideNeighbours[i];
                Vector3Int newPos = origin + neighbour.offset;
                if (!IsInBounds(newPos))
                {
                    hasNeighbourOutOfBounds = false;
                    addHere.Add(new MissingNeighbourData(neighbour, origin));
                }
            }
            neighbourData = null;
            return hasNeighbourOutOfBounds;
        }

        public static bool FindMissingNeighboursAt(int triangulationIndex, Vector3Int origin, Func<Vector3Int, bool> IsInBounds, List<MissingNeighbourData> addHere)
        {
            bool hasNeighbourOutOfBounds = true;
            TriangulationNeighbours neighbourData = TriangulationTableStaticData.GetNeighbourData(triangulationIndex);
            OutsideEdgeNeighbourDirection neighbour;
            int count = neighbourData.OutsideNeighbours.Count;
            for (int i = 0; i < count; ++i)
            {
                neighbour = neighbourData.OutsideNeighbours[i];
                Vector3Int newPos = origin + neighbour.offset;
                if (!IsInBounds(newPos))
                {
                    hasNeighbourOutOfBounds = false;
                    addHere.Add(new MissingNeighbourData(neighbour, origin));
                }
            }
            return hasNeighbourOutOfBounds;
        }


    }
}