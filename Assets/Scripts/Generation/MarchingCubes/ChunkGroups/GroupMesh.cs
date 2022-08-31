using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MarchingCubes
{

    [System.Serializable]
    public abstract class GroupMesh<Key, T> 
        where Key : IChunkGroupOrganizer<T>
    {

        public GroupMesh(int groupSizePower)
        {
            GROUP_SIZE_POWER = groupSizePower;
            GROUP_SIZE = (int)Mathf.Pow(2, groupSizePower);
        }

        public readonly int GROUP_SIZE_POWER;
        public readonly int GROUP_SIZE;

        public Dictionary<Serializable3DIntVector, Key> storageGroups = new Dictionary<Serializable3DIntVector, Key>();

        protected abstract Key CreateKey(Vector3Int coord);

        public bool TryGetGroupItemAt
            (int[] pos, out T result)
        {
            Vector3Int coord = PositionToGroupCoord(pos);
            if (storageGroups.TryGetValue(coord, out Key group))
            {
                if (group.TryGetLeafAtLocalPosition(pos, out result))
                {
                    return true;
                }
            }
            result = default;
            return false;
        }

        protected Key CreateGroupAtCoordinate(Vector3Int coord)
        {
            Key group = CreateKey(GROUP_SIZE * coord);
            storageGroups.Add(coord, group);
            return group;
        }

        public void SetValueAtGlobalPosition(Vector3Int position, T value, bool allowOverride)
        {
            int[] input = VectorExtension.ToArray(position);
            SetValueAtGlobalPosition(input, value, allowOverride);
        }

        public void SetValueAtGlobalPosition(int[] position, T value, bool allowOverride)
        {
            GetOrCreateGroupAtGlobalPosition(position).SetLeafAtLocalPosition(position, value, allowOverride);
        }

        public bool HasGroupItemAt(int[] p)
        {
            return TryGetGroupItemAt(p, out _);
        }

        public Key GetOrCreateGroupAtGlobalPosition(int[] pos)
        {
            return GetOrCreateGroupAtCoordinate(PositionToGroupCoord(pos));
        }

        public Key GetOrCreateGroupAtCoordinate
            (Vector3Int coord)
        {
            Key group;
            if (!storageGroups.TryGetValue(coord, out group))
            {
                group = CreateGroupAtCoordinate(coord);
            }
            return group;
        }

        public bool TryGetGroupAt(Vector3Int p, out Key group)
        {
            Vector3Int coord = PositionToGroupCoord(p);
            return storageGroups.TryGetValue(coord, out group);
        }
        public bool TryGetGroupAt(int[] p, out Key group)
        {
            Vector3Int coord = PositionToGroupCoord(p);
            return storageGroups.TryGetValue(coord, out group);
        }

        protected Vector3Int CoordToPosition(Vector3Int coord)
        {
            return coord * GROUP_SIZE;
        }

        protected Vector3Int PositionToGroupCoord(int[] pos)
        {
            return PositionToGroupCoord(pos[0], pos[1], pos[2]);
        }

        protected Vector3Int PositionToGroupCoord(Vector3Int pos)
        {
            return PositionToGroupCoord(pos.x, pos.y, pos.z);
        }

        protected Vector3Int PositionToGroupCoord(float x, float y, float z)
        {
            return new Vector3Int(
                Mathf.FloorToInt(x / GROUP_SIZE),
                Mathf.FloorToInt(y / GROUP_SIZE),
                Mathf.FloorToInt(z / GROUP_SIZE));
        }

    }
}