using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{

    [Serializable]
    public class StorageGroupMesh : GroupMesh<StorageTreeRoot, StoredChunkEdits, StorageTreeLeaf, IStorageGroupOrganizer<StoredChunkEdits>>
    {

        public StorageGroupMesh(int groupSize) : base(groupSize) { }

        protected override StorageTreeRoot CreateKey(Vector3Int coord)
        {
            return new StorageTreeRoot(new int[] {coord.x, coord.y, coord.z}, GROUP_SIZE);
        }

        //TODO: Has to mark mipmaps dirty if any child changes!
        public bool TryGetMipMapAt(Vector3Int pos, int sizePower, out float[] storedNoise, out bool isMipMapComplete)
        {
            if (TryGetGroupAt(pos, out StorageTreeRoot chunkGroup))
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

        public void Store(Vector3Int anchorPos, ReducedMarchingCubesChunk chunk, bool overrideNoise = false)
        {
            StoredChunkEdits edits;
            if (!TryGetGroupItemAt(VectorExtension.ToArray(anchorPos), out edits) || overrideNoise)
            {
                edits = new StoredChunkEdits();
                StorageTreeRoot r = GetOrCreateGroupAtCoordinate(PositionToGroupCoord(anchorPos));

                chunk.StoreChunk(edits);

                r.SetLeafAtPosition(anchorPos, edits, true);
                //call all instantiableData from chunk that need to be stored
                //(everything not depending on triangles only, e.g trees )
            }
            //Remove later
            if (edits.noise != chunk.Points)
            {
                throw new Exception();
            }
            chunk.storageLeaf = edits.leaf;
        }


    }
}