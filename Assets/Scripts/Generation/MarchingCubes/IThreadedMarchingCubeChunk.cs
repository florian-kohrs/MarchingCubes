using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface IThreadedMarchingCubeChunk : IMarchingCubeChunk
    {

        bool IsInOtherThread { set; }

        void BuildAllMeshes();


    }
}
