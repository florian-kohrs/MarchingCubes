using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    public class MarchingCubeEntity : ICubeEntity
    {

        public List<PathTriangle> triangles = new List<PathTriangle>();

        public Vector3Int origin;

        public int triangulationIndex;

        public bool hasBuildIntern;

        public List<MarchingCubeEntity> neighbours = new List<MarchingCubeEntity>();

        /// <summary>
        /// generate a matrix in the beginning, saying which triangulation has neighbours in which triangulation,
        /// also with offsets at exactly one difference with the abs value of 1 in a single axis 
        /// (see for edgeindex reference:http://paulbourke.net/geometry/polygonise/)
        /// </summary>
        public void BuildInternNeighbours()
        {
            hasBuildIntern = true;
            int count = 0;
            foreach (PathTriangle tri in triangles)
            {
                List<System.Tuple<int, Vector2Int>> neighbourIndices = TriangulationTable.GetInternNeighbourIndiceces(triangulationIndex, count);
                if (neighbourIndices != null)
                {
                    foreach (System.Tuple<int, Vector2Int> t in neighbourIndices)
                    {
                        triangles[count].AddNeighbourTwoWay(triangles[t.Item1], t.Item2);
                    }
                }
                count++;
            }
        }


        public List<System.Tuple<PathTriangle, Vector2Int, Vector3Int>> BuildNeighbours(MarchingCubeEntity[,,] cubes, System.Func<Vector3Int, bool> IsInBounds)
        {
            List<System.Tuple<PathTriangle, Vector2Int, Vector3Int>> missingNeighbours = null;
            if (!hasBuildIntern)
            {
                BuildInternNeighbours();
            }
            int count = 0;
            foreach (PathTriangle tri in triangles)
            {
                foreach (System.Tuple<Vector2Int, Vector3Int> t in TriangulationTable.GetNeighbourOffsetForTriangle(this, count))
                {
                    Vector3Int newPos = origin + t.Item2;

                    Vector2Int rotatedEdge = TriangulationTable.RotateEdgeOn(
                            t.Item1,
                            TriangulationTable.GetAxisFromDelta(t.Item2));

                    if (IsInBounds(newPos))
                    {
                        MarchingCubeEntity neighbourCube = cubes[newPos.x, newPos.y, newPos.z];

                        int i = TriangulationTable.GetIndexWithEdges(neighbourCube.triangulationIndex, rotatedEdge);
                        AddNeighboursTwoWay(neighbourCube, tri, neighbourCube.triangles[i], count, t.Item1);
                    }
                    else
                    {
                        if (missingNeighbours == null)
                        {
                            missingNeighbours = new List<System.Tuple<PathTriangle, Vector2Int, Vector3Int>>();
                        }
                        missingNeighbours.Add(System.Tuple.Create(tri, rotatedEdge, t.Item2));
                    }
                }
                count++;
            }
            return missingNeighbours;
        }


        public void BuildSpecificNeighbourInNeighbour(MarchingCubeEntity e, PathTriangle tri, Vector2Int rotatedEdge)
        {
            int neighbourIndex;
            if (TriangulationTable.TryGetIndexWithEdges(e.triangulationIndex, rotatedEdge, out neighbourIndex))
            {
                AddNeighboursTwoWay(e, neighbourIndex, tri, rotatedEdge);
            }
        }


        void AddNeighboursTwoWay(MarchingCubeEntity e, PathTriangle addHere, PathTriangle add, int neighbourIndex, Vector2Int rotatedEdge)
        {
            bool wasNewNeighbour = addHere.AddNeighbourTwoWay(add, neighbourIndex, rotatedEdge);
            if (wasNewNeighbour)
            {
                AddNeighboursTwoWay(e);
            }
        }

        void AddNeighboursTwoWay(MarchingCubeEntity e, int neighbourIndex, PathTriangle tri, Vector2Int rotatedEdge)
        {
            bool wasNewNeighbour = e.triangles[neighbourIndex].AddNeighbourTwoWay(tri, neighbourIndex, rotatedEdge);
            if (wasNewNeighbour)
            {
                AddNeighboursTwoWay(e);
            }
        }

        void AddNeighboursTwoWay(MarchingCubeEntity e)
        {
             if(!neighbours.Contains(e))
            {
                neighbours.Add(e);
                e.neighbours.Add(this);
            }
        }

        public IList<ICubeEntity> Neighbours
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        public void UpdateMesh()
        {
            throw new System.NotImplementedException();
        }

        public void BuildEntityNeighbours()
        {

        }

    }
}