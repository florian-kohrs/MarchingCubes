using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LazyComponent<T> where T : Component
{

    private MonoBehaviour m;

    public LazyComponent(MonoBehaviour m)
    {
        this.m = m;
    }

    public T Value
    {
        get
        {
            if (value == null)
            {
                value = m.GetComponent<T>();
            }
            return value;
        }
    }

    [SerializeField]
    private T value;

}
