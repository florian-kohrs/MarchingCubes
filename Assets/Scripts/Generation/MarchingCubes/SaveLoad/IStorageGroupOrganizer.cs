using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IStorageGroupOrganizer<T> : IChunkGroupOrganizer<T>
    {

        bool TryGetMipMapOfChunkSizePower(int[] relativePosition, int sizePow, out float[] storedNoise);

        float[] NoiseMap { get; }

    }
}
