using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            for (int i = 0; i < triangles.Count; i++)
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
            for (int i = 0; i < triangles.Count; i++)
            {
                if (triangles[i].Normal == normal)
                {
                    return i;
                }
                else
                {
                    float distance = (triangles[i].OriginalLOcalMiddlePointOfTriangle - point).sqrMagnitude;
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
            for (int i = 0;i< NeighbourData.InternNeighbourPairs.Count; i++)
            {
                item = NeighbourData.InternNeighbourPairs[i];
                triangles[item.first].SoftSetNeighbourTwoWay(triangles[item.second], item.firstEdgeIndices, item.sndEdgeIndice);
            }
        }

        public bool BuildNeighbours(Func<Vector3Int, MarchingCubeEntity> GetCube, Func<Vector3Int, bool> IsInBounds, List<MissingNeighbourData> addHere, bool overrideNeighbours = false)
        {
            bool hasNeighbourOutOfBounds = true;
            OutsideEdgeNeighbourDirection neighbour;
            for (int i = 0; i < neighbourData.OutsideNeighbours.Count; i++)
            {
                neighbour = neighbourData.OutsideNeighbours[i];
                Vector3Int newPos = origin + neighbour.offset;

                if (IsInBounds(newPos))
                {
                    MarchingCubeEntity neighbourCube = GetCube(newPos);

                    OutsideNeighbourConnectionInfo info = TriangulationTableStaticData.GetIndexWithEdges(neighbourCube.triangulationIndex, neighbour.rotatedEdgePair);
                    if (overrideNeighbours)
                    {
                        triangles[neighbour.triangleIndex].OverrideNeighbourTwoWay(neighbourCube.triangles[info.otherTriangleIndex], neighbour.relevantVertexIndices, info.outsideNeighbourEdgeIndices);
                    }
                    else
                    {
                        triangles[neighbour.triangleIndex].SoftSetNeighbourTwoWay(neighbourCube.triangles[info.otherTriangleIndex], neighbour.relevantVertexIndices, info.outsideNeighbourEdgeIndices);
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
            OutsideNeighbourConnectionInfo info = TriangulationTableStaticData.GetIndexWithEdges(e.triangulationIndex, rotatedEdge);
            tri.SoftSetNeighbourTwoWay(e.triangles[info.otherTriangleIndex], myEdgeIndices, info.outsideNeighbourEdgeIndices);

        }

        public bool FindMissingNeighbours(Func<Vector3Int, bool> IsInBounds, List<MissingNeighbourData> addHere)
        {
            bool hasNeighbourOutOfBounds = true;
            OutsideEdgeNeighbourDirection neighbour;
            for (int i = 0; i < neighbourData.OutsideNeighbours.Count; i++)
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
            for (int i = 0; i < neighbourData.OutsideNeighbours.Count; i++)
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