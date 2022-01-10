using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class PlanetCharacterController : MonoBehaviour
{
    
    CapsuleCollider capsuleCollider;

    CapsuleCollider CapsuleCollider
    {
        get
        {
            if (capsuleCollider == null)
            {
                capsuleCollider = GetComponent<CapsuleCollider>();
            }
            return capsuleCollider;
        }
    }

    public Transform planet;

    public float stepOffset = 0.3f;

    public float slopeLimit = 45;

    public float moveSpeed = 5;

    public float colliderHeight = 2;

    protected const float EPSILON = 0.000001f;

    public float skinWidth = 0.05f;

    public float colliderRadius = 1;

    protected float centerOffset;

    protected const string DEFAULT_LAYER_NAME = "Default";

    public float gravityPull = 9.81f;

    protected int COLLISION_LAYERS;

    [SerializeField]
    protected float airTime = 0;

    protected float fallSpeed = 0;

    protected void OnValidate()
    {
        colliderRadius = Mathf.Min(colliderRadius, colliderHeight / 4);
        CapsuleCollider.radius = colliderRadius;
        CapsuleCollider.height = colliderHeight;
        centerOffset = GetCenterOffset();
    }

    private void Start()
    {
        centerOffset = GetCenterOffset();
        COLLISION_LAYERS = LayerMask.GetMask(DEFAULT_LAYER_NAME);
    }

    protected void Update()
    {
        float z = Input.GetAxis("Vertical");
        float x = Input.GetAxis("Horizontal");
        Vector3 direction = transform.forward * z + transform.right * x;
        Move(direction, Mathf.Clamp01(direction.magnitude) * Time.deltaTime * moveSpeed);
        //AlignToPlanet();
        if(!SetOnGround())
        {
            ApplyGravity();
        }
    }

    protected void ApplyGravity()
    {
        airTime += Time.deltaTime;
        fallSpeed += airTime * gravityPull * Time.deltaTime;
        MoveOnGravity(fallSpeed * Time.deltaTime);
    }

    protected void MoveOnGravity(float distance)
    {
        float heightOffset = colliderHeight / 2;
        if (Physics.SphereCast(transform.position, colliderRadius, -transform.up, out RaycastHit hit, distance + heightOffset, COLLISION_LAYERS))
        {
            transform.position = hit.point + transform.up * heightOffset;

            Vector3 pointOnCollider = capsuleCollider.ClosestPoint(hit.point);
            Vector3 dir = hit.point - pointOnCollider;

            EndFall();
        }
        else
        {
            transform.position -= transform.up * distance;
        }
    }

    protected void EndFall()
    {
        airTime = 0;
        fallSpeed = 0; 
    }

    protected void Move(Vector3 direction, float restFrameSpeed)
    {
        Move(direction, restFrameSpeed, stepOffset);
    }

    protected void Move(Vector3 direction, float restFrameSpeed, float restStepUpDistance)
    {
        float freeDistance = DistanceInDirection(direction, restFrameSpeed);
        
        restFrameSpeed -= freeDistance;
        transform.position += direction * freeDistance;
        if(restFrameSpeed > EPSILON && restStepUpDistance > EPSILON)
        {
            freeDistance = DistanceInDirection(transform.up, restStepUpDistance);
            restStepUpDistance -= freeDistance;
            //restFrameSpeed -= freeDistance;
            transform.position += transform.up * freeDistance;
            if (restFrameSpeed > EPSILON)
            { 
                Move(direction, restFrameSpeed, restStepUpDistance); 
            }
        }
    }

    protected bool SetOnGround()
    {
        if(DistanceInDirection(-transform.up, stepOffset, out float distance))
        {
            transform.position -= transform.up * distance;
            return true;
        }
        return false;
    }

    protected float DistanceInDirection(Vector3 dir, float maxLength)
    {
        DistanceInDirection(dir, maxLength,out float distance);
        return distance;
    }

    protected bool DistanceInDirection(Vector3 dir, float maxLength, out float distance)
    {
        bool didHit = false;
        distance = maxLength + skinWidth;
        Vector3 lowerPoint = transform.position - transform.up * centerOffset;
        Vector3 upperPoint = transform.position + transform.up * centerOffset;
        if (Physics.CapsuleCast(lowerPoint, upperPoint, colliderRadius - skinWidth, dir, out RaycastHit hit, maxLength, COLLISION_LAYERS))
        {
            didHit = true;
            Vector3 pointOnCollider = capsuleCollider.ClosestPoint(hit.point);
            distance = (hit.point - pointOnCollider).magnitude;
        }
        ///substract skin width from distance since the collider was shrinked by skinWidth and max range was increases by it
        distance = Mathf.Max(0, distance - skinWidth);
        return didHit;
    }

    protected void AlignToPlanet()
    {
        transform.rotation = transform.rotation * Quaternion.FromToRotation(transform.up, transform.position - planet.position);
    }

    protected float GetCenterOffset()
    {
        return colliderHeight / 2 - colliderRadius;
    }


    protected Vector3 GetBottom()
    {
        return transform.position - transform.up * colliderHeight / 2;
    }

}
