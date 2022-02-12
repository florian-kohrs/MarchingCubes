using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisposablePoolOf<T> : PoolOf<T> where T : IDisposable
{

    //TODO: If a lot of unused buffers exist, dispose some of them
    public DisposablePoolOf(Func<T> CreateItem) : base(CreateItem) { }

    protected virtual void OnDispose() { }

    public void DisposeAll()
    {
        foreach (var item in pool)
        {
            item.Dispose();
        }
        pool.Clear();
    }

    ~DisposablePoolOf()
    {
        DisposeAll();
        OnDispose();
    }

}
