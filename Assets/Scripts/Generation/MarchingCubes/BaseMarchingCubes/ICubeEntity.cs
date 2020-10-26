using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICubeEntity
{

    void UpdateMesh();

    IList<ICubeEntity> Neighbours { get; set; }

}
