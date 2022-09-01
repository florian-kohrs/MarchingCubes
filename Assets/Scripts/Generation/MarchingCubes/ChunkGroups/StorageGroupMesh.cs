using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [Serializable]
    public class StorageGroupMesh : GroupMesh<StorageTreeNode, StoredChunkEdits>
    {

        public StorageGroupMesh(int groupSize) : base(groupSize) { }

        protected override StorageTreeNode CreateKey(Vector3Int coord)
        {
            int[] nodeCoord = VectorExtension.ToArray(coord);
            return new StorageTreeNode(null, nodeCoord, nodeCoord,0, GROUP_SIZE);
        }

        //TODO: Has to mark mipmaps dirty if any child changes!
        public bool TryGetMipMapAt(Vector3Int pos, int sizePower, out float[] storedNoise, out bool isMipMapComplete)
        {
            if (TryGetGroupAt(pos, out StorageTreeNode chunkGroup))
            {
                return chunkGroup.TryGetMipMapOfChunkSizePower(new int[] { pos.x, pos.y, pos.z }, sizePower, out storedNoise, out isMipMapComplete);
            }
            isMipMapComplete = false;
            storedNoise = null;
            return false;
        }

        public bool TryLoadPoints(CompressedMarchingCubeChunk chunk, out float[] loadedPoints)
        {
            return TryGetMipMapAt(chunk.AnchorPos, chunk.ChunkSizePower, out loadedPoints, out bool complete) && complete;
        }

        public bool TryLoadCompleteNoise(Vector3Int anchor, int sizePow, out float[] noise, out bool isMipMapComplete)
        {
            return TryGetMipMapAt(anchor, sizePow, out noise, out isMipMapComplete) && isMipMapComplete;
        }

        public bool TryGetGroupItemAt(int[] startPos, CompressedMarchingCubeChunk chunk, out StoredChunkEdits result)
        {
            bool found = TryGetGroupAt(startPos, out StorageTreeNode group);
            if (found)
            {
                group.TryFollowPathToLeaf(chunk.GetStoragePath, out result);
            }
            else
            {
                result = null;
            }
            return result != null;
        }


        public void Store(Vector3Int anchorPos, ReducedMarchingCubesChunk chunk, bool overrideNoise = false)
        {
            StoredChunkEdits edits;
            if (!TryGetGroupItemAt(VectorExtension.ToArray(anchorPos), chunk, out edits) || overrideNoise)
            {
                edits = new StoredChunkEdits();
                StorageTreeNode r = GetOrCreateGroupAtCoordinate(PositionToGroupCoord(anchorPos));

                chunk.StoreChunk(edits);

                r.SetLeafAtPath(chunk.GetStoragePath, edits, true);
                //call all instantiableData from chunk that need to be stored
                //(everything not depending on triangles only, e.g trees )
            }
            //TODO: Remove later
            if (edits.noise != chunk.Points)
            {
                throw new Exception();
            }
            chunk.storageLeaf = edits.leaf;
        }


    }
}