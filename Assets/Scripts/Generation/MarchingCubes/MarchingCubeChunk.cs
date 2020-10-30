using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class MarchingCubeChunk : MonoBehaviour
{

    public MarchingCubeChunkHandler chunkHandler;

    public int ChunkSize => MarchingCubeChunkHandler.CHUNK_SIZE;

    protected MeshFilter meshFilter;
    protected MeshCollider meshCollider;

    protected Vector4[] points;

    public Vector3Int chunkOffset;

    public int VertexSize => ChunkSize + 1;

    protected float surfaceLevel;

    protected Color[] colorData;

    protected List<Triangle> allTriangles;

    protected List<Triangle> AllTriangles
    {
        get
        {
            if (allTriangles == null)
            {
                allTriangles = new List<Triangle>();
                for (int x = 0; x < ChunkSize; x++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        for (int z = 0; z < ChunkSize; z++)
                        {
                            foreach (Triangle t in cubeEntities[x, y, z].triangles)
                            {
                                allTriangles.Add(t);
                            }
                        }
                    }
                }
            }
            return allTriangles;
        }
    }

    protected int triCount;

    protected MarchingCubeEntity[,,] cubeEntities;

    public MarchingCubeEntity[,,] CubeEntities => cubeEntities;

    protected Mesh mesh;

    protected List<MarchingCubeEntity> cubesEntities;

    protected NoiseFilter noiseFilter;

    protected Vector4[] GetCubeCornersForPoint(Vector3Int p)
    {
        return new Vector4[]
        {
            points[IndexFromCoord(p.x, p.y, p.z)],
            points[IndexFromCoord(p.x + 1, p.y, p.z)],
            points[IndexFromCoord(p.x + 1, p.y, p.z + 1)],
            points[IndexFromCoord(p.x, p.y, p.z + 1)],
            points[IndexFromCoord(p.x, p.y + 1, p.z)],
            points[IndexFromCoord(p.x + 1, p.y + 1, p.z)],
            points[IndexFromCoord(p.x + 1, p.y + 1, p.z + 1)],
            points[IndexFromCoord(p.x, p.y + 1, p.z + 1)]
        };
    }

    protected int[] GetCubeCornerIndicesForPoint(Vector3Int p)
    {
        return new int[]
        {
            IndexFromCoord(p.x, p.y, p.z),
            IndexFromCoord(p.x + 1, p.y, p.z),
            IndexFromCoord(p.x + 1, p.y, p.z + 1),
            IndexFromCoord(p.x, p.y, p.z + 1),
            IndexFromCoord(p.x, p.y + 1, p.z),
            IndexFromCoord(p.x + 1, p.y + 1, p.z),
            IndexFromCoord(p.x + 1, p.y + 1, p.z + 1),
            IndexFromCoord(p.x, p.y + 1, p.z + 1)
        };
    }

    public virtual void March(Vector3Int p, Vector4[] points)
    {
        MarchingCubeEntity e = new MarchingCubeEntity();
        e.origin = p;
        Vector4[] cubeCorners = GetCubeCornersForPoint(p);

        int cubeIndex = 0;
        if (cubeCorners[0].w < surfaceLevel) cubeIndex |= 1;
        if (cubeCorners[1].w < surfaceLevel) cubeIndex |= 2;
        if (cubeCorners[2].w < surfaceLevel) cubeIndex |= 4;
        if (cubeCorners[3].w < surfaceLevel) cubeIndex |= 8;
        if (cubeCorners[4].w < surfaceLevel) cubeIndex |= 16;
        if (cubeCorners[5].w < surfaceLevel) cubeIndex |= 32;
        if (cubeCorners[6].w < surfaceLevel) cubeIndex |= 64;
        if (cubeCorners[7].w < surfaceLevel) cubeIndex |= 128;

        for (int i = 0; TriangulationTable.triangulation[cubeIndex][i] != -1; i += 3)
        {
            // Get indices of corner points A and B for each of the three edges
            // of the cube that need to be joined to form the triangle.
            int a0 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i]];
            int b0 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i]];

            int a1 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i + 1]];
            int b1 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i + 1]];

            int a2 = TriangulationTable.cornerIndexAFromEdge[TriangulationTable.triangulation[cubeIndex][i + 2]];
            int b2 = TriangulationTable.cornerIndexBFromEdge[TriangulationTable.triangulation[cubeIndex][i + 2]];

            Triangle tri = new Triangle();

            tri.a0Index = a0;
            tri.a1Index = a1;
            tri.a2Index = a2;
            tri.b0Index = b0;
            tri.b1Index = b1;
            tri.b2Index = b2;
            tri.configIndex = cubeIndex;
            tri.configIndexIndex = i;

            tri.origin = p;

            tri.a = InterpolateVerts(cubeCorners[a0], cubeCorners[b0]);
            tri.b = InterpolateVerts(cubeCorners[a1], cubeCorners[b1]);
            tri.c = InterpolateVerts(cubeCorners[a2], cubeCorners[b2]);
            e.triangles.Add(tri);
            triCount++;

        }
        cubeEntities[p.x, p.y, p.z] = e;
    }

    protected virtual Vector3 InterpolateVerts(Vector4 v1, Vector4 v2)
    {
        Vector3 v = v1.GetXYZ();
        float t = (surfaceLevel - v1.w) / (v2.w - v1.w);
        return v + t * (v2.GetXYZ() - v);
    }


    protected Vector3Int CoordFromIndex(int i)
    {
        return new Vector3Int
           ((i % (VertexSize * VertexSize) % VertexSize)
           , (i % (VertexSize * VertexSize) / VertexSize)
           , (i / (VertexSize * VertexSize))
           );
    }

    protected int IndexFromCoord(int x, int y, int z)
    {
        return z * VertexSize * VertexSize + y * VertexSize + x;
    }

    protected int IndexFromCoord(Vector3Int v)
    {
        return IndexFromCoord(v.x, v.y, v.z);
    }

    protected Mesh Mesh
    {
        get
        {
            if (mesh == null)
            {
                mesh = new Mesh();
                meshFilter = this.GetOrAddComponent<MeshFilter>();
                meshFilter.mesh = mesh;

                meshCollider = GetComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }
            return mesh;
        }
    }

    protected void ResetMesh()
    {
        triCount = 0;
        allTriangles = null;
    }

    protected Material mat;

    public Vector3Int offset;

    public void Initialize(Material mat, float surfaceLevel, Vector3Int offset, Vector3 noiseOffset, INoiseBuilder noise, MarchingCubeChunkHandler chunkHandler)
    {
        cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
        this.offset = offset;
        this.chunkHandler = chunkHandler;
        this.mat = mat;
        ResetMesh();

        this.surfaceLevel = surfaceLevel;
        points = new Vector4[VertexSize * VertexSize * VertexSize];

        noise.BuildNoiseArea(points, offset, noiseOffset, ChunkSize, IndexFromCoord);
        Build();
        //for (int i = 0; i < points.Length; i++)
        //{
        //    Vector4 v4 = points[i];
        //    points[i] = v4;
        //}


    }

    protected void Build()
    {
        Vector3Int v = new Vector3Int();

        for (int x = 0; x < ChunkSize; x++)
        {
            v.x = x;
            for (int y = 0; y < ChunkSize; y++)
            {
                v.y = y;
                for (int z = 0; z < ChunkSize; z++)
                {
                    v.z = z;
                    March(v, points);
                }
            }
        }
        ApplyChanges();
    }

    protected void ApplyChanges()
    {
        Vector3[] vertices = new Vector3[triCount * 3];
        int[] meshTriangles = new int[triCount * 3];
        colorData = new Color[triCount * 3];

        int count = 0;
        foreach (Triangle t in AllTriangles)
        {
            for (int x = 0; x < 3; x++)
            {
                meshTriangles[count * 3 + x] = count * 3 + x;
                vertices[count * 3 + x] = t[x];
                colorData[count * 3 + x] = new Color(0.5471698f, 0.2647888f, 0.1f, 1);
            }
            count++;
        }

        ApplyChangesToMesh(vertices, meshTriangles);
    }

    protected void ApplyChangesToMesh(Vector3[] vertices, int[] meshTriangles)
    {
        Mesh.Clear();

        mesh.vertices = vertices;

        mesh.triangles = meshTriangles;
        mesh.colors = colorData;
        GetComponent<MeshRenderer>().material = mat;
        meshCollider.sharedMesh = mesh;

        mesh.RecalculateNormals();
    }

    public void Rebuild()
    {
        ResetMesh();
        Build();
    }

    public void RebuildAround(MarchingCubeEntity e)
    {
        allTriangles = null;
        triCount -= e.triangles.Count;

        Vector3Int v = new Vector3Int();
        for (int x = e.origin.x - 1; x <= e.origin.x + 1; x++)
        {
            v.x = x;
            for (int y = e.origin.y - 1; y <= e.origin.y + 1; y++)
            {
                v.y = y;
                for (int z = e.origin.z - 1; z <= e.origin.z + 1; z++)
                {
                    v.z = z;
                    if (IsInBounds(v))
                    {
                        March(v, points);
                    }
                }
            }
        }
        ApplyChanges();
    }

    protected bool IsInBounds(Vector3Int v)
    {
        return
            v.x >= 0 && v.x < MarchingCubeChunkHandler.CHUNK_SIZE
            && v.y >= 0 && v.y < MarchingCubeChunkHandler.CHUNK_SIZE
            && v.z >= 0 && v.z < MarchingCubeChunkHandler.CHUNK_SIZE;
    }

    protected bool IsCornerPoint(Vector3 p)
    {
        bool r = p.x % (ChunkSize - 1) == 0
            || p.y % (ChunkSize - 1) == 0
            || p.z % (ChunkSize - 1) == 0;
        return r;
    }

    public void EditPointsAroundTriangleIndex(int sign, int triIndex, int editDistance)
    {
        Triangle t = AllTriangles[triIndex];

        int[] cornerIndices = GetCubeCornerIndicesForPoint(t.origin);
        float delta = sign * 0.25f * Time.deltaTime;

        foreach (int i in cornerIndices)
        {
            Vector4 newV4 = points[i];
            newV4.w += delta;
            points[i] = newV4;
        }

        Vector3 p = CoordFromIndex(triIndex);

        if (IsCornerPoint(t.origin))
        {
            chunkHandler.EditNeighbourChunksAt(this, t.origin, delta);
        }
        RebuildAround(CubeEntities[t.origin.x, t.origin.y, t.origin.z]);
    }


    public void EditPointsNextToChunk(MarchingCubeChunk chunk, MarchingCubeEntity e, Vector3Int offset, float delta)
    {
        int[] cornerIndices = GetCubeCornerIndicesForPoint(e.origin);

        foreach (int index in cornerIndices)
        {
            Vector3Int indexPoint = CoordFromIndex(index);
            Vector3Int pointOffset = new Vector3Int();
            for (int i = 0; i < 3; i++)
            {
                if (offset[i] == 0)
                {
                    pointOffset[i] = 0;
                }
                else
                {
                    int indexOffset = Mathf.CeilToInt((indexPoint[i] / (MarchingCubeChunkHandler.CHUNK_SIZE - 2f)) - 1);
                    pointOffset[i] = -indexOffset;
                }
            }

            if (pointOffset == offset)
            {
                Vector4 newV4 = points[index];
                newV4.w += delta;
                points[index] = newV4;
            }
        }
        RebuildAround(e);
    }





}
