using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeEntity : ICubeEntity
    {

        public MarchingCubeEntity(ICubeNeighbourFinder cubeFinder, int triangulationIndex)
        {
            this.cubeFinder = cubeFinder;
            this.triangulationIndex = triangulationIndex;
            int size = TriangulationTable.triangulation[triangulationIndex].Length / 3;
            triangles = new PathTriangle[size];
        }

        protected ICubeNeighbourFinder cubeFinder;

        public PathTriangle GetTriangleWithNormal(Vector3 normal)
        {
            return triangles[GetTriangleIndexWithNormal(normal)];
        }

        public void AddTriangle(PathTriangle tri)
        {
            triangles[counter] = tri;
            counter++;
        }


        public int GetTriangleIndexWithNormal(Vector3 normal)
        {
            int triCount = triangles.Length;
            for (int i = 0; i < triCount; ++i)
            {
                if (triangles[i].Normal == normal)
                {
                    return i;
                }
            }
            throw new Exception("No triangle with same normal found!");
        }

        public int GetTriangleIndexWithNormalOrClosest(Vector3 normal, Vector3 point)
        {
            float closestSqr = float.MaxValue;
            int closestIndex = -1;
            int count = triangles.Length;
            for (int i = 0; i < count; ++i)
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

        public PathTriangle[] triangles;

        public Vector3Int origin;

        public Vector3Int Origin => origin;

        public int triangulationIndex;

        private int counter = 0;

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

        public int TriangulationIndex => triangulationIndex;

        public List<PathTriangle> GetNeighboursOf(PathTriangle tri)
        {
            List<PathTriangle> result = new List<PathTriangle>();
            int index = Array.IndexOf(triangles,tri);
            GetInternNeighbours(result, index);

            //TODO: check to make structs readonly and other optimations
            //possibly stop reading first to avoid memcopy
            OutsideEdgeNeighbourDirection neighbour;
            List<OutsideEdgeNeighbourDirection> edgeDirs = neighbourData.OutsideNeighbours;
            int count = edgeDirs.Count;
            for (int i = 0; i < count; ++i)
            {
                neighbour = edgeDirs[i];

                if (neighbour.triangleIndex != index)
                {
                    continue;
                }
                Vector3Int newPos = origin + neighbour.offset;
                MarchingCubeEntity cubeNeighbour;
                //TODO: store if it is boundary cube else check not necessary
                if (cubeFinder.IsCubeInBounds(newPos.x, newPos.y, newPos.z))
                {
                    cubeNeighbour = cubeFinder.GetEntityAt(newPos);
                }
                else
                {
                    cubeNeighbour = cubeFinder.GetEntityInNeighbourAt(newPos);
                    if (cubeNeighbour == null)
                        continue;
                }
                OutsideNeighbourConnectionInfo info;
                if (TriangulationTableStaticData.TryGetNeighbourTriangleIndex(
                    cubeNeighbour.triangulationIndex,
                    neighbour.originalEdgePair.x,
                    neighbour.originalEdgePair.y,
                    out info))
                {
                    result.Add(cubeNeighbour.triangles[info.otherTriangleIndex]);
                }
            }
            return result;
        }

        /// <summary>
        /// generate a matrix in the beginning, saying which triangulation has neighbours in which triangulation,
        /// also with offsets at exactly one difference with the abs value of 1 in a single axis 
        /// (see for edgeindex reference:http://paulbourke.net/geometry/polygonise/)
        /// </summary>
        public void GetInternNeighbours(List<PathTriangle> result, int triIndex)
        {
            IndexNeighbourPair item;
            List<IndexNeighbourPair> neighbours = NeighbourData.InternNeighbourPairs;
            int count = neighbours.Count;
            for (int i = 0; i < count; ++i)
            {
                item = neighbours[i];
                if (item.first == triIndex)
                {
                    result.Add(triangles[item.second]);
                }
                else if (item.second == triIndex)
                {
                    result.Add(triangles[item.first]);
                }
            }
        }

        public bool FindMissingNeighbours(Func<int, int, int, bool> IsInBounds, List<MissingNeighbourData> addHere)
        {
            bool hasNeighbourOutOfBounds = true;
            OutsideEdgeNeighbourDirection neighbour;
            var data = NeighbourData;
            if(data==null)
            {
                data = NeighbourData;
            }
            int count = NeighbourData.OutsideNeighbours.Count;
            for (int i = 0; i < count; ++i)
            {
                neighbour = neighbourData.OutsideNeighbours[i];
                if (!IsInBounds(origin.x + neighbour.offset.x, origin.y + neighbour.offset.y, origin.z + neighbour.offset.z))
                {
                    hasNeighbourOutOfBounds = false;
                    addHere.Add(new MissingNeighbourData(neighbour, origin));
                }
            }
            neighbourData = null;
            return hasNeighbourOutOfBounds;
        }

        public static bool FindMissingNeighboursAt(int triangulationIndex, Vector3Int origin, Func<Vector3Int, bool> IsInBounds, bool[] hasNeighbourInDirection)
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
                    hasNeighbourInDirection[VectorExtension.GetIndexFromDirection(neighbour.offset)] = true;
                }
            }
            return hasNeighbourOutOfBounds;
        }

        public int IndexOfTri(PathTriangle tri)
        {
            return Array.IndexOf(triangles, tri);
        }
    }
}