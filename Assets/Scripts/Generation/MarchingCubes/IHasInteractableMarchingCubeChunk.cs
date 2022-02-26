using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MarchingCubes
{
    public interface IHasInteractableMarchingCubeChunk : IBlockPlaceOrientator
    {
        MarchingCubeChunk GetChunk { get; }
    }
}
