using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IStoreableMarchingCube : IMarchingCubeNoise
    {

        void StoreChunk(StoredChunkEdits storage);

    }
}