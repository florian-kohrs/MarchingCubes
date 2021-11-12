using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public abstract class BaseMarchingCubePool<T, J>
    {

        protected Stack<T> pool = new Stack<T>();

        protected abstract T CreateItem();

        protected abstract void ResetReturnedItem(T item);

        protected abstract void ApplyChunkToItem(T item, J c);

        public T GetItemFromPoolFor(J chunk)
        {
            T item;
            if (pool.Count > 0)
                item = pool.Pop();
            else
                item = CreateItem();
            ApplyChunkToItem(item, chunk);
            return item;
        }

        public void ReturnItemToPool(T item)
        {
            ResetReturnedItem(item);
            pool.Push(item);
        }


    }
}