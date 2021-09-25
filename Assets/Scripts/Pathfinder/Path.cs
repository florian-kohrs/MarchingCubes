using MarchingCubes;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinding
{
    public class Path
    {

        public Path(PathTriangle current, PathTriangle target)
        {
            parent = null;
            this.current = current;
            this.target = target;
        }

        public Path(PathTriangle current, Path parent, float distanceToNode)
        {
            this.current = current;
            target = parent.target;
            this.parent = parent;
            previousDistance = distanceToNode;
            distance = current.DistanceToTarget(target);
        }

        public List<Path> Advance()
        {
            List<Path> result = new List<Path>();
            List<PathTriangle> circumjacent = current.GetCircumjacent();
            PathTriangle c;
            for (int i = 0; i < circumjacent.Count; ++i)
            {
                c = circumjacent[i];
                if (parent == null || !current.IsEqual(parent.current))
                {
                    result.Add(new Path(c, this, previousDistance + 1));
                }
            }
            return result;
        }

        public Path Advance(PathTriangle t)
        {
            return new Path(t, this, previousDistance + 1);
        }

        public PathTriangle current;

        public PathTriangle target;

        public Path parent;

        public float distance;

        public float previousDistance;

        public float TotalMinimumDistance => distance + previousDistance;

        public float TotalEstimatedMinimumDistance(float accuracyFactor)
        {
            return distance + (accuracyFactor * previousDistance);
        }

        public void BuildPath(ref IList<PathTriangle> result)
        {
            result.Insert(0, current);
            if (parent != null)
            {
                parent.BuildPath(ref result);
            }
        }

        public override int GetHashCode()
        {
            return (int)(distance * Mathf.Pow(2, 24) + previousDistance * Mathf.Pow(2, 16));
        }

    }

}