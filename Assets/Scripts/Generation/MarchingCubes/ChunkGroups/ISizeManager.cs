using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public interface ISizeManager 
    {
        int ChunkSizePower { get; set; }
    }
}