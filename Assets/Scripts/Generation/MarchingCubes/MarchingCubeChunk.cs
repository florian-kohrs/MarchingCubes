using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class MarchingCubeChunk : MonoBehaviour
{

    public int ChunkSize => MarchingCubeChunkHandler.CHUNK_SIZE;

    protected MeshFilter meshFilter;
    protected MeshCollider meshCollider;


    Vector4[] points;

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
        float t = (surfaceLevel - v1.w) / (v2.w - v1.w);
        return v1.GetXYZ() + t * (v2.GetXYZ() - v1.GetXYZ());
    }

    protected int IndexFromCoord(int x, int y, int z)
    {
        return z * VertexSize * VertexSize + y * VertexSize + x;
    }

    protected int IndexFromCoord(Vector3Int v)
    {
        return IndexFromCoord(v.x, v.y, v.z);
    }

    protected void ResetMesh()
    {
        meshCollider = GetComponent<MeshCollider>();
        mesh = new Mesh();
        meshFilter = this.GetOrAddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        triCount = 0;
        cubeEntities = new MarchingCubeEntity[ChunkSize, ChunkSize, ChunkSize];
        allTriangles = null;
    }

    protected Material mat;

    public void Initialize(Material mat, float surfaceLevel, Vector3Int offset, Vector3 noiseOffset, PlanetMarchingCubeNoise noise)
    {
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

        mesh.Clear();
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

    public void EditPointsAroundTriangleIndex(int sign, int triIndex)
    {
        Triangle t = AllTriangles[triIndex];

        foreach(int i in GetCubeCornerIndicesForPoint(t.origin))
        {
            Vector4 newV4 = points[i];
            newV4.w += sign * 0.1f;
            points[i] = newV4;
        }

        Rebuild();
    }

}
