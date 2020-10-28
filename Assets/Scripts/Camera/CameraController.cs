using UnityEngine;

public class CameraController : MonoBehaviour
{

    public float sensitivity = 22.0f;
    public float smoothing = 5.0f;
    public Transform yRotator;
    public Transform xRotator;

    public float maxRotation;
    public float mouseMaxSpeedPerFrame = 10;

    protected Vector2 mouseLook;
    protected Vector2 smoothedMouseLook;

    public float maxDownRotation = -50;

    public float maxUpRotation = 50;

    public float distanceFromPlayer = 3;

    public Transform maxCameraScrollAnchor;

    public Transform minCameraScrollAnchor;

    public float scrollSpeed = 10;

    public float scrollZoomValue = 0;

    protected void Start()
    {
        float startZEuler = xRotator.localEulerAngles.z;
        if (startZEuler > 180)
        {

            startZEuler -= 360;
        }
        startZEuler *= -1;

        mouseLook = new Vector2(xRotator.localEulerAngles.y, startZEuler);
        smoothedMouseLook = mouseLook;
        lastSmothedMouseLook = smoothedMouseLook;
        OnStart();
    }

    protected virtual void OnStart() { }

    protected virtual void OnCameraUpdate() { }

    protected virtual void OnCameraScroll() { }

    protected virtual bool IsCameraRotationEnabled => Input.GetMouseButton(1);

    void LateUpdate()
    {
        //if (GameManager.CanCameraMove)
        {
            RotateCamera();

            ScrollCamera();
            OnCameraUpdate();
        }
    }

    private void ScrollCamera()
    {
        scrollZoomValue = Mathf.Clamp01(scrollZoomValue + Input.mouseScrollDelta.y * Time.deltaTime * scrollSpeed);
        transform.position = Vector3.Lerp(maxCameraScrollAnchor.position, minCameraScrollAnchor.position, scrollZoomValue);
        OnCameraScroll();
    }

    protected  void RotateCamera()
    {
        if (IsCameraRotationEnabled)
        {
            // float sensitivity = PersistentGameDataController.Settings.cameraSensitivity;

            Vector2 mouseMovement = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
            mouseMovement *= sensitivity * smoothing * Time.deltaTime;

            if (mouseMovement.y > 0)
            {
                mouseMovement.y = Mathf.Min(mouseMaxSpeedPerFrame, mouseMovement.y);
            }
            else
            {
                mouseMovement.y = Mathf.Max(-mouseMaxSpeedPerFrame, mouseMovement.y);
            }

            if (mouseMovement.y > 0)
            {
                mouseMovement.y = Mathf.Min(mouseMaxSpeedPerFrame, mouseMovement.y);
            }
            else
            {
                mouseMovement.y = Mathf.Max(-mouseMaxSpeedPerFrame, mouseMovement.y);
            }

            mouseLook += mouseMovement;


            if (mouseLook.y < 0 && mouseLook.y < maxDownRotation)
            {
                mouseLook.y = maxDownRotation;
            }
            else if (mouseLook.y > 0 && mouseLook.y > maxUpRotation)
            {
                mouseLook.y = maxUpRotation;
            }
        }

        smoothedMouseLook.y = Mathf.Lerp(smoothedMouseLook.y, mouseLook.y, 1 / smoothing);
        smoothedMouseLook.x = Mathf.Lerp(smoothedMouseLook.x, mouseLook.x, 1 / smoothing);

        float yDelta = smoothedMouseLook.y - lastSmothedMouseLook.y;

        yRotator.transform.Rotate(-yDelta, 0, 0, Space.Self);

        float xDelta = smoothedMouseLook.x - lastSmothedMouseLook.x;

        xRotator.transform.Rotate(0, xDelta, 0, Space.Self);

        lastSmothedMouseLook = smoothedMouseLook;
    }

    protected Vector2 lastSmothedMouseLook;
}
