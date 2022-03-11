using MarchingCubes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnvironmentSurface
{

    ComputeBuffer MinDegreeBuffer { get; set; }

    bool BuildDetailedEnvironment { get; }

    Vector3Int AnchorPos { get; set; }

    Maybe<Bounds> MeshBounds { get; }

    MeshData MeshData { get; }

}
