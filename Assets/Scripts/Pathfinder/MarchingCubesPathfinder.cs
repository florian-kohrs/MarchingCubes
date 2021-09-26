using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathFinding
{
    public enum PathAccuracy { Perfect, VeryGood, Good, Decent, NotSoGoodAnymore, ITakeAnyThing }

    public class MarchingCubesPathfinder
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
            return new MarchingCubesPathfinder(start, target, accuracy).GetPath();
        }

        public BinaryHeap<float, PathTriangle> pathTails;

        public BinaryHeap<float, PathTriangle> backwardsTails;

        private MarchingCubesPathfinder(PathTriangle start, PathTriangle target, PathAccuracy accuracy, float estimatedStepProgress = 0.5f)
        {
            this.start = start;
            this.target = target;
            pathAccuracy = AccuracyFactor(accuracy);
            float estimatedLength = start.DistanceToTarget(target);
            int estimatedQueueSize = (int)Mathf.Clamp(estimatedStepProgress * estimatedLength * (1 - (pathAccuracy / 2)), 10, 10000);
            pathTails = new BinaryHeap<float, PathTriangle>(float.MinValue, float.MaxValue, estimatedQueueSize);
        }

        float pathAccuracy;

        protected PathTriangle target;

        protected PathTriangle start;


        public static int pathIteration = 0;

        protected IList<PathTriangle> GetPath()
        {
            if (start == target)
                return new List<PathTriangle>() { start };

            pathIteration++;
            start.activeInPathIteration = pathIteration;
            start.prevDistance = 0;
            start.pathParent = null;
            target.activeInPathIteration = pathIteration;
            target.prevDistance = 0;
            target.pathParent = null;
            AddTailUnchecked(start, pathTails);
            AddTailUnchecked(target, backwardsTails);
            return BuildPath();
        }

        protected bool reachedTarget;

        protected List<PathTriangle> BuildPath()
        {
            List<PathTriangle> result = new List<PathTriangle>();
            int count = 0;
            while (HasTail && !reachedTarget)
            {
                count++;
                AdvanceClosest();
                if (!reachedTarget)
                {
                    if (HasBackwardsTail)
                    {
                        AdvanceClosestBackwards();
                    }
                    else
                    {
                        break;
                    }
                }

            }
            if (reachedTarget)
            {
                forwardPath.BuildPath(result);
                result.Reverse();
                backwardsPath.BuildPath(result);
                Debug.Log("found path after: " + count + " iterations of length " + result.Count);
            }
            else
            {
                Debug.Log("no valid path found");
            }
            return result;
        }

        PathTriangle forwardPath = null;
        PathTriangle backwardsPath = null;

        public void AdvanceClosest()
        {
            PathTriangle closest = pathTails.Dequeue();
           
            List<PathTriangle> circumjacent = closest.GetCircumjacent();
            PathTriangle t;
            int count = circumjacent.Count;
            for (int i = 0; i < count; ++i)
            {
                t = circumjacent[i];
                if (t.LastUsedInPathIteration < pathIteration)
                {
                    if (t.LastUsedInBackwardsPathIteration >= pathIteration)
                    {
                        reachedTarget = true;
                        forwardPath = closest;
                        backwardsPath = t;
                        break;
                    }
                    else
                    {
                        t.pathParent = closest;
                        t.prevDistance = closest.prevDistance + closest.DistanceTo(t);
                        AddTailUnchecked(t, pathTails);
                    }
                }
            }
        }

        public void AdvanceClosestBackwards()
        {
            PathTriangle closest = backwardsTails.Dequeue();

            List<PathTriangle> circumjacent = closest.GetCircumjacent();
            PathTriangle t;
            int count = circumjacent.Count;
            for (int i = 0; i < count; ++i)
            {
                t = circumjacent[i];
                if (t.LastUsedInBackwardsPathIteration < pathIteration)
                {
                    if (t.LastUsedInPathIteration >= pathIteration)
                    {
                        reachedTarget = true;
                        forwardPath = t;
                        backwardsPath = closest;
                        break;
                    }
                    else
                    {
                        t.pathParent = closest;
                        t.prevDistance = closest.prevDistance + closest.DistanceTo(t);
                        AddTailUnchecked(t, backwardsTails);
                    }
                }
            }
        }


        public bool DoesFieldCompletePath(PathTriangle tri)
        {
            return tri.LastUsedInBackwardsPathIteration == tri.LastUsedInPathIteration;
        }


        public void AddTailUnchecked(PathTriangle p, BinaryHeap<float, PathTriangle>addHere)
        {
            p.SetUsedInPathIteration(pathIteration);
            float key = p.prevDistance * pathAccuracy + p.DistanceToTarget(target);
            addHere.Enqueue(key, p);
        }

        protected bool HasTail => pathTails.size > 0;

        protected bool HasBackwardsTail => backwardsTails.size > 0;

    }
}
