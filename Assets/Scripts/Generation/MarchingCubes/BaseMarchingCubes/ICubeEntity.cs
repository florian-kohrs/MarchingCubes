using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{
    public interface ICubeEntity
    {

        void UpdateMesh();

        IList<ICubeEntity> Neighbours { get; }

    }
}