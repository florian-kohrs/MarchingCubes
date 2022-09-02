using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkUpdateValues
    {
        public ChunkUpdateValues(int chunkCheckIntervalInMs, float destroyDistance,
            float[] centerLodDistance, float mergeSplitDistanceScale)
        {
            UpdateValues(chunkCheckIntervalInMs, destroyDistance, centerLodDistance, mergeSplitDistanceScale);
        }


        public void UpdateValues(int chunkCheckIntervalInMs, float destroyDistance,
            float[] centerLodDistance, float mergeSplitDistanceScale)
        {
            mergeSplitDistanceScale = Mathf.Max(1.1f, mergeSplitDistanceScale);

            this.chunkCheckIntervalInMs = chunkCheckIntervalInMs;
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

            sqrCenterDistanceRequirement = centerLodDistance;
            ApplySqrDistanceToList(sqrCenterDistanceRequirement);
        }

        protected float HalfChunkSizeForLodPow(int lodPow) => Mathf.Pow(2, lodPow + MarchingCubes.MarchingCubeChunkHandler.DEFAULT_CHUNK_SIZE_POWER) / 2;

        public int GetLodPowerForSqrDistance(float sqrDistance)
        {
            for (int i = 0; i < sqrMergeDistanceRequirement.Length; i++)
            {
                if (sqrDistance <= sqrCenterDistanceRequirement[i])
                    return i;
            }
            return MarchingCubes.MarchingCubeChunkHandler.DEACTIVATE_CHUNK_LOD_POWER;
        }

        public bool CanHaveLodPowerAt(float sqrDistance, int lodPower)
        {
            return !HasLowerLodPowerAs(sqrDistance, lodPower)
                && !HasHigherLodPowerAs(sqrDistance, lodPower);
        }

        public bool HasLowerLodPowerAs(float sqrDistance, int lodPower)
        {
            bool shouldSplit = false;
            int splitLod = lodPower - 1;
            if (splitLod >= 0)
                shouldSplit = sqrDistance <= sqrCenterDistanceRequirement[splitLod];

            return shouldSplit;
        }

        public bool HasHigherLodPowerAs(float sqrDistance, int lodPower)
        {
            return sqrDistance > sqrCenterDistanceRequirement[lodPower]; ;
        }

        public static void ApplySqrDistanceToList(float[] list)
        {
            int length = list.Length;
            for (int i = 0; i < length; i++)
            {
                list[i] *= list[i];
            }
        }

        public static void ApplySqrDistanceToList(int[] list)
        {
            int length = list.Length;
            for (int i = 0; i < length; i++)
            {
                list[i] *= list[i];
            }
        }

        public bool ShouldChunkBeDeactivated(float sqrDistance)
        {
            return sqrDistance > sqrMergeDistanceRequirement[MarchingCubeChunkHandler.MAX_CHUNK_EXTRA_SIZE_POWER];
        }

        public bool ShouldChunkBeDestroyed(float sqrDistance)
        {
            return sqrDistance > sqrDestroyDistance;
        }

        protected int chunkCheckIntervalInMs = 500;
        public int ChunkCheckIntervalInMs => chunkCheckIntervalInMs;

        private float sqrDestroyDistance;

        public float[] sqrMergeDistanceRequirement;
        public float[] sqrSplitDistanceRequirement;
        public float[] sqrCenterDistanceRequirement;

        public float SqrDestroyDistance => sqrDestroyDistance;

    }
}