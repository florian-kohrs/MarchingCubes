﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ClickEditor : MonoBehaviour
    {

        int pointDelta = -100;

        public MarchingCubeChunkHandler h;

        private void Start()
        {
            layer = LayerMask.GetMask("ChunkApproximation", "ChunkChecker");
            layer = ~layer;
        }

        //private void OnDrawGizmos()
        //{
        //    if (ps != null)
        //    {
        //        foreach (PathTriangle p in ps)
        //        {
        //            if (p != null)
        //            {
        //                Gizmos.DrawSphere(p.MiddlePoint, 0.4f);
        //            }
        //        }
        //    }
        //    if(p != null)
        //    {
        //        Gizmos.DrawCube(p.MiddlePoint, Vector3.one * 0.4f);
        //    }
        //}

        int layer;

        void Update()
        {
           
            if (Input.GetKeyDown(KeyCode.Space))
            {
                pointDelta *= -1;
            }
            if (Input.GetMouseButtonDown(2))
            {
                //RaycastHit hit;
                //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 0.1f);
                //if (Physics.Raycast(ray, out hit, 2000, layer))
                //{
                //    Transform currentHitObject = hit.collider.transform;

                //    ReducedMarchingCubesChunk chunk = currentHitObject.GetComponent<IHasInteractableMarchingCubeChunk>()?.GetChunk;

                //    if (chunk != null)
                //    {
                //        if (clickCount == 0)
                //        {
                //            firstTriIndex = chunk.GetTriangleFromRayHit(hit);
                //        }
                //        else
                //        {
                //            secondTriIndex = chunk.GetTriangleFromRayHit(hit);

                //            BuildPath(firstTriIndex, secondTriIndex);
                //        }
                //        clickCount++;
                //        clickCount = clickCount % 2;
                //    }
                //}

            }
            else if (Input.GetMouseButtonDown(1))
            {
                RaycastHit hit;
               
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 0.1f);
                if (Physics.Raycast(ray, out hit, 2000, layer))
                {
                    Transform currentHitObject = hit.collider.transform;

                    ReducedMarchingCubesChunk chunk = currentHitObject.GetComponent<IHasInteractableMarchingCubeChunk>()?.GetChunk;

                    if (chunk != null)
                    {
                        //MarchingCubeEntity e2 = chunk.GetClosestEntity(hit.point);

                        //PathTriangle tri = chunk.GetTriangleFromRayHit(hit);
                        //ps = tri.Neighbours;
                        //p = tri;

                       // h.DecreaseChunkLod(chunk, chunk.LODPower + 1);

                        // chunk.GetChunkHandler.DecreaseChunkLod(chunk, 1);
                    }
                }

            }
            else if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.DrawRay(ray.origin, ray.direction * 10, Color.red, 0.1f);
                if (Physics.Raycast(ray, out hit, 2000, layer))
                {
                    Transform currentHitObject = hit.collider.transform;

                    ReducedMarchingCubesChunk chunk = currentHitObject.GetComponent<IHasInteractableMarchingCubeChunk>()?.GetChunk;

                    if (chunk != null)
                    {
                        chunk.EditPointsAroundRayHit(pointDelta, hit,6);
                    }
                }
            }
        }

    }
}