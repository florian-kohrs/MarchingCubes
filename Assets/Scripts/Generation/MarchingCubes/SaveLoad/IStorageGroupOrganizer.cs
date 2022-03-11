using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IStorageGroupOrganizer<T> : IChunkGroupOrganizer<T>
    {

        bool TryGetNodeWithSizePower(int[] relativePosition, int sizePow, out IStorageGroupOrganizer<StoredChunkEdits> child);

        bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise, out bool isMipMapComplete);

        float[] NoiseMap { get; }

        bool HasNoiseMapReady { get; }

        bool IsMipMapComplete { get; }

        int ChildrenWithMipMapReady { get; }

        int DirectNonNullChildren { get; }

        void RemoveMipMapInHirachy();

    }
}
