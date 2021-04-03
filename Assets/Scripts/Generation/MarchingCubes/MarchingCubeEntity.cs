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

        //public List<MarchingCubeEntity> neighbours = new List<MarchingCubeEntity>();

        /// <summary>
        /// generate a matrix in the beginning, saying which triangulation has neighbours in which triangulation,
        /// also with offsets at exactly one difference with the abs value of 1 in a single axis 
        /// (see for edgeindex reference:http://paulbourke.net/geometry/polygonise/)
        /// </summary>
        public void BuildInternNeighbours()
        {
            foreach (var item in NeighbourData.InternNeighbourPairs)
            {
                triangles[item.first].AddNeighbourTwoWayUnchecked(triangles[item.second], item.edge.edge1, item.edge.edge2);
            }
        }

        public List<MissingNeighbourData> BuildNeighbours(Func<Vector3Int, MarchingCubeEntity> GetCube, Func<Vector3Int, bool> IsInBounds)
        {
            List<MissingNeighbourData> missingNeighbours = null;
            foreach (OutsideEdgeNeighbourDirection neighbour in NeighbourData.OutsideNeighbours)
            {
                Vector3Int newPos = origin + neighbour.offset;

                if (IsInBounds(newPos))
                {
                    MarchingCubeEntity neighbourCube = GetCube(newPos);

                    int i = TriangulationTableStaticData.GetIndexWithEdges(neighbourCube.triangulationIndex, neighbour.rotatedEdgePair);
                    triangles[neighbour.triangleIndex].AddNeighbourTwoWay(neighbourCube.triangles[i], neighbour.edgePair.edge1, neighbour.edgePair.edge2);
                }
                else
                {
                    if (missingNeighbours == null)
                    {
                        missingNeighbours = new List<MissingNeighbourData>();
                    }
                    missingNeighbours.Add(new MissingNeighbourData(neighbour, newPos));
                }
            }

            neighbourData = null;
            return missingNeighbours;
        }


        public void BuildSpecificNeighbourInNeighbour(MarchingCubeEntity e, PathTriangle tri, EdgePair rotatedEdge)
        {
            int neighbourIndex;
            if (TriangulationTableStaticData.TryGetIndexWithEdges(e.triangulationIndex, rotatedEdge, out neighbourIndex))
            {
                tri.AddNeighbourTwoWay(e.triangles[neighbourIndex], rotatedEdge.edge1, rotatedEdge.edge1);
            }
            else
            {
                throw new System.Exception("Index was not found!");
            }
        }


        //void AddNeighboursTwoWay(MarchingCubeEntity e, PathTriangle addHere, PathTriangle add, int neighbourIndex, Vector2Int rotatedEdge)
        //{
        //    AddNeighboursTwoWay(e, addHere, add, neighbourIndex, rotatedEdge.x, rotatedEdge.y);
        //}

        //void AddNeighboursTwoWay(MarchingCubeEntity e, PathTriangle addHere, PathTriangle add, int neighbourIndex, int rotatedEdge1, int rotatedEdge2)
        //{
        //    bool wasNewNeighbour = addHere.AddNeighbourTwoWay(add, neighbourIndex, rotatedEdge1, rotatedEdge2);
        //    if (wasNewNeighbour)
        //    {
        //        AddNeighboursTwoWay(e);
        //    }
        //}

        //void AddNeighboursTwoWay(MarchingCubeEntity e, int neighbourIndex, PathTriangle tri, Vector2Int rotatedEdge)
        //{
        //    bool wasNewNeighbour = e.triangles[neighbourIndex].AddNeighbourTwoWay(tri, neighbourIndex, rotatedEdge);
        //    if (wasNewNeighbour)
        //    {
        //        AddNeighboursTwoWay(e);
        //    }
        //}

        //void AddNeighboursTwoWay(MarchingCubeEntity e)
        //{
        //     if(!neighbours.Contains(e))
        //    {
        //        neighbours.Add(e);
        //        e.neighbours.Add(this);
        //    }
        //}

        //public IList<ICubeEntity> Neighbours
        //{
        //    get
        //    {
        //        throw new System.NotImplementedException();
        //    }
        //}

        public void UpdateMesh()
        {
            throw new System.NotImplementedException();
        }

        public void BuildEntityNeighbours()
        {

        }

    }
}