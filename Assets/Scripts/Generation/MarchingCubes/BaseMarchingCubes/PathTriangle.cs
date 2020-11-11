using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTriangle
{

    /// <summary>
    /// look edges up in triangulation table
    /// </summary>
    public const int PRECONDITIONED_SHARED_EDGES = 2;

    public PathTriangle(Triangle t)
    {
        tri = t;
    }

    public Triangle tri;

    public List<PathTriangle> neighbours = new List<PathTriangle>();


    public void BuildNeighboursIn(PathTriangle t)
    {
        if (!neighbours.Contains(t) && HasSufficientConnectedTriangles(t.tri))
        {
            AddNeighbourTwoWay(t);
        }
    }

    /// <summary>
    /// doesnt check if the neighbour is already registered
    /// </summary>
    /// <param name="p"></param>
    public void AddNeighbourTwoWay(PathTriangle p)
    {
        neighbours.Add(p);
        p.neighbours.Add(this);
    }

    protected bool HasSufficientConnectedTriangles(Triangle t)
    {
        int count = 0;

        for (int i = 0; i < 3 && count < PRECONDITIONED_SHARED_EDGES; i++)
        {
            if (tri.Contains(t[i]))
            {
                count++;
            }
        }

        return count >= PRECONDITIONED_SHARED_EDGES;
    }

}
