﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MarchingCubes
{
    public interface IHasMarchingCubeChunk
    {
        MarchingCubeChunk GetChunk { get; }
    }
}