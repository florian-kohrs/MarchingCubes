using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisposablePoolOf<T> : PoolOf<T> where T : IDisposable
{

    public DisposablePoolOf(Func<T> CreateItem) : base(CreateItem) { }


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
    }

}
