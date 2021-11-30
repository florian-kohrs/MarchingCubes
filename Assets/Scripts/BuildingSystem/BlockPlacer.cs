using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockPlacer : MonoBehaviour
{

    public BaseBuildingBlock hoveringBlock;

    public Transform planetCenter;

    private Transform objectToPlace;

    public float buildRange = 15;

    public float angleAroundNormal;

    protected float buildSqrRange;

    protected float currentMeshHeightOffset;

    protected OrientationMode orientationMode = OrientationMode.TriangleNormal;

    protected enum OrientationMode { TriangleNormal, WorldNormal};

    private void Start()
    {
        enabled = false;
        buildSqrRange = buildRange * buildRange;
        Test();
    }

    protected void Test()
    {
        BeginPlaceBlock(hoveringBlock);
    }

    public void BeginPlaceBlock(BaseBuildingBlock block)
    {
        this.hoveringBlock = block;
        objectToPlace = Instantiate(block.prefab).transform;
        currentMeshHeightOffset = block.prefab.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y;
        enabled = true;
    }

    private void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 2000))
        {
            float sqrDist = (hit.point - transform.position).sqrMagnitude;
            bool canBuild = sqrDist <= buildSqrRange;
            if (!CheckForBlockCombinerAndApply(hit))
            {
                canBuild = canBuild && CheckForBlockOrientatorAndApply(hit);
                PositionAt(hit.point);
            }
            
            if (canBuild && Input.GetMouseButtonDown(0))
            {
                objectToPlace = null;
                enabled = false;
                Test();
            }
        }
    }

    protected bool CheckForBlockCombinerAndApply(RaycastHit hit)
    {
        IBlockCombiner combiner = hit.collider.GetComponent<IBlockCombiner>();

        Vector3 normal;
        Vector3 forward;
        Vector3 dockPosition;
        combiner.GetDockOrientation(hit, out dockPosition, out normal, out forward);

        return combiner != null;
    }

    protected bool CheckForBlockOrientatorAndApply(RaycastHit hit)
    {
        IBlockPlaceOrientator orientator = hit.collider.GetComponent<IBlockPlaceOrientator>();
        AssignToOrientation(orientator, hit);
        return orientator != null;
    }

    protected void AssignToOrientation(IBlockPlaceOrientator orientator, RaycastHit hit)
    {
        Vector3 normal;
        Vector3 forward;

        if (orientationMode == OrientationMode.WorldNormal)
        {
            normal = (hit.point - planetCenter.position).normalized;
        }
        else
        {
            normal = orientator.NormalFromRay(hit);
        }

        forward = (Quaternion.Euler(Vector3.forward) * Quaternion.AngleAxis(angleAroundNormal, normal)).eulerAngles;
        
        AlignToTriangle(normal, forward);
    }

    protected void PositionAt(Vector3 hit)
    {
        objectToPlace.position = hit;
        objectToPlace.Translate(objectToPlace.up * currentMeshHeightOffset);
    }

    protected void AlignToTriangle(Vector3 normal, Vector3 forward)
    {
        objectToPlace.rotation = Quaternion.LookRotation(normal, -forward);
        objectToPlace.Rotate(new Vector3(90, 0, 0), Space.Self);
    }

}
