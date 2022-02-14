using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class ComputeBufferExtension
{

    public static T[] ReadAppendBuffer<T>(ComputeBuffer buffer, ComputeBuffer bufferCount, out int length)
    {
        length = GetLengthOfAppendBuffer(buffer, bufferCount);
        return ReadAppendBuffer<T>(buffer, length);
    }

    public static T[] ReadAppendBuffer<T>(ComputeBuffer buffer, ComputeBuffer bufferCount)
    {
        return ReadAppendBuffer<T>(buffer, GetLengthOfAppendBuffer(buffer, bufferCount));
    }

    public static T[] ReadAppendBuffer<T>(ComputeBuffer buffer, int length)
    {
        if (length <= 0)
        {
            return Array.Empty<T>();
        }
        else
        {
            T[] result = new T[length];
            buffer.GetData(result);
            return result;
        }
    }

    public static int GetLengthOfAppendBuffer(ComputeBuffer buffer, ComputeBuffer bufferCount)
    {
        ComputeBuffer.CopyCount(buffer, bufferCount, 0);
        int[] length = new int[1];
        bufferCount.GetData(length);
        return length[0];
    }

    public static void GetLengthOfAppendBufferAsync(ComputeBuffer buffer, ComputeBuffer bufferCount, Action<int> callback)
    {
        ComputeBuffer.CopyCount(buffer, bufferCount, 0);
        AsyncGPUReadback.Request(bufferCount, (r) => ReadLengthFromResult(r,callback));
    }

    private static void ReadLengthFromResult(AsyncGPUReadbackRequest result, Action<int> callBack)
    {
        NativeArray<int> lengthArray = result.GetData<int>();
        callBack(lengthArray[0]);
    }

    public static T[] ReadBuffer<T>(ComputeBuffer buffer)
    {
        T[] result = new T[buffer.count];
        buffer.GetData(result);
        return result;
    }


    public static void ReadBuffersAsync<T, J>(ComputeBuffer buffer1, ComputeBuffer buffer2, Action<T[], J[]> callback) where T : struct where J : struct
    {
        T[] first = null;
        J[] snd = null;
        Action checkIfBothAreDone = () =>
        {
            if (first != null && snd != null)
            {
                callback(first, snd);
            }
        };
        AsyncGPUReadback.Request(buffer1, (r) => { first = ReadFromGPUReadbackResult<T>(r).ToArray(); checkIfBothAreDone(); });
        AsyncGPUReadback.Request(buffer2, (r) => { snd = ReadFromGPUReadbackResult<J>(r).ToArray(); checkIfBothAreDone(); });
    }



    //check speed on standalone version if this is going to be slow
    public static void ReadBufferAsync<T>(ComputeBuffer buffer, Action<NativeArray<T>> callback) where T : struct
    {
        AsyncGPUReadback.Request(buffer, (r) => callback(ReadFromGPUReadbackResult<T>(r)));
    }

    private static NativeArray<T> ReadFromGPUReadbackResult<T>(AsyncGPUReadbackRequest result) where T : struct
    {
        return result.GetData<T>();
    }


    public static T[] ReadBufferUntil<T>(ComputeBuffer buffer, int length)
    {
        T[] result = new T[length];
        buffer.GetData(result);
        return result;
    }

}
