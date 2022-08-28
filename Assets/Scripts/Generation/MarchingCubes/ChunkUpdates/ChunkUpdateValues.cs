using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkUpdateValues
{
    public ChunkUpdateValues(int chunkCheckIntervalInMs, float deactivateDistance, float destroyDistance,
        float[] centerLodDistance, float mergeSplitDistanceScale)
    {
        UpdateValues(chunkCheckIntervalInMs, deactivateDistance, destroyDistance, centerLodDistance, mergeSplitDistanceScale);
    }


    public void UpdateValues(int chunkCheckIntervalInMs, float deactivateDistance, float destroyDistance,
        float[] centerLodDistance, float mergeSplitDistanceScale)
    {
        mergeSplitDistanceScale = Mathf.Max(1.1f, mergeSplitDistanceScale);

        this.chunkCheckIntervalInMs = chunkCheckIntervalInMs;
        sqrDeactivateDistance = deactivateDistance * deactivateDistance;
        sqrDestroyDistance = destroyDistance * destroyDistance;
        float[] mergeDistanceRequirement = new float[centerLodDistance.Length];
        float[] splitDistanceRequirement = new float[centerLodDistance.Length];
        for (int i = 0; i < centerLodDistance.Length; i++)
        {
            float halfChunkSize = 0;// HalfChunkSizeForLodPow(i);
            mergeDistanceRequirement[i] = centerLodDistance[i] * mergeSplitDistanceScale + halfChunkSize;
            splitDistanceRequirement[i] = centerLodDistance[i] / mergeSplitDistanceScale - halfChunkSize;
        }
        ApplySqrDistanceToList(mergeDistanceRequirement);
        ApplySqrDistanceToList(splitDistanceRequirement);
        sqrMergeDistanceRequirement = mergeDistanceRequirement;
        sqrSplitDistanceRequirement = splitDistanceRequirement;
    }

    protected float HalfChunkSizeForLodPow(int lodPow) => Mathf.Pow(2, lodPow + MarchingCubes.MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER) / 2;

    public int GetLodForSqrDistance(float sqrDistance)
    {
        for (int i = 0; i < sqrMergeDistanceRequirement.Length; i++)
        {
            if (sqrDistance <= sqrSplitDistanceRequirement[i])
                return i;
        }
        return MarchingCubes.MarchingCubeChunkHandler.DEACTIVATE_CHUNK_LOD_POWER;
    }

    protected void ApplySqrDistanceToList(float[] list)
    {
        int length = list.Length;
        for (int i = 0; i < length; i++)
        {
            list[i] *= list[i];
        }
    }

    protected int chunkCheckIntervalInMs = 500;
    public int ChunkCheckIntervalInMs => chunkCheckIntervalInMs;

    private float sqrDeactivateDistance;
    private float sqrDestroyDistance;

    public float[] sqrMergeDistanceRequirement;
    public float[] sqrSplitDistanceRequirement;

    public float SqrDeactivateDistance => sqrDeactivateDistance;
    public float SqrDestroyDistance => sqrDestroyDistance;

}
