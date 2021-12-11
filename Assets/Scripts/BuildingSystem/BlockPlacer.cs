using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockPlacer : MonoBehaviour
{
    //TODO: function to combine a set of disconnected cubes 

    public const string BUILDING_BLOCK_LAYER_NAME = "BuildingBlock";

    protected static int BUILDING_BLOCK_LAYER_ID;
    protected static int DEFAULT_AND_BUILDING_BLOCK_LAYER_ID;

    public BaseBuildingBlock hoveringBlock;

    public Transform planetCenter;

    public Material couldPlaceMat;

    public Material cantPlaceMat;

    public bool usePlacementHelper;

    private Material originalMat;

    private IBlockCombiner dockedToBlock;

    private Transform objectToPlace;

    private MeshRenderer toPlaceRenderer;

    public float buildRange = 15;

    public float angleAroundNormal;

    public const int buildColliderLayer = 1 | 9;

    public const int buildingBlockLayer = 1 | 9;

    protected float buildSqrRange;

    protected Vector3 currentMeshExtend;

    public BuildingReferenceSystem system;

    public OrientationMode orientationMode = OrientationMode.TriangleNormal;

    public enum OrientationMode { TriangleNormal, WorldNormal};

    private void Start()
    {
        enabled = false;
        BUILDING_BLOCK_LAYER_ID = LayerMask.GetMask(BUILDING_BLOCK_LAYER_NAME);
        DEFAULT_AND_BUILDING_BLOCK_LAYER_ID = 1 | BUILDING_BLOCK_LAYER_ID;
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
        lastRay = default;
        lastMousePos = default;
        rayDidHit = false;
        objectToPlace = Instantiate(block.prefab).transform;
        currentMeshExtend = objectToPlace.GetComponent<MeshFilter>().sharedMesh.bounds.extents;
        toPlaceRenderer = objectToPlace.GetComponent<MeshRenderer>();
        originalMat = toPlaceRenderer.sharedMaterial;
        enabled = true;
    }

    protected Ray lastRay;
    protected Vector3 lastMousePos;
    protected bool rayDidHit;
    protected RaycastHit hit;

    protected const float DISTANCE_THRESHOLD_BEFORE_FIRE_NEW_RAY = 0.05f;
    protected const float SQR_DISTANCE_THRESHOLD_BEFORE_FIRE_NEW_RAY = DISTANCE_THRESHOLD_BEFORE_FIRE_NEW_RAY * DISTANCE_THRESHOLD_BEFORE_FIRE_NEW_RAY;

    protected bool RayChangedEnoughToReshoot(Ray newRay)
    {
        return Input.mousePosition != lastMousePos
            || (lastRay.origin - newRay.origin).sqrMagnitude > SQR_DISTANCE_THRESHOLD_BEFORE_FIRE_NEW_RAY;
    }
      
    private void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if(RayChangedEnoughToReshoot(ray))
        {
            lastRay = ray;
            lastMousePos = Input.mousePosition;
            rayDidHit = Physics.Raycast(ray, out hit, 2000, DEFAULT_AND_BUILDING_BLOCK_LAYER_ID);
        }

        if (rayDidHit)
        {
            float sqrDist = (hit.point - transform.position).sqrMagnitude;
            bool canBuild = sqrDist <= buildSqrRange;
            if (!CheckForBlockDockingAndApply(hit))
            {
                canBuild = canBuild && CheckForBlockOrientatorAndApply(hit);
                PositionAt(hit.point);
                if(system != null)
                {
                    system.AssignToBuildingReference(objectToPlace, hit.point);
                }
                else if (usePlacementHelper)
                {
                    IBlockCombiner c;
                    SearchForCubeConnection();
                }
            }
            
            if (canBuild && Input.GetMouseButtonDown(0))
            {
                PlaceCurrentBlock();
            }
            else
            {
                ApplyMaterialForUnplacedBlock(canBuild);
            }
        }
        if(Input.GetKey(KeyCode.Q))
        {
            angleAroundNormal += Time.deltaTime * 60;
        }
    }

    protected void ApplyMaterialForUnplacedBlock(bool canPlace)
    {
        if (canPlace)
            toPlaceRenderer.material = couldPlaceMat;
        else
            toPlaceRenderer.material = cantPlaceMat;
    }

    protected void PlaceCurrentBlock()
    {
        objectToPlace.GetComponent<Collider>().enabled = true;
        system = new BuildingReferenceSystem(objectToPlace);
        toPlaceRenderer.material = originalMat;
        toPlaceRenderer = null;
        objectToPlace = null;
        enabled = false;
        Test();
    }

    protected bool CheckForBlockDockingAndApply(RaycastHit hit)
    {
        IBlockCombiner combiner = hit.collider.GetComponent<IBlockCombiner>();
        if (combiner == null)
            return false;

        Vector3 normal;
        Vector3 forward;
        Vector3 dockPosition;
        Vector3 localOrientation;
        combiner.GetDockOrientation(hit, out dockPosition, out normal, out forward, out localOrientation);
        objectToPlace.position = dockPosition;
        objectToPlace.Translate(Vector3.Scale(currentMeshExtend, localOrientation));
        AlignToTriangle(normal,forward);
        return true;
    }

    protected bool CheckForBlockOrientatorAndApply(RaycastHit hit)
    {
        IBlockPlaceOrientator orientator = hit.collider.gameObject.GetComponent<IBlockPlaceOrientator>();
        if(orientator == null)
            return false;

        AssignToOrientation(orientator, hit);
        return true;
    }

    protected void AssignToOrientation(IBlockPlaceOrientator orientator, RaycastHit hit)
    {
        Vector3 normal;

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
        objectToPlace.position += objectToPlace.up * currentMeshExtend.y;
    }

    protected void AlignToTriangle(Vector3 normal, Vector3 forward)
    {
        objectToPlace.rotation = Quaternion.LookRotation(normal, -forward);
        objectToPlace.Rotate(new Vector3(90, 0, 0), Space.Self);
    }

    protected int activeColliderDocking = 0;

    protected void SearchForCubeConnection()
    {
        Vector3 pos = objectToPlace.position;
        Collider[] results = Physics.OverlapBox(pos, currentMeshExtend, objectToPlace.rotation, BUILDING_BLOCK_LAYER_ID);
        //RaycastHit[] hit;
        //Physics.BoxCastAll()
        if(results != null && results.Length > 0)
        {
            activeColliderDocking = activeColliderDocking % results.Length;
            Collider activeCollider = results[activeColliderDocking];
            RaycastHit hit;
            Vector3 direction = (activeCollider.transform.position - pos).normalized;
            if (direction != Vector3.zero)
            {
                Ray ray = new Ray(pos - direction, direction);
                if (activeCollider.Raycast(ray, out hit, buildRange))
                {
                    CheckForBlockDockingAndApply(hit);
                }
            }
        }
    }


}
