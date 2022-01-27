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

    protected const int NUM_THREADS_PER_AXIS = 4;
    protected const int THREADS_PER_AXIS = MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE / NUM_THREADS_PER_AXIS;

    public ComputeShader EnvironmentPlacer;

    public ComputeShader TreePlacer;

    public ComputeShader GrassSpawner;

    /// <summary>
    /// at which chunk coord is which environment entity (trees, etc.) located
    /// </summary>
    protected ComputeBuffer environmentEntities;

    /// <summary>
    /// set which contains the indices of all original cubes positions => used to figure out if detailed
    /// environment can spawn on triangle
    /// </summary>
    protected ComputeBuffer originalCubeSet;

    /// <summary>
    /// set where no detailed environment will be spawned cause its occupied
    /// </summary>
    protected ComputeBuffer isTreeAtCube;

    protected struct EnvironmentData
    {
        public int cube;
    }

    //TODO: When saving a chunk save all positions where trees are

    protected void CreateBuffers()
    {
        isTreeAtCube = new ComputeBuffer(BUFFER_CHUNK_SIZE, sizeof(int));
        EnvironmentPlacer.SetBuffer(0, "entitiesAtCube", environmentEntities);
    }

    protected void SetTreeShaderBuffers()
    {

    }

    protected void RequestTreePositions()
    {
        
    }

    protected void PrepareEnvironmentForChunk(IMarchingCubeChunk chunk)
    {
        EnvironmentPlacer.SetBuffer(0, "minAngleAtCubeIndex", chunk.MinDegreeBuffer);
        EnvironmentPlacer.SetVector("anchorPosition", VectorExtension.RaiseVector3Int(chunk.AnchorPos));
    }

    public void AddEnvirenmentForOriginalChunk(IMarchingCubeChunk chunk, bool buildDetailEnvironment)
    {
        PrepareEnvironmentForChunk(chunk);
        

        EnvironmentPlacer.Dispatch(0, THREADS_PER_AXIS, THREADS_PER_AXIS, THREADS_PER_AXIS);
        AsyncGPUReadback.Request(environmentEntities, OnTreePositionsRecieved);

        //recieve tree positions
        //recieve tree rotations -> place colliders from pool
        //add grass to unused cubes
        //when editing chunk store tree positions and rotations and original cubes
    
    }

    protected void ComputeGrassForChunk(IMarchingCubeChunk chunk)
    {

    }


    public void AddEnvirenmentForEditedChunk(IMarchingCubeChunk chunk, bool buildDetailEnvironment)
    {
        EnvironmentPlacer.SetBuffer(0, "minAngleAtCubeIndex", chunk.MinDegreeBuffer);
        EnvironmentPlacer.SetVector("anchorPosition", VectorExtension.RaiseVector3Int(chunk.AnchorPos));
        EnvironmentPlacer.Dispatch(0, THREADS_PER_AXIS, THREADS_PER_AXIS, THREADS_PER_AXIS);
        AsyncGPUReadback.Request(environmentEntities, OnTreePositionsRecieved);

        //recieve tree positions
        //recieve tree rotations -> place colliders from pool
        //add grass to unused cubes
        //when editing chunk store tree positions and rotations and original cubes
    }

    protected void OnTreePositionsRecieved(AsyncGPUReadbackRequest result)
    {
        NativeArray<Matrix4x4> x = result.GetData<Matrix4x4>();
        int length = x.Length;
        for (int i = 0; i < length; i++)
        {
            Matrix4x4 transform = x[i];
            //place collider there
        }
    }

    private void OnDestroy()
    {
        environmentEntities.Dispose();
        originalCubeSet.Dispose();
        isTreeAtCube.Dispose();
    }

}
