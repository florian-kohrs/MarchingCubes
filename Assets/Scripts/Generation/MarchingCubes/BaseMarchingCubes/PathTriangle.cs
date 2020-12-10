using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathTriangle : INavigatable<PathTriangle, PathTriangle>
{

    public PathTriangle(MarchingCubeChunk chunk, Triangle t)
    {
        this.chunk = chunk;
        tri = t;
    }

    MarchingCubeChunk chunk;

    public Triangle tri;

    public List<PathTriangle> neighbours = new List<PathTriangle>(3);


    public List<Vector2> edgesWithoutNeighbour = null;
    public List<Vector2> EdgesWithoutNeighbour
    {
        get
        {
            if(edgesWithoutNeighbour == null)
            {
                edgesWithoutNeighbour = new List<Vector2>();
            }
            return edgesWithoutNeighbour;
        }
    }


    public void BuildNeighboursIn(PathTriangle t)
    {
        //if (!neighbours.Contains(t))
        {
            neighbours.Add(t);
        }
    }


    /// <summary>
    /// doesnt check if the neighbour is already registered
    /// </summary>
    /// <param name="p"></param>
    public void AddNeighbourTwoWay(PathTriangle p)
    {
        if (!neighbours.Contains(p))
        {
            neighbours.Add(p);
        }
        if (!p.neighbours.Contains(this))
        {
            p.neighbours.Add(this);
        }
        if (p.neighbours.Count > 3 || neighbours.Count > 3)
        {
            List<System.Tuple<TriangulationTable.MirrorAxis, List<int>, List<int>, int, int>> triNeighbours = new List<System.Tuple<TriangulationTable.MirrorAxis, List<int>, List<int>, int, int>>();
            foreach (PathTriangle tri in neighbours)
            {
                int pos = tri.chunk.CubeEntities[tri.tri.origin.x, tri.tri.origin.y, tri.tri.origin.z].triangles.IndexOf(tri);

                List<int> i = TriangulationTable.triangulation[tri.tri.triangulationIndex]
                    .Skip(pos * 3)
                    .Take(3)
                    .ToList();

                TriangulationTable.MirrorAxis axis;
                if (tri.tri.origin.x != this.tri.origin.x)
                    axis = TriangulationTable.MirrorAxis.X;
                else if (tri.tri.origin.y != this.tri.origin.y)
                    axis = TriangulationTable.MirrorAxis.Y;
                else
                    axis = TriangulationTable.MirrorAxis.Z;

                int rotatedstuff = TriangulationTable.RotateIndex(tri.tri.triangulationIndex, axis);
                List<int> i2 = TriangulationTable.triangulation[rotatedstuff]
                    .ToList();

                triNeighbours.Add(System.Tuple.Create(axis, i, i2, rotatedstuff, pos));
            }

            List<List<int>> triNeighbours2 = new List<List<int>>();
            foreach (PathTriangle tri in p.neighbours)
            {
                triNeighbours2.Add(TriangulationTable.triangulation[tri.tri.triangulationIndex].Skip(tri.chunk.CubeEntities[tri.tri.origin.x, tri.tri.origin.y, tri.tri.origin.z].triangles.IndexOf(tri) * 3).Take(3).ToList());
            }
            int[] tris = TriangulationTable.triangulation[tri.triangulationIndex];
        }
    }

    public IEnumerable<PathTriangle> GetCircumjacent(PathTriangle field)
    {
        return field.neighbours;
    }

    public float DistanceToTarget(PathTriangle from, PathTriangle to)
    {
        return (to.UnrotatedMiddlePointOfTriangle - from.UnrotatedMiddlePointOfTriangle).magnitude;
    }

    public float DistanceToField(PathTriangle from, PathTriangle to)
    {
        return (to.UnrotatedMiddlePointOfTriangle - from.UnrotatedMiddlePointOfTriangle).magnitude;
    }

    public bool ReachedTarget(PathTriangle current, PathTriangle destination)
    {
        return current == destination;
    }

    public bool IsEqual(PathTriangle t1, PathTriangle t2)
    {
        return t1 == t2;
    }

    private Vector3 middlePointOfTriangle;


    public Vector3 UnrotatedMiddlePointOfTriangle
    {
        get
        {
            if (middlePointOfTriangle == default)
            {
                middlePointOfTriangle = (tri.a + tri.b + tri.c) / 3;
            }

            return Vector3.Scale(middlePointOfTriangle + chunk.transform.position, chunk.transform.lossyScale);
        }
    }

}
