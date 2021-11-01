using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    [System.Serializable]
    public class StoredChunkEdits : ISizeManager
    {

        [Save]
        public Dictionary<Vector3Int, float> editedPoints = new Dictionary<Vector3Int, float>();

        
        int ISizeManager.ChunkSizePower { get => 5; set => throw new System.NotImplementedException(); }
    }
}
