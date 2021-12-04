using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IStorageGroupOrganizer<T> : IChunkGroupOrganizer<T>
    {

        bool TryGetNodeWithSizePower(int[] relativePosition, int sizePow, out IStorageGroupOrganizer<StoredChunkEdits> child);

        bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out PointData[] storedNoise, out bool isMipMapComplete);

        PointData[] NoiseMap { get; }

        bool HasNoiseMapReady { get; }

        bool IsMipMapComplete { get; }

        int ChildrenWithMipMapReady { get; }

        int DirectNonNullChildren { get; }

    }
}
