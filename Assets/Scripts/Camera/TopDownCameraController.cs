using UnityEngine;
using UnityEngine.UIElements;

public class TopDownCameraController : CameraController
{

    //protected override void OnStart()
    //{
    //    xRotator = transform.parent;
    //    yRotator = xRotator.parent;
    //}

    protected override void OnCameraUpdate()
    {
    }

    protected override void OnCameraScroll()
    {
        transform.localEulerAngles = Vector3.Lerp(maxCameraScrollAnchor.localEulerAngles, minCameraScrollAnchor.localEulerAngles, scrollZoomValue);
    }

}
