using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTriangle
{

    public PathTriangle(Triangle t)
    {
        tri = t;
    }

    public Triangle tri;

    public List<PathTriangle> neighbours = new List<PathTriangle>();


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
        neighbours.Add(p);
        p.neighbours.Add(this);
    }


}
