using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinding
{
    public enum PathAccuracy { Perfect, VeryGood, Good, Decent, NotSoGoodAnymore, ITakeAnyThing }

    public class Pathfinder
    {


        private float AccuracyFactor(PathAccuracy acc)
        {
            float result;
            switch (acc)
            {
                case PathAccuracy.Perfect:
                    {
                        result = 1f;
                        break;
                    }
                case PathAccuracy.VeryGood:
                    {
                        result = 0.95f;
                        break;
                    }
                case PathAccuracy.Good:
                    {
                        result = 0.8f;
                        break;
                    }
                case PathAccuracy.Decent:
                    {
                        result = 0.5f;
                        break;
                    }
                case PathAccuracy.NotSoGoodAnymore:
                    {
                        result = 0.2f;
                        break;
                    }
                case PathAccuracy.ITakeAnyThing:
                    {
                        result = 0f;
                        break;
                    }
                default:
                    {
                        throw new System.ArgumentException("Unexpected Accuracy: " + acc);
                    }
            }
            return result;
        }


        public static IList<PathTriangle> FindPath(PathTriangle start, PathTriangle target, PathAccuracy accuracy)
        {
            return new Pathfinder(start, target, accuracy).GetPath();
        }

        public BinaryHeap<float, Path> pathTails;

        private Pathfinder(PathTriangle start, PathTriangle target, PathAccuracy accuracy, float estimatedStepProgress = 0.5f)
        {
            this.start = start;
            this.target = target;
            pathAccuracy = AccuracyFactor(accuracy);
            float estimatedLength = start.DistanceToTarget(target);
            int estimatedQueueSize = (int)Mathf.Clamp(estimatedStepProgress * estimatedLength * (1 - (pathAccuracy / 2)), 10, 10000);
            pathTails = new BinaryHeap<float, Path>(float.MinValue, float.MaxValue, estimatedQueueSize);
        }

        float pathAccuracy;

        protected PathTriangle target;

        protected PathTriangle start;


        public static int pathIteration = 0;

        protected IList<PathTriangle> GetPath()
        {
            pathIteration++;
            AddTailUnchecked(new Path(start, target));
            return BuildPath();
        }

        protected IList<PathTriangle> BuildPath()
        {
            int count = 0;
            while (HasTail && !ReachedTarget)
            {
                count++;
                AdvanceClosest();
            }
            IList<PathTriangle> result = new List<PathTriangle>();
            pathTails.Peek().BuildPath(ref result);
            if (ReachedTarget)
            {
                Debug.Log("found path after: " + count + " iterations of length " + result.Count);
            }
            else
            {
                Debug.Log("no valid path found");
            }
            return result;
        }

        public void AdvanceClosest()
        {
            Path closest = GetClosest();
            List<PathTriangle> circumjacent = closest.current.GetCircumjacent();
            PathTriangle t;
            int count = circumjacent.Count;
            for (int i = 0; i < count; ++i)
            {
                t = circumjacent[i];
                if (t.LastUsedInPathIteration < pathIteration)
                {
                    AddTailUnchecked(closest.Advance(t));
                }
            }
        }

        public Path GetClosest()
        {
            return pathTails.Dequeue();
        }



        public bool ReachedTarget => pathTails.Peek().current == target;


        public void AddTailUnchecked(Path p)
        {
            p.current.SetUsedInPathIteration(pathIteration);
            pathTails.Enqueue(p.TotalEstimatedMinimumDistance(pathAccuracy), p);
        }

        protected bool TryGetClosestField(out Path path)
        {
            if (pathTails.size > 0)
            {
                path = pathTails.Dequeue();
            }
            else
            {
                path = null;
            }
            return path != null;
        }

        protected bool HasTail => pathTails.size > 0;

    }
}
