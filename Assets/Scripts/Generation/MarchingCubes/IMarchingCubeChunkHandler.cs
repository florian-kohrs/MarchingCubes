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

        void SetChunkColliderOf(ICompressedMarchingCubeChunk c);

        bool TryGetReadyChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk);

        bool TryGetOrCreateChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk);

        /// <summary>
        /// returns true if the chunk was created
        /// </summary>
        /// <param name="p"></param>
        /// <param name="editPoint"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="delta"></param>
        /// <param name="maxDistance"></param>
        /// <param name="chunk"></param>
        /// <returns></returns>
        bool CreateChunkWithNoiseEdit(Vector3Int p, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance, out ICompressedMarchingCubeChunk chunk);

        bool TryGetReadyChunkAt(Vector3Int p, out ICompressedMarchingCubeChunk chunk, out Vector3Int positionInOtherChunk);

        MarchingCubeMeshDisplayer GetNextMeshDisplayer();

        MarchingCubeMeshDisplayer GetNextInteractableMeshDisplayer(IMarchingCubeChunk forChunk);

        void FreeMeshDisplayer(MarchingCubeMeshDisplayer display);

        void FreeAllDisplayers(List<MarchingCubeMeshDisplayer> displayers);

        void DecreaseChunkLod(ICompressedMarchingCubeChunk chunk, int toLodPower);

        float[] RequestNoiseForChunk(ICompressedMarchingCubeChunk chunk);

        void SetEditedNoiseAtPosition(IMarchingCubeChunk chunk, Vector3 editPoint, Vector3Int start, Vector3Int end, float delta, float maxDistance);

        TriangleBuilder[] GenerateCubesFromNoise(ICompressedMarchingCubeChunk chunk, int triCount, float[] noise);

        Color32 GetColor(PathTriangle t, int steepness);

        void Store(Vector3Int anchorPos, IMarchingCubeChunk chunk, bool overrideNoise = false);

        void TakeMeshDisplayerBack(MarchingCubeMeshDisplayer freeDisplayer);

        int ReadCurrentTriangleData(out TriangleBuilder[] ts, int triCount = -1);

        //IMarchingCubeChunk CreateChunkFromNoiseAt(ChunkGroupTreeLeaf leaf, float[] noise);

        bool TryLoadPoints(ICompressedMarchingCubeChunk marchingCubeChunk, out float[] loadedPoints);
        void ReturnMinDegreeBuffer(ComputeBuffer minDegreeBuffer);

        void StartEnvironmentPipelineForChunk(IEnvironmentSurface environmentChunk);

    }

}
