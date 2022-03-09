using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferPool : DisposablePoolOf<ComputeBuffer>
{
    public BufferPool(Func<ComputeBuffer> CreateItem) : base(CreateItem) 
    { 
    }


}
