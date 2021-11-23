using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{
    public class InstanciableData : System.IDisposable
    {

        private const string MATERIAL_PROPERTY_BUFFER_NAME = "_Properties";

        ~InstanciableData()
        {
            Dispose();
        }

        public InstanciableData(Mesh instanceMesh, ComputeBuffer instanceTransformations, Material material, Bounds bounds)
        {
            this.material = material;
            this.instanceTransformations = instanceTransformations;
            this.bounds = bounds;
            this.instanceMesh = instanceMesh;
            args = new uint[]
            {
                instanceMesh.GetIndexCount(0),
                1,
                instanceMesh.GetIndexStart(0),
                instanceMesh.GetBaseVertex(0),
                0
            };
            Color[] colors = new Color[instanceMesh.vertexCount];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.green;
            }
            instanceMesh.colors = colors;
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);
            float range = 10;
            MeshInstancedProperties props = new MeshInstancedProperties();
            Vector3 position = new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));
            Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
            Vector3 scale = Vector3.one * 5;
            props.mat = Matrix4x4.TRS(position, rotation, scale);
            instanceTransformations.SetData(new MeshInstancedProperties[] { props });
            //this.material.SetBuffer(MATERIAL_PROPERTY_BUFFER_NAME, instanceTransformations);

            //ComputeBuffer.CopyCount(instanceTransformations, argsBuffer, 4);

            meshPropertiesBuffer = argsBuffer;
            MeshInstantiator.meshInstantiator.AddData(this);
        }

        public void UpdateTransformationBufferLength()
        {
            ComputeBuffer.CopyCount(instanceTransformations, argsBuffer, 4);
        }

        //TODO: may add material property block

        public Mesh instanceMesh;

        private uint[] args;

        public ComputeBuffer meshPropertiesBuffer;
        public ComputeBuffer instanceTransformations;
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
            if (instanceTransformations != null)
            {
                instanceTransformations.Dispose();
                instanceTransformations = null;
            }
        }

    }
}