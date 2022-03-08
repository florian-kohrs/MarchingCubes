using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes
{
    public class ChunkNeighbourTask
    {

        public ChunkNeighbourTask(CompressedMarchingCubeChunk chunk, MeshData meshData)
        {
            this.chunk = chunk;
            this.meshData = meshData;
            maxEntityIndexPerAxis = chunk.MaxEntitiesIndexPerAxis;
        }

        public static ChunkGroupMesh chunkGroup;

        public CompressedMarchingCubeChunk chunk;

        public MeshData meshData;

        public bool[] HasNeighbourInDirection { get; private set; } = new bool[6];

        protected int maxEntityIndexPerAxis;

        public void FindNeighbours()
        {
            NeighbourSearch();
            CheckIfNeighboursExistAlready();
        }

        protected void NeighbourSearch()
        {
            int length = meshData.vertices.Length;
            Vector3 coord;
            Vector3 offset = chunk.AnchorPos;
            int size = chunk.LOD;

            for (int i = 0; i < length; i+=3)
            {
                coord = (meshData.vertices[i] - offset) / size;
                SetNeighbourAt(coord.x, coord.y, coord.z);
            }
        }

        protected void SetNeighbourAt(float x, float y, float z)
        {
            if (x == 0)
            {
                HasNeighbourInDirection[1] = true;

            }
            else if (x == maxEntityIndexPerAxis)
            {
                HasNeighbourInDirection[0] = true;
            }

            if (y == 0)
            {
                HasNeighbourInDirection[3] = true;
            }
            else if (y == maxEntityIndexPerAxis)
            {
                HasNeighbourInDirection[2] = true;
            }

            if (z == 0)
            {
                HasNeighbourInDirection[5] = true;
            }
            else if (z == maxEntityIndexPerAxis)
            {
                HasNeighbourInDirection[4] = true;
            }
        }

        protected void CheckIfNeighboursExistAlready()
        {
            Vector3Int v3;
            for (int i = 0; i < 6; i++)
            {
                if(HasNeighbourInDirection[i])
                {
                    v3 = VectorExtension.GetDirectionFromIndex(i) * (chunk.ChunkSize + 1) + chunk.CenterPos;
                    HasNeighbourInDirection[i] = !chunkGroup.HasChunkStartedAt(VectorExtension.ToArray(v3));
                }
            }
        }

    }
}