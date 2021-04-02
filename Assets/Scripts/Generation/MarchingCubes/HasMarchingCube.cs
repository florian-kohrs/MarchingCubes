using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MarchingCubes
{
    public class HasMarchingCube : MonoBehaviour, IHasMarchingCubeChunk
    {

        public MarchingCubeChunk chunk;

        public MarchingCubeChunk GetChunk => chunk;
    }
}