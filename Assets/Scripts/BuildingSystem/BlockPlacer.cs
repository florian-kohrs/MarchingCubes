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

    public const int buildColliderLayer = 0;

    protected float buildSqrRange;

    protected float currentMeshHeightOffset;

    public OrientationMode orientationMode = OrientationMode.TriangleNormal;

    public enum OrientationMode { TriangleNormal, WorldNormal};

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
        hoveringBlock = block;
        objectToPlace = Instantiate(block.prefab).transform;
        currentMeshHeightOffset = block.prefab.GetComponent<MeshFilter>().sharedMesh.bounds.extents.y;
        enabled = true;
    }

    private void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 2000, buildColliderLayer))
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
        if(Input.GetKey(KeyCode.Q))
        {
            angleAroundNormal += Time.deltaTime * 60;
        }
    }

    protected bool CheckForBlockCombinerAndApply(RaycastHit hit)
    {
        IBlockCombiner combiner = hit.collider.GetComponent<IBlockCombiner>();
        if (combiner == null)
            return false;

        Vector3 normal;
        Vector3 forward;
        Vector3 dockPosition;
        combiner.GetDockOrientation(hit, out dockPosition, out normal, out forward);
        return true;
    }

    protected bool CheckForBlockOrientatorAndApply(RaycastHit hit)
    {
        IBlockPlaceOrientator orientator = hit.collider.GetComponent<IBlockPlaceOrientator>();
        if(orientator == null)
            return false;

        AssignToOrientation(orientator, hit);
        return true;
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

        AlignToTriangle(normal, Vector3.forward);

        objectToPlace.Rotate(new Vector3(0,angleAroundNormal,0),Space.Self);
    }

    protected void PositionAt(Vector3 hit)
    {
        objectToPlace.position = hit;
        objectToPlace.position += objectToPlace.up * currentMeshHeightOffset;
    }

    protected void AlignToTriangle(Vector3 normal, Vector3 forward)
    {
        objectToPlace.rotation = Quaternion.LookRotation(normal, -forward);
        objectToPlace.Rotate(new Vector3(90, 0, 0), Space.Self);
    }

}
