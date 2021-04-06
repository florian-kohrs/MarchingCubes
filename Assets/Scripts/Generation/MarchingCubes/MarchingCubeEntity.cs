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
            for (int i = 0; i < triangles.Count; i++)
            {
                if(triangles[i].Normal == normal)
                {
                    return triangles[i];
                }
            }
            throw new System.Exception("No triangle with same normal found!");
            //return best;
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
            foreach (var item in NeighbourData.InternNeighbourPairs)
            {
                triangles[item.first].SoftSetNeighbourTwoWay(triangles[item.second], item.firstEdgeIndices, item.sndEdgeIndice);
            }
        }

        public bool BuildNeighbours(Func<Vector3Int, MarchingCubeEntity> GetCube, Func<Vector3Int, bool> IsInBounds, List<MissingNeighbourData> addHere)
        {
            bool hasNeighbourOutOfBounds = true;
            foreach (OutsideEdgeNeighbourDirection neighbour in NeighbourData.OutsideNeighbours)
            {
                Vector3Int newPos = origin + neighbour.offset;

                if (IsInBounds(newPos))
                {
                        MarchingCubeEntity neighbourCube = GetCube(newPos);

                        OutsideNeighbourConnectionInfo info = TriangulationTableStaticData.GetIndexWithEdges(neighbourCube.triangulationIndex, neighbour.rotatedEdgePair);
                        triangles[neighbour.triangleIndex].SoftSetNeighbourTwoWay(neighbourCube.triangles[info.otherTriangleIndex],
                        neighbour.relevantVertexIndices, info.outsideNeighbourEdgeIndices);
                }
                else
                {
                    hasNeighbourOutOfBounds = false;
                    addHere.Add(new MissingNeighbourData(neighbour, newPos));
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
            foreach (OutsideEdgeNeighbourDirection neighbour in NeighbourData.OutsideNeighbours)
            {
                Vector3Int newPos = origin + neighbour.offset;
                if (!IsInBounds(newPos))
                {
                    hasNeighbourOutOfBounds = false;
                    addHere.Add(new MissingNeighbourData(neighbour, newPos));
                }
            }
            neighbourData = null;
            return hasNeighbourOutOfBounds;
        }


    }
}