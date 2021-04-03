using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    public class PathTriangle : INavigatable<PathTriangle, PathTriangle>
    {

        public PathTriangle(MarchingCubeChunk chunk, Triangle t)
        {
            this.chunk = chunk;
            tri = t;
            //Quaternion inverse = Quaternion.Inverse(Quaternion.Euler(normal));
            //Vector3 a1 = Vector3.ProjectOnPlane(t.a, normal);
            //Vector3 a2 = Vector3.ProjectOnPlane(t.b, normal);
            //Vector3 a3= Vector3.ProjectOnPlane(t.c, normal);
        }


        MarchingCubeChunk chunk;

        public Triangle tri;

        ///replace with array
        public List<PathTriangle> neighbours = new List<PathTriangle>(3);

        protected List<float> neighbourDistanceMapping;

        protected List<float> NeighbourDistanceMapping
        {
            get
            {
                if (neighbourDistanceMapping == null)
                {
                    neighbourDistanceMapping = new List<float>(3);
                }
                return neighbourDistanceMapping;
            }
        }


        public bool IsInTriangle(Vector3 point)
        {
            float a = AreaOfTriangle(tri.a, tri.b,tri.c);
            float a1 = AreaOfTriangle(point,tri.a, tri.b);
            float a2 = AreaOfTriangle(point, tri.b, tri.c);
            float a3 = AreaOfTriangle(point,tri.a, tri.c)  ;
            return a == a1 + a2 + a3;
        }

        protected float AreaOfTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            return default;
        }

        public List<Vector2> edgesWithoutNeighbour = null;

        public List<Vector2> EdgesWithoutNeighbour
        {
            get
            {
                if (edgesWithoutNeighbour == null)
                {
                    edgesWithoutNeighbour = new List<Vector2>();
                }
                return edgesWithoutNeighbour;
            }
        }


        public void AddNeighbourTwoWay(PathTriangle p, int edge1, int edge2)
        {
            if (!neighbours.Contains(p))
            {
                AddNeighbourTwoWayUnchecked(p, edge1, edge2);
            }
        }

        public void AddNeighbourTwoWayUnchecked(PathTriangle p, int edge1, int edge2)
        {
                neighbours.Add(p);
                p.neighbours.Add(this);
                BuildDistance(p, edge1, edge2);
            
        }

        public void AddNeighbourTwoWayUnchecked(PathTriangle p, byte edge1, byte edge2)
        {
            AddNeighbourTwoWayUnchecked(p, (int)edge1, (int)edge2);
        }

        public void AddNeighbourTwoWayUnchecked(PathTriangle p, EdgePair edges)
        {
            AddNeighbourTwoWayUnchecked(p, (int)edges.edge1, (int)edges.edge2);
        }

        public void AddNeighbourTwoWay(PathTriangle p, byte edge1, byte edge2)
        {
            AddNeighbourTwoWay(p, (int)edge1, (int)edge2);
        }

        //public void AddNeighbourTwoWay(PathTriangle p, Vector2Int edgeIndices)
        //{
        //    AddNeighbourTwoWay(p, edgeIndices.x, edgeIndices);
        //}

        public void BuildDistance(PathTriangle p, int edge1, int edge2)
        {
            Vector3 middleEdgePoint = tri[edge1] + ((tri[edge2] - tri[edge1]) / 2);
            float distance = (OriginalMiddlePointOfTriangle - middleEdgePoint).magnitude;
            distance += (OriginalMiddlePointOfTriangle - p.OriginalMiddlePointOfTriangle).magnitude;
            NeighbourDistanceMapping.Add(distance);
            p.NeighbourDistanceMapping.Add(distance);
        }
        public IEnumerable<PathTriangle> GetCircumjacent(PathTriangle field)
        {
            return field.neighbours;
        }

        public float DistanceToTarget(PathTriangle from, PathTriangle to)
        {
            return (to.OriginalMiddlePointOfTriangle - from.OriginalMiddlePointOfTriangle).magnitude;
        }

        public float DistanceToField(PathTriangle from, PathTriangle to)
        {
            return from.NeighbourDistanceMapping[from.neighbours.IndexOf(to)];
        }

        public bool ReachedTarget(PathTriangle current, PathTriangle destination)
        {
            return current == destination;
        }

        public bool IsEqual(PathTriangle t1, PathTriangle t2)
        {
            return t1 == t2;
        }

        public Vector3 Normal => Vector3.zero;// tri.normal;

        public Vector3 OriginalMiddlePointOfTriangle => Vector3.zero;// tri.middlePoint + chunk.transform.position;
       
    }

}