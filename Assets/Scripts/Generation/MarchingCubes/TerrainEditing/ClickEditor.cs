﻿using System.Collections;
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