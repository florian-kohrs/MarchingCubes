using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    public interface IMarchingCubeChunkHandler
    {

        //Dictionary<Vector3Int, IMarchingCubeChunk> Chunks { get; }

        void FreeCollider(ChunkLodCollider c);

        bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk);

        bool TryGetOrCreateChunkAt(Vector3Int p, out IMarchingCubeChunk chunk);

        bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk, out Vector3Int positionInOtherChunk);

        BaseMeshDisplayer GetNextMeshDisplayer();

        BaseMeshDisplayer GetNextInteractableMeshDisplayer(IMarchingCubeInteractableChunk forChunk);

        void StartWaitForParralelChunkDoneCoroutine(IEnumerator e);

        void FreeMeshDisplayer(BaseMeshDisplayer display);

        void FreeAllDisplayers(List<BaseMeshDisplayer> displayers);

        void DecreaseChunkLod(IMarchingCubeChunk chunk, int toLodPower);

        MarchingCubeChunkNeighbourLODs GetNeighbourLODSFrom(IMarchingCubeChunk chunk);

        float[] RequestNoiseForChunk(IMarchingCubeChunk chunk);

        TriangleBuilder[] GenerateCubesFromNoise(IMarchingCubeChunk chunk, int triCount, float[] noise);

        int[] GetColor(PathTriangle t, int steepness);

        void Store(Vector3Int anchorPos, float[] noise);

        float[][] GetSplittedNoiseArray(IMarchingCubeChunk chunk);

        //IMarchingCubeChunk CreateChunkFromNoiseAt(ChunkGroupTreeLeaf leaf, float[] noise);

    }

}