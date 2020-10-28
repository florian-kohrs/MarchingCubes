using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlanetRigidBody : MonoBehaviour
{

    public Transform planet;

    public float maxSpeed = 5;

    public float acceleration = 10;

    public float jumpPower = 9;

    protected Vector3 lastGlobalInputSpeed;

    protected Vector3 globalVelocity;

    public float airTime;

    protected float fallSpeed;

    protected bool Grounded => airTime <= 0;

    protected Rigidbody r;


    void OnCollisionStay()
    {
        airTime = 0;
        fallSpeed = 0;
        lastFallSpeed = 0;
        lastYMovement = 0;
    }

    private void OnCollisionExit(Collision collision)
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        airTime = 0;
        fallSpeed = 0;
        lastFallSpeed = 0;
        lastYMovement = 0;
    }

    void Start()
    {
        r = this.GetOrAddComponent<Rigidbody>();
        r.useGravity = false;
        r.freezeRotation = true;
        r.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        //r.isKinematic = true;
    }

    private void Update()
    {
        if (Grounded && Input.GetButtonDown("Jump"))
        {
            transform.Translate(0, 0.1f, 0);
            lastYMovement += jumpPower;
            airTime = 0.000001f;
        }
    }

    private void FixedUpdate()
    {
        ApplyRotationToPlanetGravity(transform);
        ApplyMovementAndGravity();
        airTime += Time.deltaTime;
    }
    protected float lastFallSpeed;

    protected float lastYMovement;

    protected void ApplyMovementAndGravity()
    {
        ///no delta time is applied to movespeed, as this be handled by the rigidbody moving the object
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        input = input.normalized * Mathf.Min(input.magnitude, 1);
        Vector3 frameSpeed = input * maxSpeed;
        frameSpeed = Vector3.Lerp(lastGlobalInputSpeed, frameSpeed, acceleration * Time.deltaTime);
        frameSpeed.y = lastYMovement;

        Vector3 speedDiff = lastGlobalInputSpeed - frameSpeed;

        lastGlobalInputSpeed = frameSpeed;

        //Vector3 localSpeed = transform.InverseTransformDirection(lastGlobalInputSpeed);

       

        frameSpeed.y -= GetFrameGravityPull();
        lastYMovement = frameSpeed.y;


        Vector3 globalSpeed = transform.TransformDirection(frameSpeed);
        r.velocity = globalSpeed;
    }

    protected float GetFrameGravityPull()
    {
        fallSpeed = GetCurrentGravityPull();

        float fallSpeedDelta = fallSpeed - lastFallSpeed;
        
        lastFallSpeed = fallSpeed;

        return fallSpeedDelta;
    }

    protected float GetCurrentGravityPull()
    {
        return 9.81f * airTime * airTime;
    }

    public void ApplyRotationToPlanetGravity(Transform t)
    {
        t.rotation = Quaternion.LookRotation(t.position - planet.position, -t.forward);
        t.Rotate(new Vector3(90, 0, 0), Space.Self);
    }

}
