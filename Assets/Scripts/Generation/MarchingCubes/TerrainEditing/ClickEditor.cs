using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickEditor : MonoBehaviour
{

    int sign = 1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            sign *= -1;
        }
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 0.1f);
            if (Physics.Raycast(ray, out hit, 2000))
            {
                Transform currentHitObject = hit.collider.transform;

                MarchingCubeChunk chunk = currentHitObject.GetComponent<MarchingCubeChunk>();

                if (chunk != null)
                {
                    chunk.EditPointsAroundTriangleIndex(sign,hit.triangleIndex,0);
                }
            }
        }

    }

}
