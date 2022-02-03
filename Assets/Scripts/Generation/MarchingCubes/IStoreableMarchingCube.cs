using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IStoreableMarchingCube
    {

        void StoreChunk(StoredChunkEdits storage);
    
    }
}