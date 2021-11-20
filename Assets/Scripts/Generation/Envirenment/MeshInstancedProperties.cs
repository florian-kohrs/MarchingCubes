using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{
    public struct MeshInstancedProperties
    {

        public Matrix4x4 mat;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4; // matrix;
        }
    }
}