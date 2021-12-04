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

        void SetChunkColliderOf(IMarchingCubeChunk c);

        bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk);

        bool TryGetOrCreateChunkAt(Vector3Int p, out IMarchingCubeChunk chunk);

        bool TryGetReadyChunkAt(Vector3Int p, out IMarchingCubeChunk chunk, out Vector3Int positionInOtherChunk);

        MarchingCubeMeshDisplayer GetNextMeshDisplayer();

        MarchingCubeMeshDisplayer GetNextInteractableMeshDisplayer(IMarchingCubeInteractableChunk forChunk);

        void FreeMeshDisplayer(MarchingCubeMeshDisplayer display);

        void FreeAllDisplayers(List<MarchingCubeMeshDisplayer> displayers);

        void DecreaseChunkLod(IMarchingCubeChunk chunk, int toLodPower);

        PointData[] RequestNoiseForChunk(IMarchingCubeChunk chunk);

        TriangleBuilder[] GenerateCubesFromNoise(IMarchingCubeChunk chunk, int triCount, float[] noise);

        int[] GetColor(PathTriangle t, int steepness);

        void Store(Vector3Int anchorPos, PointData[] noise);

        void TakeMeshDisplayerBack(MarchingCubeMeshDisplayer freeDisplayer);

        int ReadCurrentTriangleData(out TriangleBuilder[] ts, int triCount = -1);

        //IMarchingCubeChunk CreateChunkFromNoiseAt(ChunkGroupTreeLeaf leaf, float[] noise);

    }

}