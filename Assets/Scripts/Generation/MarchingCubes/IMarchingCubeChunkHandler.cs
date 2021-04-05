using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMarchingCubeChunkHandler
{

    //Dictionary<Vector3Int, IMarchingCubeChunk> Chunks { get; }

    void EditNeighbourChunksAt(Vector3Int chunkOffset, Vector3Int cubeOrigin, float delta);

    bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk);

}
