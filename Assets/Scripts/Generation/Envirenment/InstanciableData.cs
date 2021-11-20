using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{
    public class InstanciableData : IDisposable
    {

        private const string MATERIAL_PROPERTY_BUFFER_NAME = "_Properties";

        ~InstanciableData()
        {
            Dispose();
        }

        public InstanciableData(Mesh instanceMesh, ComputeBuffer instanceTransformations, Material material, Bounds bounds)
        {
            this.material = material;
            this.bounds = bounds;
            this.instanceMesh = instanceMesh;
            args = new uint[]
            {
                instanceMesh.GetIndexCount(0),
                0,
                instanceMesh.GetIndexStart(0),
                instanceMesh.GetBaseVertex(0),
                0
            };
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            this.material.SetBuffer(MATERIAL_PROPERTY_BUFFER_NAME, instanceTransformations);

            ComputeBuffer.CopyCount(instanceTransformations, argsBuffer, 4);

            meshPropertiesBuffer = argsBuffer;
        }

        //TODO: may add material property block

        public Mesh instanceMesh;
        public int instanceCount;

        private uint[] args;

        public ComputeBuffer meshPropertiesBuffer;
        public ComputeBuffer argsBuffer;

        public Material material;
        public Bounds bounds;
        public bool changed;

        public void Dispose()
        {
            if (meshPropertiesBuffer != null)
            {
                meshPropertiesBuffer.Dispose();
                meshPropertiesBuffer = null;
            }
            if (argsBuffer != null)
            {
                argsBuffer.Dispose();
                argsBuffer = null;
            }
        }

    }
}