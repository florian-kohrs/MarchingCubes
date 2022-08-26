using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MarchingCubes
{
    public interface IChunkGroupDestroyableOrganizer<T> : IChunkGroupOrganizer<T>
    {

        void DestroyBranch();

    }
}