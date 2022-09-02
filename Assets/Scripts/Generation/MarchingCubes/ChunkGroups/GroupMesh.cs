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

        protected abstract Key CreateRootNodeAt(Vector3Int coord);

        protected Key CreateGroupAtCoordinate(Vector3Int coord)
        {
            Key group = CreateRootNodeAt(GROUP_SIZE * coord);
            storageGroups.Add(coord, group);
            return group;
        }

        protected void StoreGroupAtCoordinate(Key group, Vector3Int coord)
        {
            storageGroups.Add(coord, group);
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

        public Key GetOrCreateGroupAtGlobalPosition(int[] pos)
        {
            return GetOrCreateGroupAtCoordinate(PositionToGroupCoord(pos));
        }

        public bool CreateNodeIfNotExisitingAt(int[] pos)
        {
            return CreateNodeIfNotExisitingAt(pos, out _);
        }

        public bool CreateNodeIfNotExisitingAt(int[] pos, out Key key)
        {
            return CreateNodeIfNotExisitingAtCoord(PositionToGroupCoord(pos), out key);
        }

        public bool CreateNodeIfNotExisitingAtCoord(Vector3Int coord, out Key k)
        {
            if (!storageGroups.TryGetValue(coord, out k))
            {
                k = CreateGroupAtCoordinate(coord);
                return true;
            }
            return false;
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

        public bool HasGroupAtPos(int[] pos)
        {
            return HasGroupAtCoord(PositionToGroupCoord(pos));
        }

        public bool HasGroupAtCoord(Vector3Int coord)
        {
            return storageGroups.TryGetValue(coord, out _);
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

        public Key GetGroupAt(int[] p)
        {
            Vector3Int coord = PositionToGroupCoord(p);
            return storageGroups[coord];
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