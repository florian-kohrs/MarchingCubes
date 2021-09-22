using System.Collections.Generic;
using UnityEngine;

public class Path<T, J>
{

    public Path(INavigatable<T, J> assistant, T current, J target)
    {
        nav = assistant;
        parent = null;
        this.current = current;
        this.target = target;
    }

    public Path(T current, Path<T, J> parent, float distanceToNode)
    {
        this.current = current;
        target = parent.target;
        this.parent = parent;
        nav = parent.nav;
        previousDistance = distanceToNode;
        distance = nav.DistanceToTarget(current, target);
    }

    public List<Path<T, J>> Advance()
    {
        List<Path<T, J>> result = new List<Path<T, J>>();
        List<T> circumjacent = nav.GetCircumjacent(current);
        T c;
        for (int i = 0; i < circumjacent.Count; ++i)
        {
            c = circumjacent[i];
            if (parent == null || !nav.IsEqual(current, parent.current))
            {
                result.Add(new Path<T, J>(
                    c, this, previousDistance + nav.DistanceToField(current, c)));
            }
        }
        return result;
    }

    public Path<T, J> Advance(T t)
    {
        return new Path<T, J>(t, this, previousDistance + nav.DistanceToField(current, t));
    }

    public INavigatable<T, J> nav;

    public T current;

    public J target;

    public Path<T, J> parent;

    public float distance;

    public float previousDistance;

    public float TotalMinimumDistance => distance + previousDistance;

    public float TotalEstimatedMinimumDistance(float accuracyFactor)
    {
        return distance + (accuracyFactor * previousDistance);
    }

    public void BuildPath(ref IList<T> result)
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
