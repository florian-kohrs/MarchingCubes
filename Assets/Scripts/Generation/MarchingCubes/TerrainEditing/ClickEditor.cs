using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ClickEditor : MonoBehaviour
    {

        int pointDelta = -5;

        protected PathTriangle firstTriIndex;
        protected PathTriangle secondTriIndex;
        protected int clickCount = 0;
        MarchingCubeEntity e;
        protected IList<PathTriangle> ps;
        protected PathTriangle p;

        private void OnDrawGizmos()
        {
            if (ps != null)
            {
                foreach (PathTriangle p in ps)
                {
                    if (p != null)
                    {
                        Gizmos.DrawSphere(p.OriginalLocalMiddlePointOfTriangle, 0.4f);
                    }
                }
            }
            if(p != null)
            {
                Gizmos.DrawCube(p.OriginalLocalMiddlePointOfTriangle, Vector3.one * 0.4f);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                pointDelta *= -1;
            }
            if (Input.GetMouseButtonDown(2))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 0.1f);
                if (Physics.Raycast(ray, out hit, 2000))
                {
                    Transform currentHitObject = hit.collider.transform;

                    IMarchingCubeInteractableChunk chunk = currentHitObject.GetComponent<IHasInteractableMarchingCubeChunk>()?.GetChunk;

                    if (chunk != null)
                    {
                        if (clickCount == 0)
                        {
                            firstTriIndex = chunk.GetTriangleFromRayHit(hit);
                        }
                        else
                        {
                            secondTriIndex = chunk.GetTriangleFromRayHit(hit);

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

                    IMarchingCubeInteractableChunk chunk = currentHitObject.GetComponent<IHasInteractableMarchingCubeChunk>()?.GetChunk;

                    if (chunk != null)
                    {
                        //MarchingCubeEntity e2 = chunk.GetClosestEntity(hit.point);

                        PathTriangle tri = chunk.GetTriangleFromRayHit(hit);
                        ps = tri.neighbours;
                        p = tri;

                        // chunk.GetChunkHandler.DecreaseChunkLod(chunk, 1);
                    }
                }

            }
            else if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 0.1f);
                if (Physics.Raycast(ray, out hit, 2000))
                {
                    Transform currentHitObject = hit.collider.transform;

                    IMarchingCubeInteractableChunk chunk = currentHitObject.GetComponent<IHasInteractableMarchingCubeChunk>()?.GetChunk;

                    if (chunk != null)
                    {
                        chunk.EditPointsAroundRayHit(pointDelta, hit, 0);
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