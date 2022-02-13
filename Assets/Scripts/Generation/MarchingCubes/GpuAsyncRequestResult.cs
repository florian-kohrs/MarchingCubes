using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace MarchingCubes
{
    public class GpuAsyncRequestResult
    {

        public GpuAsyncRequestResult(NativeArray<TriangleBuilder> requestResult)
        {
            Maybe<NativeArray<TriangleBuilder>> result = new Maybe<NativeArray<TriangleBuilder>>();
            result.Value = requestResult;
            this.requestResult = result;
        }

        public GpuAsyncRequestResult()
        {
            requestResult = new Maybe<NativeArray<TriangleBuilder>>();
        }

        public Maybe<NativeArray<TriangleBuilder>> requestResult;

        public bool IsEmpty => !requestResult.HasValue;

        public TriangleChunkHeap ToTriangleChunkHeap()
        {
            if(IsEmpty)
            {
                return new TriangleChunkHeap(Array.Empty<TriangleBuilder>(), 0, 0);
            }
            else 
            {
                return new TriangleChunkHeap(requestResult.Value.ToArray(),0,requestResult.Value.Length);
            }
        }

    }
}