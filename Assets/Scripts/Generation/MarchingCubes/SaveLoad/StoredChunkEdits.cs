using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    [System.Serializable]
    public class StoredChunkEdits : ISizeManager
    {

        //TODO: Also store how many triangle the chunk had -> almost no memory overhead
        //and doesnt need to ask gpu so huge speed up

        //[Save]
        //public Dictionary<int, float> editedPoints = new Dictionary<int, float>();


        public PointData[] vals;
        
        int ISizeManager.ChunkSizePower { get => 5; set => throw new System.NotImplementedException(); }
    }
}
