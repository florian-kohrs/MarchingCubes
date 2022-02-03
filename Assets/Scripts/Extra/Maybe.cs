using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maybe<T> where T : struct
{

    protected T value;

    protected bool hasValue;

    public void LazyRemoveValue()
    {
        hasValue = false;
    }

    public T Value
    {
        get 
        { 
            return value; 
        }
        set 
        { 
            hasValue = true;
            this.value = value; 
        }
    }

    public bool HasValue => hasValue;


}
