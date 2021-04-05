using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MarchingCubes
{
    public class HasMarchingCube : MonoBehaviour, IHasMarchingCubeChunk
    {

        public IMarchingCubeInteractableChunk chunk;

        public IMarchingCubeInteractableChunk GetChunk => chunk;
    }
}