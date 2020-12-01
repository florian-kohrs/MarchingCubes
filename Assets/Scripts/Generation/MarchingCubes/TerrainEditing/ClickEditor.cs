using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickEditor : MonoBehaviour
{

    int sign = 1;

    public bool buildPath = true;

    protected PathTriangle firstTriIndex;
    protected PathTriangle secondTriIndex;
    protected int clickCount = 0;

    protected IList<PathTriangle> ps;

    private void OnDrawGizmos()
    {
        if(ps!= null)
        {
            foreach(PathTriangle p in ps)
            {
                Gizmos.DrawSphere(p.UnrotatedMiddlePointOfTriangle, 0.4f);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            sign *= -1;
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (buildPath)
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
                        if (clickCount == 0)
                        {
                            firstTriIndex = chunk.GetTriangleAt(hit.triangleIndex);
                        }
                        else
                        {
                            secondTriIndex = chunk.GetTriangleAt(hit.triangleIndex);
                            
                            //BuildPath(firstTriIndex, secondTriIndex);
                        }
                        ps = chunk.GetTriangleAt(hit.triangleIndex).neighbours;
                        clickCount++;
                        clickCount = clickCount % 2;
                    }
                }
                
            }
            else
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
                        chunk.EditPointsAroundTriangleIndex(sign, hit.triangleIndex, 0);
                    }
                }
            }
        }

    }

    public void BuildPath(PathTriangle from, PathTriangle to)
    {

        ps = Pathfinder<PathTriangle, PathTriangle>.FindPath(from, from, to, PathAccuracy.Good);
    }

}
