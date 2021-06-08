using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public interface IMarchingCubeChunkHandler
    {

        //Dictionary<Vector3Int, IMarchingCubeChunk> Chunks { get; }

        void EditNeighbourChunksAt(Vector3Int chunkOffset, Vector3Int cubeOrigin, float delta);

        bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk);

        BaseMeshDisplayer GetNextMeshDisplayer();

        BaseMeshDisplayer GetNextInteractableMeshDisplayer(IMarchingCubeInteractableChunk forChunk);

        void StartWaitForParralelChunkDoneCoroutine(IEnumerator e);

        void FreeMeshDisplayer(BaseMeshDisplayer display);

        void FreeAllDisplayers(List<BaseMeshDisplayer> displayers);

        void DecreaseChunkLod(IMarchingCubeChunk chunk, int toLodPower);

    }

}