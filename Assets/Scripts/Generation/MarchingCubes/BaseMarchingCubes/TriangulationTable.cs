using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MarchingCubes
{
    public class TriangulationTable : MonoBehaviour
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
                    throw new Exception("Index breakdown didnt work");
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

                for (int i = 0; i < Length; i++)
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
            for (int i = 0; i < 3; i++)
            {
                if (triangulation[triangulationIndex][triIndex * 3 + i] == edgeValue)
                    return i;

            }
            throw new Exception("Edge value "
                + edgeValue
                + " not found in triangulation index "
                + triangulationIndex
                + " at triangle number "
                + triIndex);
        }

        public static Vector2Int RotateEdgeOn(Vector2Int edge, MirrorAxis axis)
        {
            return new Vector2Int(RotateEdgeIndexOn(edge.x, axis), RotateEdgeIndexOn(edge.y, axis));
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

        public static bool GetNeighbourIndexIn(int fromIndex, int fromTriIndex, int toIndex, out int result, MirrorAxis shiftedOnAxis)
        {
            CubeRepresentation cube = new CubeRepresentation(toIndex);
            cube.MirrorRepresentation(shiftedOnAxis);
            return GetNeighbourIndexIn(fromIndex, fromTriIndex, cube.CubeIndex, out result);
        }


        public static int RotateIndex(int triangulationIndex, MirrorAxis axis)
        {
            CubeRepresentation cube = new CubeRepresentation(triangulationIndex);
            cube.MirrorRepresentation(axis);
            return cube.CubeIndex;
        }

        public static bool GetNeighbourIndexIn(int fromIndex, int fromTriIndex, int toIndex, out int result)
        {
            return NeighbourTable.TryGetValue(new NeighbourKey(fromIndex, fromTriIndex, toIndex), out result);
        }

        public static List<Tuple<int, Vector2Int>> GetInternNeighbourIndiceces(int fromIndex, int fromTriIndex)
        {
            List<Tuple<int, Vector2Int>> neighbours;
            InternNeighbours.TryGetValue(BuildLong(fromIndex, fromTriIndex), out neighbours);
            return neighbours;
        }

        protected static long BuildLong(int i1, int i2)
        {
            return ((long)i1 << 32) + i2;
        }

        public struct NeighbourKey
        {
            public NeighbourKey(int from, int tri, int to)
            {
                fromIndex = from;
                fromTriIndex = tri;
                toIndex = to;
            }

            public int fromIndex;
            public int fromTriIndex;
            public int toIndex;
        }

        private const int SAME_VERTICES_TO_BE_NEIGHBOURS = 2;

        protected static Dictionary<NeighbourKey, int> neighbourTable;
        protected static Dictionary<long, List<Tuple<int, Vector2Int>>> internNeighbours;

        public static Vector3Int GetTriangleAt(int trianuglationIndex, int triIndex)
        {
            return new Vector3Int(
                triangulation[trianuglationIndex][triIndex * 3],
                triangulation[trianuglationIndex][triIndex * 3 + 1],
                triangulation[trianuglationIndex][triIndex * 3 + 2]);
        }

        public static IEnumerable<Vector2Int> GetEdges(Vector3Int v3)
        {
            yield return new Vector2Int(v3.x, v3.y);
            yield return new Vector2Int(v3.y, v3.z);
            yield return new Vector2Int(v3.z, v3.x);
        }

        public static List<Tuple<Vector2Int, Vector3Int>> GetNeighbourOffsetForTriangle(MarchingCubeEntity e, int triIndex)
        {
            List<Tuple<Vector2Int, Vector3Int>> result = new List<Tuple<Vector2Int, Vector3Int>>();
            int index = triIndex * 3;

            for (int i = 0; i < 3; i++)
            {
                Vector3Int r = Vector3Int.zero;
                Vector2Int edgeIndex = new Vector2Int(triangulation[e.triangulationIndex][index + i], triangulation[e.triangulationIndex][index + ((i + 1) % 3)]);
                GetEdgeAxisDirection(ref r, edgeIndex.x);
                GetEdgeAxisDirection(ref r, edgeIndex.y);
                r = r.Map(f => { if (Mathf.Abs(f) == 2) { return (int)Mathf.Sign(f) * 1; } else { return 0; } });
                if (r != Vector3.zero)
                {
                    result.Add(Tuple.Create(edgeIndex, r));
                }
            }
            return result;
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

        public static int GetIndexWithEdges(int index, Vector2Int edge)
        {
            int result = -1;
            Vector3 v = new Vector3Int();
            for (int i = 0; i < TRIANGULATION_ENTRY_SIZE && result < 0; i += 3)
            {
                v.x = triangulation[index][i];
                v.y = triangulation[index][i + 1];
                v.z = triangulation[index][i + 2];
                if (v.SharesExactNValuesWith(new Vector3(edge.x, edge.y, -1), 2))
                {
                    result = i / 3;
                }
            }
            if (result == -1)
            {
                throw new Exception("no triangle found in " + index + " with the edges " + edge.x + "," + edge.y);
            }
            return result;
        }

        public static bool TryGetIndexWithEdges(int index, Vector2Int edge, out int result)
        {
            result = -1;
            Vector3 v = new Vector3Int();
            for (int i = 0; i < TRIANGULATION_ENTRY_SIZE && result < 0; i += 3)
            {
                v.x = triangulation[index][i];
                v.y = triangulation[index][i + 1];
                v.z = triangulation[index][i + 2];
                if (v.SharesExactNValuesWith(new Vector3(edge.x, edge.y, -1), 2))
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

        public static Dictionary<NeighbourKey, int> NeighbourTable
        {
            get
            {
                if (neighbourTable == null)
                {
                    BuildNeighbourTable();
                }
                return neighbourTable;
            }
        }

        public static Dictionary<long, List<Tuple<int, Vector2Int>>> InternNeighbours
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
            internNeighbours = new Dictionary<long, List<Tuple<int, Vector2Int>>>();
            for (int i = 1; i < triangulation.Count - 1; i++)
            {
                for (int triIndex1 = 0; triIndex1 < triangulation[i].Length - 1 && triangulation[i][triIndex1] >= 0; triIndex1 += 3)
                {
                    int firstIndex = triIndex1 / 3;
                    Vector3 v1 = new Vector3(
                           triangulation[i][triIndex1],
                           triangulation[i][triIndex1 + 1],
                           triangulation[i][triIndex1 + 2]);
                    for (int triIndex2 = triIndex1 + 3; triIndex2 < triangulation[i].Length - 1 && triangulation[i][triIndex2] >= 0; triIndex2 += 3)
                    {
                        int secondIndex = triIndex2 / 3;
                        Vector3 v2 = new Vector3(
                             triangulation[i][triIndex2],
                             triangulation[i][triIndex2 + 1],
                             triangulation[i][triIndex2 + 2]);

                        Vector3Int v3_1;
                        Vector3Int v3_2;

                        if (v2.CountAndMapIndiciesWithSameValues(v1, out v3_1, out v3_2) >= SAME_VERTICES_TO_BE_NEIGHBOURS)
                        {
                            long key1 = BuildLong(i, firstIndex);
                            long key2 = BuildLong(i, secondIndex);
                            AddInternNeighbour(key1, secondIndex, v3_2.ReduceToVector2(f => f > 0));
                            AddInternNeighbour(key2, firstIndex, v3_1.ReduceToVector2(f => f > 0));
                        }
                    }
                }
            }
        }


        protected static void AddInternNeighbour(long key, int value, Vector2Int edge)
        {
            List<Tuple<int, Vector2Int>> neighbours;
            if (!internNeighbours.TryGetValue(key, out neighbours))
            {
                neighbours = new List<Tuple<int, Vector2Int>>();
                internNeighbours[key] = neighbours;
            }
            neighbours.Add(Tuple.Create(value, edge));
        }

        /// <summary>
        /// unfinished and unused however could potentially speed up finding neighbours a lot
        /// </summary>
        protected static void BuildNeighbourTable()
        {
            neighbourTable = new Dictionary<NeighbourKey, int>();
            NeighbourKey key1;
            NeighbourKey key2;

            for (int x = 0; x < triangulation.Count - 1; x++)
            {
                key1.fromIndex = x;
                key2.toIndex = x;
                for (int y = x; y < triangulation.Count; y++)
                {
                    key1.toIndex = y;
                    key2.fromIndex = y;
                    for (int i1 = 0; triangulation[y][i1] != -1; i1 += 3)
                    {
                        key1.fromTriIndex = i1 / 3;

                        Vector3 v1 = new Vector3(
                           triangulation[x][i1],
                           triangulation[x][i1 + 1],
                           triangulation[x][i1 + 2]);

                        for (int i2 = 0; triangulation[y][i2] != -1; i2 += 3)
                        {
                            Vector3 v2 = new Vector3(
                                triangulation[y][i2],
                                triangulation[y][i2 + 1],
                                triangulation[y][i2 + 2]);

                            Vector3Int v3_1;
                            Vector3Int v3_2;

                            if (v2.CountAndMapIndiciesWithSameValues(v1, out v3_1, out v3_2) >= SAME_VERTICES_TO_BE_NEIGHBOURS)
                            {
                                key1.fromTriIndex = i1 / 3;
                                key2.fromTriIndex = i2 / 3;

                                Add(key2, key1.fromTriIndex);
                                Add(key1, key2.fromTriIndex);

                            }
                        }
                    }
                }
            }
        }


        protected static void Add(NeighbourKey key, int i)
        {
            neighbourTable[key] = i;
        }

        //protected List<int> NeighbourIndicesFromTo(int fromIndex, int toIndex, int triIndex, int sign, )

        // Values from http://paulbourke.net/geometry/polygonise/


        public static readonly List<int[]> triangulation = new List<int[]>(255) {
    new int[]{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 } ,
    new int[]{ 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 } ,
    new int[]{ 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]{ 1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
    new int[]    { 8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1 },
    new int[]    { 3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1 },
    new int[]    { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
    new int[]    { 4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1 },
    new int[]    { 9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1 },
    new int[]    { 10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1 },
    new int[]    { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
    new int[]    { 5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1 },
    new int[]    { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1 },
    new int[]    { 2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
    new int[]    { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1 },
    new int[]    { 11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1 },
    new int[]    { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1 },
    new int[]    { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1 },
    new int[]    { 11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1 },
    new int[]    { 2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1 },
    new int[]    { 6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
    new int[]    { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1 },
    new int[]    { 6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
    new int[]    { 6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1 },
    new int[]    { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1 },
    new int[]    { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1 },
    new int[]    { 3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
    new int[]    { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1 },
    new int[]    { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1 },
    new int[]    { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
    new int[]    { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1 },
    new int[]    { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1 },
    new int[]    { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1 },
    new int[]    { 10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
    new int[]    { 1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1 },
    new int[]    { 0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1 },
    new int[]    { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
    new int[]    { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1 },
    new int[]    { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1 },
    new int[]    { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1 },
    new int[]    { 3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1 },
    new int[]    { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1 },
    new int[]    { 10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
    new int[]    { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1 },
    new int[]    { 7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
    new int[]    { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1 },
    new int[]    { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1 },
    new int[]    { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1 },
    new int[]    { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1 },
    new int[]    { 0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1 },
    new int[]    { 7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1 },
    new int[]    { 7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1 },
    new int[]    { 10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1 },
    new int[]    { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1 },
    new int[]    { 7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1 },
    new int[]    { 6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1 },
    new int[]    { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1 },
    new int[]    { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1 },
    new int[]    { 8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1 },
    new int[]    { 1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1 },
    new int[]    { 10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1 },
    new int[]    { 10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1 },
    new int[]    { 9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1 },
    new int[]    { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1 },
    new int[]    { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1 },
    new int[]    { 7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1 },
    new int[]    { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1 },
    new int[]    { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1 },
    new int[]    { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1 },
    new int[]    { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1 },
    new int[]    { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1 },
    new int[]    { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1 },
    new int[]    { 6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1 },
    new int[]    { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1 },
    new int[]    { 6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1 },
    new int[]    { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1 },
    new int[]    { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1 },
    new int[]    { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1 },
    new int[]    { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1 },
    new int[]    { 9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1 },
    new int[]    { 1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1 },
    new int[]    { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1 },
    new int[]    { 0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1 },
    new int[]    { 11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1 },
    new int[]    { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1 },
    new int[]    { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1 },
    new int[]    { 2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1 },
    new int[]    { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1 },
    new int[]    { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1 },
    new int[]    { 1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1 },
    new int[]    { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1 },
    new int[]    { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1 },
    new int[]    { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1 },
    new int[]    { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1 },
    new int[]    { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1 },
    new int[]    { 9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1 },
    new int[]    { 5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1 },
    new int[]    { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1 },
    new int[]    { 8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1 },
    new int[]    { 9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1 },
    new int[]    { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1 },
    new int[]    { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1 },
    new int[]    { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1 },
    new int[]    { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1 },
    new int[]    { 11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1 },
    new int[]    { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1 },
    new int[]    { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1 },
    new int[]    { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1 },
    new int[]    { 1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1 },
    new int[]    { 4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1 },
    new int[]    { 0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1 },
    new int[]    { 1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    { 0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]   { 0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
    new int[]    {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }
    };


        public static readonly List<int> cornerIndexAFromEdge = new List<int>(12) {
            0,
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            0,
            1,
            2,
            3
    };

        public static readonly List<int> cornerIndexBFromEdge = new List<int>(12)
    {
        1,
        2,
        3,
        0,
        5,
        6,
        7,
        4,
        4,
        5,
        6,
        7
    };
    }

}