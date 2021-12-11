using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingReferenceSystem
{

    public BuildingReferenceSystem() { }

    public BuildingReferenceSystem(Transform t)
    {
        origin = t.position;
        front = t.forward;
        up = t.up;
        right = t.right;

        transposedRight = new Vector3(right.x, up.x, front.x);
        transposedUp = new Vector3(right.y, up.y, front.y);
        transposedFront = new Vector3(right.z, up.z, front.z);
    }

    public Vector3 up;
    public Vector3 front;
    public Vector3 right;

    private Vector3 transposedUp;
    private Vector3 transposedFront;
    private Vector3 transposedRight;

    public Vector3 origin;

    public void AssignToBuildingReference(Transform t, Vector3 newPosition)
    {
        RotateToReferenceSystem(t);
        PositionAtReferenceSystem(t, newPosition);
    }

    protected void PositionAtReferenceSystem(Transform t, Vector3 newPosition)
    {
        Vector3Int localPos = PositionToReferenceGridPosition(newPosition);
        Vector3 globalPosition = new Vector3(
            Vector3.Dot(transposedRight, localPos),
            Vector3.Dot(transposedUp, localPos),
            Vector3.Dot(transposedFront, localPos));
        globalPosition += origin;
        t.position = globalPosition;
    }

    protected Vector3Int PositionToReferenceGridPosition(Vector3 pos)
    {
        Vector3 localPos = pos - origin;

        float x = Vector3.Dot(right, localPos);
        float y = Vector3.Dot(up, localPos);
        float z = Vector3.Dot(front, localPos);

        return new Vector3Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y), Mathf.RoundToInt(z));
    }

    protected void RotateToReferenceSystem(Transform t)
    {
        t.rotation = Quaternion.LookRotation(up, -front);
        t.Rotate(new Vector3(90, 0, 0), Space.Self);
    }

}
