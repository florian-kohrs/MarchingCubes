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

        protected bool[] hasNeighbourInDirection { get; private set; } = new bool[6];

        public bool[] HasNeighbourInDirection => hasNeighbourInDirection;

        protected int maxEntityIndexPerAxis;

        public void FindNeighbours()
        {
            NeighbourSearch();
            CheckIfNeighboursExistAlready();
        }

        public void NeighbourSearch()
        {
            int length = meshData.vertices.Length;
            float xOffset = chunk.AnchorPos.x;
            float yOffset = chunk.AnchorPos.y;
            float zOffset = chunk.AnchorPos.z;
            int size = chunk.LOD;

            for (int i = 0; i < length; i+=1)
            {
                SetNeighbourAt(
                    (meshData.vertices[i].x - xOffset) / size, 
                    (meshData.vertices[i].y - yOffset) / size, 
                    (meshData.vertices[i].z - zOffset) / size);
            }
        }

        
        protected void SetNeighbourAt(float x, float y, float z)
        {
            if (x == 0)
            {
                hasNeighbourInDirection[1] = true;

            }
            else if (x == maxEntityIndexPerAxis)
            {
                hasNeighbourInDirection[0] = true;
            }

            if (y == 0)
            {
                hasNeighbourInDirection[3] = true;
            }
            else if (y == maxEntityIndexPerAxis)
            {
                hasNeighbourInDirection[2] = true;
            }

            if (z == 0)
            {
                hasNeighbourInDirection[5] = true;
            }
            else if (z == maxEntityIndexPerAxis)
            {
                hasNeighbourInDirection[4] = true;
            }
        }

        protected void CheckIfNeighboursExistAlready()
        {
            Vector3Int v3;
            for (int i = 0; i < 6; i++)
            {
                if(hasNeighbourInDirection[i])
                {
                    v3 = VectorExtension.GetDirectionFromIndex(i) * (chunk.ChunkSize + 1) + chunk.CenterPos;
                    hasNeighbourInDirection[i] = !chunkGroup.HasChunkStartedAt(VectorExtension.ToArray(v3));
                }
            }
        }

    }
}