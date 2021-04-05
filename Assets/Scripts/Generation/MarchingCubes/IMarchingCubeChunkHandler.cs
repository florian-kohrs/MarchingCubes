using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMarchingCubeChunkHandler
{

    Dictionary<Vector3Int, MarchingCubeChunk> Chunks { get; }

    void EditNeighbourChunksAt(Vector3Int chunkOffset, Vector3Int cubeOrigin, float delta);

}
