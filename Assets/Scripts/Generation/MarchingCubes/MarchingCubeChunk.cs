using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MarchingCubes
{

    //TODO: Dont use as Interactablechunk when destruction begun
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MarchingCubesChunk : ReducedMarchingCubesChunk, ICubeNeighbourFinder
    {


        public MarchingCubeEntity[,,] cubeEntities;

        public HashSet<MarchingCubeEntity> entities = new HashSet<MarchingCubeEntity>();

       
       

        //TODO: Add for each cube entitiy index in mesh and increase next index by entitiy cube count and try to use this when rebuilding mesh
        //TODO: Build from consume buffer

       
        public MarchingCubeEntity GetEntityAt(Vector3Int v3)
        {
            return GetEntityAt(v3.x, v3.y, v3.z);
        }
        public MarchingCubeEntity GetEntityAt(int x, int y, int z)
        {
            return cubeEntities[x, y, z];
        }

        public bool TryGetEntityAt(int x, int y, int z, out MarchingCubeEntity e)
        {
            e = GetEntityAt(x, y, z);
            return e != null;
        }

        public bool HasEntityAt(Vector3Int v)
        {
            return GetEntityAt(v.x, v.y, v.z) != null;
        }

        public bool HasEntityAt(int x, int y, int z)
        {
            return GetEntityAt(x, y, z) != null;
        }

        public void RemoveEntityAt(MarchingCubeEntity e)
        {
            RemoveEntityAt(e.origin.x, e.origin.y, e.origin.z, e);
        }

        public void RemoveEntityAt(int x, int y, int z, MarchingCubeEntity e)
        {
            SetEntityAt(x, y, z, null);
            entities.Remove(e);
        }

        public void SetEntityAt(Vector3Int v3, MarchingCubeEntity e)
        {
            SetEntityAt(v3.x, v3.y, v3.z, e);
        }

        public void AddEntityAt(Vector3Int v, MarchingCubeEntity e)
        {
            AddEntityAt(v.x, v.y, v.z, e);
        }

        public void AddEntityAt(int x, int y, int z, MarchingCubeEntity e)
        {
            entities.Add(e);
            cubeEntities[x, y, z] = e;
        }

        public void SetEntityAt(int x, int y, int z, MarchingCubeEntity e)
        {
            cubeEntities[x, y, z] = e;
        }

        public MarchingCubeEntity GetEntityInNeighbourAt(Vector3Int outsidePos)
        {
            CompressedMarchingCubeChunk chunk;
            if (chunkHandler.TryGetReadyChunkAt(AnchorPos + outsidePos, out chunk))
            {
                if (chunk is MarchingCubesChunk c)
                {
                    Vector3Int pos = outsidePos + AnchorPos - chunk.AnchorPos;
                    return c.GetEntityAt(pos);
                }
            }
            return null;
        }

        protected override void ResetAll()
        {
            base.ResetAll();
            entities.Clear();
            cubeEntities = null;
        }

        protected MarchingCubeEntity CreateAndAddEntityAt(int x, int y, int z, int triangulationIndex)
        {
            MarchingCubeEntity e = new MarchingCubeEntity(this, triangulationIndex);
            e.origin = new Vector3Int(x, y, z);
            AddEntityAt(x, y, z, e);
            return e;
        }


        protected override void RebuildFromTriangleArray(TriangleChunkHeap heap)
        {
            vertsLeft = VertexCount;
            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            TriangleBuilder[] ts = heap.tris;
            MarchingCubeEntity cube;
            cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
            int x, y, z;
            int endIndex = heap.startIndex + heap.triCount;
            for (int i = heap.startIndex; i < endIndex; ++i)
            {
                x = ts[i].x;
                y = ts[i].y;
                z = ts[i].z;
                if (!TryGetEntityAt(x, y, z, out cube))
                {
                    cube = CreateAndAddEntityAt(x, y, z, ts[i].triIndex);
                    SetNeighbourAt(x, y, z);
                }
                PathTriangle pathTri = new PathTriangle(cube, in ts[i].tri, ts[i].color32);
                cube.AddTriangle(pathTri);
                AddTriangleToMeshData(in ts[i], ref usedTriCount, ref totalTreeCount);
            }
        }

        protected void AddFromTriangleArray(TriangleBuilder[] ts)
        {
            int count = ts.Length;
            int x, y, z;
            MarchingCubeEntity cube;

            for (int i = 0; i < count; ++i)
            {
                x = ts[i].x;
                y = ts[i].y;
                z = ts[i].z;
                if (!TryGetEntityAt(x, y, z, out cube))
                {
                    cube = CreateAndAddEntityAt(x, y, z, ts[i].triIndex);
                    SetNeighbourAt(x, y, z);
                }
                //else
                //{
                //    Debug.Log($"didint remove {x} {y} {z} with distance {(new Vector3(x, y, z) - origin).magnitude} with allowed distance {distance}." +
                //        $"triangle has {TriangulationTable.triangulationSizes[ts[i].triIndex]}");
                //}
                PathTriangle pathTri = new PathTriangle(cube, in ts[i].tri, ts[i].color32);
                cube.AddTriangle(pathTri);
            }
        }

        protected void BuildMeshFromCurrentTriangles()
        {
            if (IsEmpty)
                return;

            vertsLeft = VertexCount;

            ResetArrayData();

            int totalTreeCount = 0;
            int usedTriCount = 0;

            MarchingCubeEntity e;
            int count;
            IEnumerator<MarchingCubeEntity> enumerator = entities.GetEnumerator();
            while (enumerator.MoveNext())
            {
                e = enumerator.Current;
                count = e.triangles.Length;
                for (int i = 0; i < count; ++i)
                {
                    AddTriangleToMeshData(e.triangles[i], ref usedTriCount, ref totalTreeCount);
                }
            }

        }

       
     
        protected void RebuildFromNoiseAround(Vector3Int start, Vector3Int end, Vector3Int clickedIndex, float radius)
        {
            float marchDistance = Vector3.one.magnitude + radius + 1;
            float marchSquare = marchDistance * marchDistance;
            int voxelMinus = chunkSize - 1;

            int startX = Mathf.Max(0, start.x - 1);
            int startY = Mathf.Max(0, start.y - 1);
            int startZ = Mathf.Max(0, start.z - 1);

            int posY = clickedIndex.y;
            int posZ = clickedIndex.z;

            int endX = Mathf.Min(voxelMinus, end.x + 1);
            int endY = Mathf.Min(voxelMinus, end.y + 1);
            int endZ = Mathf.Min(voxelMinus, end.z + 1);

            float distanceX = start.x - clickedIndex.x;

            for (int x = startX; x <= endX; x++)
            {
                int distanceY = startY - posY;
                float xx = distanceX * distanceX;
                for (int y = startY; y <= endY; y++)
                {
                    int distanceZ = startZ - posZ;
                    int yy = distanceY * distanceY;
                    for (int z = startZ; z <= endZ; z++)
                    {
                        int zz = distanceZ * distanceZ;
                        float sqrDis = xx + yy + zz;
                        if (sqrDis <= marchSquare)
                        {
                            MarchingCubeEntity cube;
                            if (TryGetEntityAt(x, y, z, out cube))
                            {
                                NumTris -= cube.triangles.Length;
                                RemoveEntityAt(x, y, z, cube);
                            }
                            cube = MarchAt(x, y, z, this);
                            if (cube != null)
                            {
                                AddEntityAt(x, y, z, cube);
                            }
                        }

                        distanceZ++;
                    }
                    distanceY++;
                }
                distanceX++;
            }

            if (!IsEmpty)
            {
                SetSimpleCollider();
            }

            RebuildMesh();
        }

        public void RebuildMesh()
        {
            if (!IsInOtherThread)
            {
                FreeAllMeshes();
            }
            BuildMeshFromCurrentTriangles();
        }


        public PathTriangle GetTriangleFromRayHit(RaycastHit hit)
        {
            MarchingCubeEntity cube = GetClosestEntity(hit.point);
            return cube.GetTriangleWithNormalOrClosest(hit.normal, hit.point);
        }

        protected int LocalCornerIndexToGlobalDelta(int local)
        {
            if (local == 0)
                return 0;
            else if (local == 1)
                return 1;
            else if (local == 2)
                return 1 + sqrPointsPerAxis;
            else if (local == 3)
                return sqrPointsPerAxis;
            else if (local == 4)
                return pointsPerAxis;
            else if (local == 5)
                return pointsPerAxis + 1;
            else if (local == 6)
                return pointsPerAxis + sqrPointsPerAxis + 1;
            else if (local == 7)
                return pointsPerAxis + sqrPointsPerAxis;
            else
                throw new Exception("Invalid value");
        }

        protected Vector3Int LocalCornerIndexToOffset(int local)
        {
            Vector3Int r = new Vector3Int();

            if (local == 1)
            {
                r.x = 1;
            }
            else if (local == 2)
            {
                r.x = 1;
                r.z = 1;
            }
            else if (local == 3)
            {
                r.z = 1;
            }
            else if (local == 4)
            {
                r.y = 1;
            }
            else if (local == 5)
            {
                r.x = 1;
                r.y = 1;
            }
            else if (local == 6)
            {
                r.x = 1;
                r.y = 1;
                r.z = 1;
            }
            else if (local == 7)
            {
                r.y = 1;
                r.z = 1;
            }

            return r;
        }

        protected int FlipIndexInDirection(int pointIndex, Vector3Int dir)
        {
            if (dir.x > 0)
            {
                pointIndex -= vertexSize;
            }
            else if (dir.x < 0)
            {
                pointIndex += vertexSize;
            }
            if (dir.y > 0)
            {
                pointIndex -= pointsPerAxis * vertexSize;
            }
            else if (dir.y < 0)
            {
                pointIndex += pointsPerAxis * vertexSize;
            }
            if (dir.y > 0)
            {
                pointIndex -= sqrPointsPerAxis * vertexSize;
            }
            else if (dir.y < 0)
            {
                pointIndex += sqrPointsPerAxis * vertexSize;
            }
            return pointIndex;
        }

       


        public MarchingCubeEntity GetClosestEntity(Vector3 v3)
        {
            Vector3 rest = v3 - AnchorPos;
            rest /= lod;
            return GetEntityAt((int)rest.x, (int)rest.y, (int)rest.z);
        }

        public MarchingCubeEntity GetEntityFromRayHit(RaycastHit hit)
        {
            return GetClosestEntity(hit.point);
        }


        protected Vector3Int[] GetCurrentCubePositions()
        {
            int length = entities.Count;
            Vector3Int[] result = new Vector3Int[length];
            int index = 0;
            IEnumerator<MarchingCubeEntity> enumerator = entities.GetEnumerator();
            while (enumerator.MoveNext())
            {
                result[index] = enumerator.Current.origin;
                index++;
            }
            return result;
        }

        #region March
        public virtual MarchingCubeEntity MarchAt(int x, int y, int z, ICubeNeighbourFinder chunk, int lod)
        {
            float[] noisePoints = GetNoiseInCornersForPoint(x, y, z, lod);

            int cubeIndex = 0;
            if (noisePoints[0] > MarchingCubeChunkHandler.SURFACE_LEVEL) cubeIndex |= 1;
            if (noisePoints[1] > MarchingCubeChunkHandler.SURFACE_LEVEL) cubeIndex |= 2;
            if (noisePoints[2] > MarchingCubeChunkHandler.SURFACE_LEVEL) cubeIndex |= 4;
            if (noisePoints[3] > MarchingCubeChunkHandler.SURFACE_LEVEL) cubeIndex |= 8;
            if (noisePoints[4] > MarchingCubeChunkHandler.SURFACE_LEVEL) cubeIndex |= 16;
            if (noisePoints[5] > MarchingCubeChunkHandler.SURFACE_LEVEL) cubeIndex |= 32;
            if (noisePoints[6] > MarchingCubeChunkHandler.SURFACE_LEVEL) cubeIndex |= 64;
            if (noisePoints[7] > MarchingCubeChunkHandler.SURFACE_LEVEL) cubeIndex |= 128;

            if (cubeIndex > 0 && cubeIndex < 255)
            {
                int[] cubeCorners = GetCubeCornerArrayForPoint(x, y, z, lod);
                MarchingCubeEntity e = new MarchingCubeEntity(chunk, cubeIndex);
                e.origin = new Vector3Int(x, y, z);

                int[] triangulation = TriangulationTable.triangulation[cubeIndex];
                int count = triangulation.Length;
                for (int i = 0; i < count; i += 3)
                {
                    // Get indices of corner points A and B for each of the three edges
                    // of the cube that need to be joined to form the triangle.
                    int a0 = TriangulationTable.cornerIndexAFromEdge[triangulation[i]];
                    int b0 = TriangulationTable.cornerIndexBFromEdge[triangulation[i]];

                    int a1 = TriangulationTable.cornerIndexAFromEdge[triangulation[i + 1]];
                    int b1 = TriangulationTable.cornerIndexBFromEdge[triangulation[i + 1]];

                    int a2 = TriangulationTable.cornerIndexAFromEdge[triangulation[i + 2]];
                    int b2 = TriangulationTable.cornerIndexBFromEdge[triangulation[i + 2]];

                    Triangle tri = new Triangle(
                        InterpolateVerts(cubeCorners, noisePoints, a0, b0),
                        InterpolateVerts(cubeCorners, noisePoints, a1, b1),
                        InterpolateVerts(cubeCorners, noisePoints, a2, b2));

                    e.AddTriangle(new PathTriangle(e, in tri, Color.red));
                    NumTris++;
                }

                return e;
            }
            else
            {
                return null;
            }
        }

        public virtual MarchingCubeEntity MarchAt(int x, int y, int z, int lod)
        {
            return MarchAt(x, y, z, null, lod);
        }

        public virtual MarchingCubeEntity MarchAt(int x, int y, int z, ICubeNeighbourFinder finder)
        {
            return MarchAt(x, y, z, finder, 1);
        }

        protected Vector3 InterpolateVerts(int[] cubeCorners, float[] points, int startIndex1, int startIndex2)
        {
            int index1 = startIndex1 * 3;
            int index2 = startIndex2 * 3;
            float t = (points[startIndex1]) / (points[startIndex2] - points[startIndex1]);
            return new Vector3(
                cubeCorners[index1] + t * (cubeCorners[index2] - cubeCorners[index1]),
                cubeCorners[index1 + 1] + t * (cubeCorners[index2 + 1] - cubeCorners[index1 + 1]),
                cubeCorners[index1 + 2] + t * (cubeCorners[index2 + 2] - cubeCorners[index1 + 2]));
        }

        protected int[] GetCubeCornerArrayForPoint(int x, int y, int z, int spacing)
        {
            Vector3Int v3 = AnchorPos;
            x *= lod;
            y *= lod;
            z *= lod;
            x += v3.x;
            y += v3.y;
            z += v3.z;

            int offset = spacing * lod;
            return new int[]
            {
                x, y, z,
                x + offset, y,z,
                x + offset, y, z + offset,
                x, y, z + offset,
                x, y + offset, z,
                x + offset, y + offset, z,
                x + offset, y + offset, z + offset,
                x, y + offset, z + offset
            };
        }

        protected float[] GetNoiseInCornersForPoint(int x, int y, int z, int lod)
        {
            int pointsLod = pointsPerAxis * lod;
            int sqrPointsLod = sqrPointsPerAxis * lod;
            int pointIndex = PointIndexFromCoord(x, y, z);
            return new float[]
            {
                points[pointIndex],
                points[pointIndex + lod],
                points[pointIndex + lod + sqrPointsLod],
                points[pointIndex + sqrPointsLod],
                points[pointIndex + pointsLod],
                points[pointIndex + lod + pointsLod],
                points[pointIndex + lod + pointsLod + sqrPointsLod],
                points[pointIndex + pointsLod + sqrPointsLod]
            };
        }

        #endregion


    }
}
