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
    }

    void Start()
    {
        r = this.GetOrAddComponent<Rigidbody>();
        r.useGravity = false;
        r.freezeRotation = true;
        r.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        //r.isKinematic = true;
    }


    private void FixedUpdate()
    {
        ApplyRotationToPlanetGravity(transform);
        ApplyMovementAndGravity();
        airTime += Time.deltaTime;
    }

    protected void ApplyMovementAndGravity()
    {
        ///no delta time is applied to movespeed, as this be handled by the rigidbody moving the object
        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3 frameSpeed = input * maxSpeed;
        frameSpeed = Vector3.Lerp(lastGlobalInputSpeed, frameSpeed, acceleration * Time.deltaTime);

        Vector3 speedDiff = lastGlobalInputSpeed - frameSpeed;

        lastGlobalInputSpeed = frameSpeed;

        //Vector3 localSpeed = transform.InverseTransformDirection(lastGlobalInputSpeed);
        fallSpeed -= 9.81f * Time.deltaTime;
        frameSpeed.y = fallSpeed;
        if (Grounded && Input.GetButtonDown("Jump"))
        {
            frameSpeed.y = jumpPower;
        }

        Vector3 globalSpeed = transform.TransformDirection(frameSpeed);
         r.velocity = globalSpeed;
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
