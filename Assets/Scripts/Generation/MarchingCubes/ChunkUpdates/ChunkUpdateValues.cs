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
        mergeSplitDistanceScale = Mathf.Max(0.1f, mergeSplitDistanceScale);

        this.chunkCheckIntervalInMs = chunkCheckIntervalInMs;
        sqrDeactivateDistance = deactivateDistance * deactivateDistance;
        sqrDestroyDistance = destroyDistance * destroyDistance;
        float[] mergeDistanceRequirement = new float[centerLodDistance.Length];
        float[] splitDistanceRequirement = new float[centerLodDistance.Length];
        for (int i = 0; i < centerLodDistance.Length; i++)
        {
            mergeDistanceRequirement[i] = centerLodDistance[i] * mergeSplitDistanceScale;
            splitDistanceRequirement[i] = centerLodDistance[i] / mergeSplitDistanceScale;
        }
        ApplySqrDistanceToList(mergeDistanceRequirement);
        ApplySqrDistanceToList(splitDistanceRequirement);
        sqrMergeDistanceRequirement = mergeDistanceRequirement;
        sqrSplitDistanceRequirement = splitDistanceRequirement;
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
