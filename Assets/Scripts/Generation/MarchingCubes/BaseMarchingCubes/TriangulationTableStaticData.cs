using MarchingCubes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TriangulationTableStaticData : MonoBehaviour
{

    private class CubeRepresentation
    {

        public CubeRepresentation(int index)
        {
            int count = index;
            for (int i = Length - 1; i >= 0 && count > 0; i--)
            {
                int currentValue = (int)Mathf.Pow(2, i);
                if (count >= currentValue)
                {
                    this[i] = true;
                    count -= currentValue;
                }
            }
            if (count > 0)
            {
                throw new Exception("Index breakdown didnt trigger");
            }
        }

        public int CubeIndex
        {
            get
            {
                int cubeIndex = 0;
                if (this[0]) cubeIndex |= 1;
                if (this[1]) cubeIndex |= 2;
                if (this[2]) cubeIndex |= 4;
                if (this[3]) cubeIndex |= 8;
                if (this[4]) cubeIndex |= 16;
                if (this[5]) cubeIndex |= 32;
                if (this[6]) cubeIndex |= 64;
                if (this[7]) cubeIndex |= 128;
                return cubeIndex;
            }
        }

        public void MirrorRepresentation(MirrorAxis axis)
        {
            if (axis == MirrorAxis.X)
            {
                Swap(RotateOnX);
            }
            else if (axis == MirrorAxis.Y)
            {
                Swap(RotateOnY);
            }
            else
            {
                Swap(RotateOnZ);
            }
        }

        public bool v0;
        public bool v1;
        public bool v2;
        public bool v3;
        public bool v4;
        public bool v5;
        public bool v6;
        public bool v7;

        int Length => 8;


        public void Swap(Func<int, int> f)
        {
            Dictionary<int, bool> changes = new Dictionary<int, bool>();

            for (int i = 0; i < Length; ++i)
            {
                if (this[i])
                {
                    changes[f(i)] = this[i];
                    this[i] = false;
                }
            }

            foreach (KeyValuePair<int, bool> pair in changes)
            {
                this[pair.Key] = pair.Value;
            }
        }

        public static int RotateOnX(int i)
        {
            if (i == 1 || i == 5 || i == 3 || i == 7)
            {
                return i - 1;
            }
            else
            {
                return i + 1;
            }
        }

        public static int RotateOnY(int i)
        {
            if (i >= 4)
            {
                return i - 4;
            }
            else
            {
                return i + 4;
            }
        }



        public static int RotateOnZ(int i)
        {
            if (i == 1 || i == 5)
            {
                return i + 1;
            }
            else if (i == 6 || i == 2)
            {
                return i - 1;
            }
            else if (i == 7 || i == 3)
            {
                return i - 3;
            }
            else
            {
                return i + 3;
            }
        }

        public bool this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return v0;
                    case 1:
                        return v1;
                    case 2:
                        return v2;
                    case 3:
                        return v3;
                    case 4:
                        return v4;
                    case 5:
                        return v5;
                    case 6:
                        return v6;
                    default:
                        return v7;
                }
            }
            set
            {
                switch (i)
                {
                    case 0:
                        v0 = value;
                        break;
                    case 1:
                        v1 = value;
                        break;
                    case 2:
                        v2 = value;
                        break;
                    case 3:
                        v3 = value;
                        break;
                    case 4:
                        v4 = value;
                        break;
                    case 5:
                        v5 = value;
                        break;
                    case 6:
                        v6 = value;
                        break;
                    default:
                        v7 = value;
                        break;
                }
            }
        }

    }


    public static int GetEdgeIndex(int triangulationIndex, int triIndex, int edgeValue)
    {
        for (int i = 0; i < 3; ++i)
        {
            if (TriangulationTable.triangulation[triangulationIndex][triIndex * 3 + i] == edgeValue)
                return i;

        }
        throw new Exception("Edge value "
            + edgeValue
            + " not found in TriangulationTable.triangulation index "
            + triangulationIndex
            + " at triangle number "
            + triIndex);
    }

    public static Vector2Int RotateEdgeOn(Vector2Int edge, MirrorAxis axis)
    {
        return new Vector2Int(RotateEdgeIndexOn(edge.x, axis), RotateEdgeIndexOn(edge.y, axis));
    }

    public static Vector2Int RotateEdgeOn(int edge1, int edge2, MirrorAxis axis)
    {
        return new Vector2Int(RotateEdgeIndexOn(edge1, axis), RotateEdgeIndexOn(edge2, axis));
    }

    public static int RotateEdgeIndexOn(int edgeIndex, MirrorAxis axis)
    {
        int result = edgeIndex;
        if (axis == MirrorAxis.X)
        {
            if (edgeIndex == 1 || edgeIndex == 5)
                result = edgeIndex + 2;
            else if (edgeIndex == 10)
                result = 11;
            else if (edgeIndex == 9)
                result = 8;
            else if (edgeIndex == 3 || edgeIndex == 7)
                result = edgeIndex - 2;
            else if (edgeIndex == 11)
                result = 10;
            else if (edgeIndex == 8)
                result = 9;
        }
        else if (axis == MirrorAxis.Y)
        {
            if (edgeIndex >= 0 && edgeIndex < 4)
                result = edgeIndex + 4;
            else if (edgeIndex < 8)
                result = edgeIndex - 4;
        }
        else
        {
            if (edgeIndex == 2 || edgeIndex == 6)
                result = edgeIndex - 2;
            else if (edgeIndex == 11)
                result = 8;
            else if (edgeIndex == 10)
                result = 9;
            else if (edgeIndex == 0 || edgeIndex == 4)
                result = edgeIndex + 2;
            else if (edgeIndex == 8)
                result = 11;
            else if (edgeIndex == 9)
                result = 10;
        }
        return result;
    }


    public enum MirrorAxis { X = 1, Y = 255, Z = 6500 }

    //public static bool GetNeighbourIndexIn(int fromIndex, int fromTriIndex, int toIndex, out int result, MirrorAxis shiftedOnAxis)
    //{
    //    CubeRepresentation cube = new CubeRepresentation(toIndex);
    //    cube.MirrorRepresentation(shiftedOnAxis);
    //    return GetNeighbourIndexIn(fromIndex, fromTriIndex, cube.CubeIndex, out result);
    //}


    public static int RotateIndex(int triangulationIndex, MirrorAxis axis)
    {
        CubeRepresentation cube = new CubeRepresentation(triangulationIndex);
        cube.MirrorRepresentation(axis);
        return cube.CubeIndex;
    }

    public static int RotateCornerIndex(int cornerIndex, Vector3Int dir)
    {
        return RotateCornerIndexOnAxis(cornerIndex, AxisFromDir(dir));
    }

    public static MirrorAxis AxisFromDir(Vector3Int dir)
    {
        if (dir.x == 0)
            return MirrorAxis.X;
        if (dir.y == 0)
            return MirrorAxis.Y;
        else
            return MirrorAxis.Z;
    }

    //public static bool GetNeighbourIndexIn(int fromIndex, int fromTriIndex, int toIndex, out int result)
    //{
    //    return NeighbourTable.TryGetValue(new NeighbourKey(fromIndex, fromTriIndex, toIndex), out result);
    //}

    public static TriangulationNeighbours GetNeighbourData(int triangulationIndex)
    {
        TriangulationNeighbours neighbours;
        NeigbourInformation.TryGetValue(triangulationIndex, out neighbours);
        return neighbours;
    }

    protected static long BuildLong(int i1, int i2)
    {
        return ((long)i1 << 32) + i2;
    }


    private const int SAME_VERTICES_TO_BE_NEIGHBOURS = 2;

    /// key is combination of triangulationindex and triangleindex
    /// </summary>
    protected static Dictionary<int, TriangulationNeighbours> internNeighbours;

    protected static Dictionary<int, OutsideNeighbourConnectionInfo> externNeighboursLookup = new Dictionary<int, OutsideNeighbourConnectionInfo>();

    public static Vector3Int GetTriangleAt(int trianuglationIndex, int triIndex)
    {
        return new Vector3Int(
            TriangulationTable.triangulation[trianuglationIndex][triIndex * 3],
            TriangulationTable.triangulation[trianuglationIndex][triIndex * 3 + 1],
            TriangulationTable.triangulation[trianuglationIndex][triIndex * 3 + 2]);
    }

    public static Vector2Int[] GetEdges(Vector3Int v3)
    {
        Vector2Int[] edges = new Vector2Int[3];
        edges[0] = new Vector2Int(v3.x, v3.y);
        edges[1] = new Vector2Int(v3.y, v3.z);
        edges[2] = new Vector2Int(v3.z, v3.x);
        return edges;
    }

    public static void GetNeighbourForAllPossibleNeighbours(int triangulationIndex, int index, List<OutsideEdgeNeighbourDirection> addResult)
    {
        for (int i = 0; i < 3; ++i)
        {
            Vector3Int offset = Vector3Int.zero;
            Vector2Int edgeVertices = new Vector2Int(TriangulationTable.triangulation[triangulationIndex][index + i], TriangulationTable.triangulation[triangulationIndex][index + ((i + 1) % 3)]);
            GetEdgeAxisDirection(ref offset, edgeVertices.x);
            GetEdgeAxisDirection(ref offset, edgeVertices.y);
            //r = r.Map(f => { if (Mathf.Abs(f) == 2)  return (int)Mathf.Sign(f) * 1;  else  return 0;  });
            offset = new Vector3Int(
                Mathf.Abs(offset.x) == 2 ? (int)Mathf.Sign(offset.x) : 0,
                 Mathf.Abs(offset.y) == 2 ? (int)Mathf.Sign(offset.y) : 0,
                  Mathf.Abs(offset.z) == 2 ? (int)Mathf.Sign(offset.z) : 0);
            if (offset != Vector3.zero)
            {
                //try find edge for every other comb if exists
                OutsideEdgeNeighbourDirection neighbour = new OutsideEdgeNeighbourDirection(index / 3, edgeVertices.x, edgeVertices.y, i, (i + 1) % 3, offset);
                addResult.Add(neighbour);
                
                for (int otherTriIndex = 1; otherTriIndex < 255; otherTriIndex++)
                {
                    OutsideNeighbourConnectionInfo info;
                    if (TryGetIndexWithEdges(otherTriIndex, neighbour.rotatedEdgePair.x, neighbour.rotatedEdgePair.y, out info))
                    {
                        int key = BuildIndexForOutsideNeighbour(triangulationIndex, otherTriIndex, index, i, (i + 1) % 3);
                        externNeighboursLookup[key] = info;
                    }
                }
            }
        }
    }

    protected const int TRIANGULATION_ENTRY_SIZE = 15;

    public static Vector2Int RotateVector2OnDelta(Vector3Int delta, Vector2Int v2)
    {
        IEnumerable<int> r = RotateValuesOnDelta(delta, v2.x, v2.y);
        return new Vector2Int(r.First(), r.Last());
    }


    public static IEnumerable<int> RotateValuesOnDelta(Vector3Int delta, params int[] @is)
    {
        if (delta.x != 0)
        {
            return RotateValuesOnAxis(@is, MirrorAxis.X);
        }
        else if (delta.y != 0)
        {

            return RotateValuesOnAxis(@is, MirrorAxis.Y);
        }
        else if (delta.z != 0)
        {

            return RotateValuesOnAxis(@is, MirrorAxis.Z);
        }
        else
        {
            return @is;
        }
    }

    public static MirrorAxis GetAxisFromDelta(Vector3Int delta)
    {
        if (delta.x != 0)
        {
            return MirrorAxis.X;
        }
        else if (delta.y != 0)
        {
            return MirrorAxis.Y;
        }
        else
        {
            return MirrorAxis.Z;
        }
    }


    public static IEnumerable<int> RotateValuesOnAxis(IEnumerable<int> @is, MirrorAxis axis)
    {
        System.Func<int, int> f;
        if (axis == MirrorAxis.X)
        {
            f = CubeRepresentation.RotateOnX;
        }
        else if (axis == MirrorAxis.Y)
        {
            f = CubeRepresentation.RotateOnY;
        }
        else
        {
            f = CubeRepresentation.RotateOnZ;
        }
        return @is.Select(f);
    }

    public static int RotateCornerIndexOnAxis(int cornerIndex, MirrorAxis axis)
    {
        if (axis == MirrorAxis.X)
        {
            return CubeRepresentation.RotateOnX(cornerIndex);
        }
        else if (axis == MirrorAxis.Y)
        {
            return CubeRepresentation.RotateOnY(cornerIndex);
        }
        else
        {
            return CubeRepresentation.RotateOnZ(cornerIndex);
        }
    }

    public static bool TryGetNeighbourTriangleIndex(int triangulationIndex, int otherTriangulationIndex, int triangleIndex, int edge1, int edge2, out OutsideNeighbourConnectionInfo result)
    {
        int key = BuildIndexForOutsideNeighbour(triangulationIndex, otherTriangulationIndex, triangleIndex, edge1, edge2);
        return externNeighboursLookup.TryGetValue(key, out result);
    }

    public static OutsideNeighbourConnectionInfo GetIndexWithEdges(int index, Vector2Int edge)
    {
        return GetIndexWithEdges(index, edge.x, edge.y);
    }

    protected static Dictionary<int, OutsideNeighbourConnectionInfo> indexWithEdges = new Dictionary<int, OutsideNeighbourConnectionInfo>(1000);

    protected static int BuildIndexWithEdgeKey(int a, int b, int c) => (a << 16) + (b << 8) + c;


    protected static int BuildIndexForOutsideNeighbour(int triangulationIndex, int otherTriangulationIndex, int triIndex, int edge1, int edge2) => 
        (edge2 << 28) + (edge1 << 24) + (triangulationIndex << 16) + (otherTriangulationIndex << 8) + triIndex;



    public static bool TryGetIndexWithEdges(int index, int edge1, int edge2, out OutsideNeighbourConnectionInfo result)
    {

        int key = BuildIndexWithEdgeKey(index, edge1, edge2);
        Vector3 edge = new Vector3(edge1, edge2, -1);
        bool found = false;

        if (indexWithEdges.TryGetValue(key, out result))
            return true;

        result = new OutsideNeighbourConnectionInfo();
        Vector3 v = new Vector3Int();
        int[] triangulation = TriangulationTable.triangulation[index];
        for (int i = 0; i < TRIANGULATION_ENTRY_SIZE && triangulation[i] >= 0; i += 3)
        {
            v.x = triangulation[i];
            v.y = triangulation[i + 1];
            v.z = triangulation[i + 2];
            Vector2Int sharedIndices;
            if (v.SharesExactThisNValuesWith(edge, out sharedIndices, SAME_VERTICES_TO_BE_NEIGHBOURS))
            {
                result.outsideNeighbourEdgeIndicesX = sharedIndices.x;
                result.outsideNeighbourEdgeIndicesY = sharedIndices.y;
                result.otherTriangleIndex = i / 3;
                found = true;
                break;
            }
        }
        if (found)
        {
            indexWithEdges[key] = result;
        }
        return found;
    }

    public static OutsideNeighbourConnectionInfo GetIndexWithEdges(int index, int edge1, int edge2)
    {
        OutsideNeighbourConnectionInfo result;


        if (!TryGetIndexWithEdges(index, edge1, edge2, out result))
        {
            throw new Exception("no triangle found in " + index + " with the edges " + edge1 + "," + edge2);
        }

        return result;
    }

    public static bool TryGetIndexWithEdges(int index, Vector2Int edge, out int result)
    {
        return TryGetIndexWithEdges(index, edge.x, edge.y, out result);
    }

    public static bool TryGetIndexWithEdges(int index, int edge1, int edge2, out int result)
    {
        result = -1;
        Vector3 v = new Vector3Int();
        int[] triangulation = TriangulationTable.triangulation[index];
        for (int i = 0; i < TRIANGULATION_ENTRY_SIZE && triangulation[i] >= 0 && result < 0; i += 3)
        {
            v.x = triangulation[i];
            v.y = triangulation[i + 1];
            v.z = triangulation[i + 2];
            if (v.SharesExactNValuesWith(new Vector3(edge1, edge2, -1), 2))
            {
                result = i / 3;
            }
        }
        return result >= 0;
    }

    protected static void GetEdgeAxisDirection(ref Vector3Int v3, int edge)
    {
        if (edge < 4 && edge >= 0)
        {
            v3.y--;
        }
        else if (edge >= 4 && edge < 8)
        {
            v3.y++;
        }
        if (edge == 7 || edge == 8 || edge == 11 || edge == 3)
        {
            v3.x--;
        }
        else if (edge == 5 || edge == 1 || edge == 10 || edge == 9)
        {
            v3.x++;
        }
        if (edge == 11 || edge == 10 || edge == 6 || edge == 2)
        {
            v3.z++;
        }
        else if (edge == 4 || edge == 8 || edge == 0 || edge == 9)
        {
            v3.z--;
        }
    }

    //public static Dictionary<NeighbourKey, int> NeighbourTable
    //{
    //    get
    //    {
    //        if (neighbourTable == null)
    //        {
    //            BuildNeighbourTable();
    //        }
    //        return neighbourTable;
    //    }
    //}

    public static Dictionary<int, TriangulationNeighbours> NeigbourInformation
    {
        get
        {
            if (internNeighbours == null)
            {
                BuildInternNeighbours();
            }
            return internNeighbours;
        }
    }

    protected static void BuildInternNeighbours()
    {
        internNeighbours = new Dictionary<int, TriangulationNeighbours>();
        for (int i = 1; i < TriangulationTable.triangulation.Count - 1; ++i)
        {
            TriangulationNeighbours currentNeighbours = new TriangulationNeighbours();
            internNeighbours.Add(i, currentNeighbours);
            //also stop when 3 neighbours are found
            for (int triIndex1 = 0; triIndex1 < 15 && TriangulationTable.triangulation[i][triIndex1] >= 0; triIndex1 += 3)
            {
                int firstIndex = triIndex1 / 3;

                GetNeighbourForAllPossibleNeighbours(i, triIndex1, currentNeighbours.OutsideNeighbours);

                Vector3 v1 = new Vector3(
                       TriangulationTable.triangulation[i][triIndex1],
                       TriangulationTable.triangulation[i][triIndex1 + 1],
                       TriangulationTable.triangulation[i][triIndex1 + 2]);
                for (int triIndex2 = triIndex1 + 3; triIndex2 < 15 && TriangulationTable.triangulation[i][triIndex2] >= 0; triIndex2 += 3)
                {
                    int secondIndex = triIndex2 / 3;
                    Vector3 v2 = new Vector3(
                         TriangulationTable.triangulation[i][triIndex2],
                         TriangulationTable.triangulation[i][triIndex2 + 1],
                         TriangulationTable.triangulation[i][triIndex2 + 2]);

                    Vector3Int v1ConnectedIndices;
                    Vector3Int v2ConnectedIndices;

                    if (v2.CountAndMapIndiciesWithSameValues(v1, out v1ConnectedIndices, out v2ConnectedIndices) >= SAME_VERTICES_TO_BE_NEIGHBOURS)
                    {
                        AddInternNeighbour(firstIndex, secondIndex, v1ConnectedIndices.ReduceToVector2(f => f > 0), v2ConnectedIndices.ReduceToVector2(f => f > 0), currentNeighbours.InternNeighbourPairs);
                    }
                }
            }
        }
    }

    public static int ActiveTrianglesAt(int i)
    {
        int result = 0;
        for (; result < 15 && TriangulationTable.triangulation[i][result] >= 0; result += 3)
        {
        }
        return result;
    }


    protected static void AddInternNeighbour(int first, int snd, Vector2Int firstEdge, Vector2Int sndEdge, List<IndexNeighbourPair> addHere)
    {
        IndexNeighbourPair newPair = new IndexNeighbourPair(first, snd, firstEdge, sndEdge);
        addHere.Add(newPair);
    }

    /// <summary>
    /// unfinished and unused however could potentially speed up finding neighbours a lot
    /// </summary>
    //protected static void BuildNeighbourTable()
    //{
    //    neighbourTable = new Dictionary<NeighbourKey, int>();
    //    NeighbourKey key1;
    //    NeighbourKey key2;

    //    for (int x = 0; x < TriangulationTable.triangulation.Count - 1; x++)
    //    {
    //        key1.fromIndex = x;
    //        key2.toIndex = x;
    //        for (int y = x; y < TriangulationTable.triangulation.Count; y++)
    //        {
    //            key1.toIndex = y;
    //            key2.fromIndex = y;
    //            for (int i1 = 0; TriangulationTable.triangulation[y][i1] != -1; i1 += 3)
    //            {
    //                key1.fromTriIndex = i1 / 3;

    //                Vector3 v1 = new Vector3(
    //                   TriangulationTable.triangulation[x][i1],
    //                   TriangulationTable.triangulation[x][i1 + 1],
    //                   TriangulationTable.triangulation[x][i1 + 2]);

    //                for (int i2 = 0; TriangulationTable.triangulation[y][i2] != -1; i2 += 3)
    //                {
    //                    Vector3 v2 = new Vector3(
    //                        TriangulationTable.triangulation[y][i2],
    //                        TriangulationTable.triangulation[y][i2 + 1],
    //                        TriangulationTable.triangulation[y][i2 + 2]);

    //                    Vector3Int v3_1;
    //                    Vector3Int v3_2;

    //                    if (v2.CountAndMapIndiciesWithSameValues(v1, out v3_1, out v3_2) >= SAME_VERTICES_TO_BE_NEIGHBOURS)
    //                    {
    //                        key2.fromTriIndex = i2 / 3;

    //                        Add(key2, key1.fromTriIndex);
    //                        Add(key1, key2.fromTriIndex);

    //                    }
    //                }
    //            }
    //        }
    //    }
    //}




    public static readonly List<Vector3Int> offsetFromCornerIndex = new List<Vector3Int>(8)
    {
        new Vector3Int(0,0,0),
        new Vector3Int(1,0,0),
        new Vector3Int(1,0,1),
        new Vector3Int(0,0,1),
        new Vector3Int(0,1,0),
        new Vector3Int(1,1,0),
        new Vector3Int(1,1,1),
        new Vector3Int(0,1,1),
    };


    //protected List<int> NeighbourIndicesFromTo(int fromIndex, int toIndex, int triIndex, int sign, )

}
