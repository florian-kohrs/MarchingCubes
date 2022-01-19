using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarchingCubes;
using UnityEngine.Rendering;
using System;

public class EnvirenmentSpawner : MonoBehaviour
{

    public Shader EnvironmentPlacer;

    public Shader GrassSpawner;

    protected ComputeBuffer treePositions;

    protected ComputeBuffer emptyCubePositions;


    protected ComputeBuffer minDegreeCubes;


    //TODO: When saving a chunk save all positions where trees are

    public void AddEnvirenmentForChunk(IMarchingCubeChunk chunk)
    {
        

        AsyncGPUReadback.Request(treePositions, OnTreesRecieved);
    }

    protected void OnTreesRecieved(AsyncGPUReadbackRequest result)
    {
        //SetCollidersToPositions;
    }

}
