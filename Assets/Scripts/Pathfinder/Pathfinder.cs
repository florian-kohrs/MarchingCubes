using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PathAccuracy { Perfect, VeryGood, Good, Decent, NotSoGoodAnymore, ITakeAnyThing }

public class Pathfinder<T, J>
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


    public static IList<T> FindPath(INavigatable<T, J> assistant, T start, J target, PathAccuracy accuracy)
    {
        return new Pathfinder<T, J>(assistant, start, target, accuracy).GetPath();
    }

    private Pathfinder(INavigatable<T, J> assistant, T start, J target, PathAccuracy accuracy, float estimatedStepProgress = 0.5f)
    {
        this.start = start;
        this.target = target;
        pathAccuracy = AccuracyFactor(accuracy);
        nav = assistant;
        float estimatedLength = nav.DistanceToTarget(start, target);
        int estimatedQueueSize = (int)Mathf.Clamp(estimatedStepProgress * estimatedLength * (1 - (pathAccuracy / 2)), 10, 10000);
        pathTails = new BinaryHeap<float, Path<T, J>>(float.MinValue, float.MaxValue, estimatedQueueSize);
    }

    INavigatable<T, J> nav;

    float pathAccuracy;

    protected J target;

    protected T start;

    protected IList<T> GetPath()
    {
        AddTailUnchecked(new Path<T, J>(nav, start, target));
        return BuildPath();
    }

    protected IList<T> BuildPath()
    {
        int count = 0;
        while (HasTail && !ReachedTarget)
        {
            count++;
            AdvanceClosest();
        }
        IList<T> result = new List<T>();
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
        Path<T, J> closest = GetClosest();
        usedFields.Add(closest.current);
        
        foreach (T t in nav.GetCircumjacent(closest.current))
        {
            if (!usedFields.Contains(t))
            {
                AddTailUnchecked(closest.Advance(t));
            }
        }
    }

    public Path<T, J> GetClosest()
    {
        Path<T, J> closest;

        do
        {
            closest = pathTails.Dequeue();
        }
        while (usedFields.Contains(closest.current));

        return closest;
    }

    public bool ReachedTarget => nav.ReachedTarget(pathTails.Peek().current, target);


    public BinaryHeap<float, Path<T, J>> pathTails;

    public HashSet<T> usedFields = new HashSet<T>();


    public void AddTailUnchecked(Path<T, J> p)
    {
        pathTails.Enqueue(p.TotalEstimatedMinimumDistance(pathAccuracy), p);
    }

    protected bool TryGetClosestField(out Path<T, J> path)
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
