using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynchronizedCollection<T,J> where T : ICollection<J>
{

    protected T defaultCollection;

    protected T extraCollection;

    protected bool isLocked;

    public bool IsLocked
    {
        get
        {
            return isLocked;    
        }
        set
        {
            isLocked = value;
            if(!isLocked)
            {

            }
        }
    }

}
