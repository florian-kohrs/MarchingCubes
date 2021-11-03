using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IStorageGroupOrganizer<T> : IChunkGroupOrganizer<T>
    {

        bool TryGetNodeWithSizePower(int[] relativePosition, int sizePow, out IStorageGroupOrganizer<StoredChunkEdits> child);

        //bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise);

        float[] NoiseMap { get; }

        bool HasNoiseMapReady { get; }

        int ChildrenWithMipMapReady { get; }

        int DirectNonNullChildren { get; }

    }
}
