using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public static T[] ReadBuffer<T>(ComputeBuffer buffer)
    {
        T[] result = new T[buffer.count];
        buffer.GetData(result);
        return result;
    }

    public static T[] ReadBufferUntil<T>(ComputeBuffer buffer, int length)
    {
        T[] result = new T[length];
        buffer.GetData(result);
        return result;
    }

}
