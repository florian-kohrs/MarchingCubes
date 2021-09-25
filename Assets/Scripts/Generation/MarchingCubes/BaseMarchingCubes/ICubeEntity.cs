using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{
    public interface ICubeEntity
    {

        List<PathTriangle> GetNeighboursOf(PathTriangle tri);

    }
}