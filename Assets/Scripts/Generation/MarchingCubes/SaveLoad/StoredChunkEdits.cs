using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    [System.Serializable]
    public class StoredChunkEdits : ISizeManager
    {

        [Save]
        public Dictionary<int, float> editedPoints = new Dictionary<int, float>();

        
        int ISizeManager.ChunkSizePower { get => 5; set => throw new System.NotImplementedException(); }
    }
}
