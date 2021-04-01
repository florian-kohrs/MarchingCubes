using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ClickEditor : MonoBehaviour
    {

        int sign = 1;

        protected PathTriangle firstTriIndex;
        protected PathTriangle secondTriIndex;
        protected int clickCount = 0;
        MarchingCubeEntity e;
        protected IList<PathTriangle> ps;

        private void OnDrawGizmos()
        {
            if (ps != null)
            {
                foreach (PathTriangle p in ps)
                {
                    Gizmos.DrawSphere(p.OriginalMiddlePointOfTriangle, 0.4f);

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

                            BuildPath(firstTriIndex, secondTriIndex);
                        }
                        clickCount++;
                        clickCount = clickCount % 2;
                    }
                }

            }
            else if (Input.GetMouseButtonDown(1))
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
                        MarchingCubeEntity e1 = chunk.GetCubeAtTriangleIndex(hit.triangleIndex);
                        MarchingCubeEntity e2 = chunk.GetClosestEntity(hit.point);
                        if (e1 != e2)
                        {
                            Debug.LogError("Wrong cube detected!");
                        }
                        else
                        {
                           // Debug.Log("Right Cube detected");
                            PathTriangle t1 = chunk.GetTriangleAt(hit.triangleIndex);
                            PathTriangle t2 = chunk.GetClosestTriangleFromRayHit(hit);
                            if (t1 != t2)
                            {
                                Debug.LogError("Wrong triangle detected!");
                                float rightMinDistance = (t1.OriginalMiddlePointOfTriangle - hit.point).sqrMagnitude;
                                float wrongMinDistance = (t2.OriginalMiddlePointOfTriangle - hit.point).sqrMagnitude;
                                PathTriangle t3 = chunk.GetClosestTriangleFromRayHit(hit);
                            }
                            else
                            {
                                Debug.Log("Right triangle detected");
                            }
                        }
                        ps = chunk.GetTriangleAt(hit.triangleIndex).neighbours;
                        e = chunk.GetCubeAtTriangleIndex(hit.triangleIndex);
                       // Debug.Log("NeighboursCount:" + e.neighbours.Count);
                    }
                }

            }
            else if (Input.GetMouseButtonDown(2))
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



        public void BuildPath(PathTriangle from, PathTriangle to)
        {
            ps = Pathfinder<PathTriangle, PathTriangle>.FindPath(from, from, to, PathAccuracy.NotSoGoodAnymore);
        }

    }
}