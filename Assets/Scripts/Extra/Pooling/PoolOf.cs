using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolOf<T>
{

    public PoolOf(Func<T> CreateItem)
    {
        this.CreateItem = CreateItem;
    }

    protected Stack<T> pool = new Stack<T>();

    protected Func<T> CreateItem;

    protected virtual T BuildItemInstance() => CreateItem();

    public void ReturnItemToPool(T item)
    {
        pool.Push(item);
    }

    public T GetItemFromPool()
    {
        T item;
        if (pool.Count > 0)
        {
            item = pool.Pop();
        }
        else
        {
            item = BuildItemInstance();
        }
        return item;
    }


}
