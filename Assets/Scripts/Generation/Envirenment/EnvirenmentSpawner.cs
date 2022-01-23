using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarchingCubes;
using UnityEngine.Rendering;
using System;
using Unity.Collections;

public class EnvirenmentSpawner : MonoBehaviour
{

    protected const int BUFFER_CHUNK_SIZE = 
        MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE * 
        MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE *
        MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE;

    public Shader EnvironmentPlacer;

    public Shader TreePlacer;

    public Shader GrassSpawner;

    protected ComputeBuffer treePositions;

    protected ComputeBuffer IsTreeAtCube;

    protected ComputeBuffer minDegreeCubes;

    protected struct TreePosition
    {
        public Vector3Int cube;
    }

    //TODO: When saving a chunk save all positions where trees are

    protected void CreateBuffers()
    {
        IsTreeAtCube = new ComputeBuffer(BUFFER_CHUNK_SIZE, sizeof(int));
    }

    public void AddEnvirenmentForChunk(IMarchingCubeChunk chunk)
    {
        

        AsyncGPUReadback.Request(treePositions, OnTreesRecieved);
    }

    protected void OnTreesRecieved(AsyncGPUReadbackRequest result)
    {
        NativeArray<Matrix4x4> x = result.GetData<Matrix4x4>();
        int length = x.Length;
        for (int i = 0; i < length; i++)
        {
            Matrix4x4 transform = x[i];
            //place collider there
        }
    }

}
