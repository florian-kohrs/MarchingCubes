using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IChunkBuilder
    {

        void GetSizeAndLodPowerForChunkPosition(Vector3Int chunkOrigin, out int size, out int lodPower);

        int GetLodPowerForChunkPosition(Vector3Int chunkOrigin);

    }
}
