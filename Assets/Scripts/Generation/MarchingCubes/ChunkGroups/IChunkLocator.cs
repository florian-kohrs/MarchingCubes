using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IChunkLocator
    {

        IMarchingCubeChunk GetChunkAtLocal(Vector3Int pos);

    }
}