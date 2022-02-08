using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BufferPool : DisposablePoolOf<ComputeBuffer>
{
    public BufferPool(Func<ComputeBuffer> CreateItem, string bufferShaderName, params ComputeShader[] shaders) : base(CreateItem) 
    { 
        this.shaders = shaders; 
        bufferName = bufferShaderName;
    }

    protected ComputeShader[] shaders;

    protected string bufferName;

    public ComputeBuffer GetBufferForShaders()
    {
        ComputeBuffer result = GetItemFromPool();
        for (int i = 0; i < shaders.Length; i++)
        {
            shaders[i].SetBuffer(0, bufferName, result);
        }
        return result;
    }

}
