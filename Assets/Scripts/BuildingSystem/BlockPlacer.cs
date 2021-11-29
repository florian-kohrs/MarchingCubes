using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockPlacer : MonoBehaviour
{

    public BaseBuildingBlock hoveringBlock;

    private Transform objectToPlace;

    public float buildRange = 15;

    protected float buildSqrRange;

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
            IBlockPlaceOrientator orientator = hit.collider.GetComponent<IBlockPlaceOrientator>();
            if(orientator != null)
            {
                Vector3 normal = orientator.NormalFromRay(hit);
                AlignToTriangle(normal, Vector3.forward);
                if(canBuild && Input.GetMouseButtonDown(0))
                {
                    objectToPlace = null;
                    enabled = false;
                    Test();
                }
            }
            objectToPlace.position = hit.point;
        }

      
    }

    protected void AlignToTriangle(Vector3 normal, Vector3 forward)
    {
        objectToPlace.rotation = Quaternion.LookRotation(normal, -forward);
        objectToPlace.Rotate(new Vector3(90, 0, 0), Space.Self);
    }

}
