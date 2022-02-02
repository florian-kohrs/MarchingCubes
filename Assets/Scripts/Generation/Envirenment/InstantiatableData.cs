using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{
    public class InstantiatableData : System.IDisposable
    {

        protected const string MATERIAL_PROPERTY_BUFFER_NAME = "_Properties";

        public bool ShouldRemoveInstanceData => !bounds.HasValue;

        public Mesh instanceMesh;

        protected uint[] args;

        public ComputeBuffer instanceTransformations;
        public ComputeBuffer argsBuffer;

        public Material material;
        public Maybe<Bounds> bounds;
        public bool changed;


        ~InstantiatableData()
        {
            Dispose();
        }

        public InstantiatableData(Mesh instanceMesh, ComputeBuffer instanceTransformations, Material material, Maybe<Bounds> bounds, int count = 0)
        {
            this.material = material;
            this.instanceTransformations = instanceTransformations;
            this.bounds = bounds;
            this.instanceMesh = instanceMesh;
            args = new uint[]
            {
                instanceMesh.GetIndexCount(0),
                0,
                instanceMesh.GetIndexStart(0),
                instanceMesh.GetBaseVertex(0),
                (uint)count
            };


            //Color[] colors = new Color[instanceMesh.vertexCount];
            //for (int i = 0; i < colors.Length; i++)
            //{
            //    colors[i] = Color.red;
            //}
            //instanceMesh.colors = colors;
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);


            //MeshInstancedProperties props = new MeshInstancedProperties();
            //Vector3 position = new Vector3(1, 2, 3);
            //Quaternion rotation = Quaternion.identity;
            //Vector3 scale = Vector3.one * 3;
            //props.mat = Matrix4x4.TRS(position, rotation, scale);

            //float[] vals = new float[16];
            //for (int column = 0; column < 4; column++)
            //{
            //    for (int row = 0; row < 4; row++)
            //    {
            //        vals[column * 4 + row] = props.mat[1];
            //    }
            //}


            //MeshInstancedProperties[] mesh = new MeshInstancedProperties[count];
            //ExtensionArray.Fill(mesh, props);
            //instanceTransformations.SetData(mesh);

            material.SetBuffer(MATERIAL_PROPERTY_BUFFER_NAME, instanceTransformations);
        }

        public InstantiatableData(Mesh instanceMesh, ComputeBuffer instanceTransformations, Material material, Maybe<Bounds> bounds) : this(instanceMesh, instanceTransformations, material, bounds, 0)
        {
            ComputeBuffer.CopyCount(instanceTransformations, argsBuffer, 4);

            //argsBuffer.GetData(args);
            //MeshInstancedProperties[] propss = new MeshInstancedProperties[args[1]];
            //instanceTransformations.GetData(propss);

            MeshInstantiator.meshInstantiator.AddData(this);
        }



        public void UpdateTransformationBufferLength()
        {
            ComputeBuffer.CopyCount(instanceTransformations, argsBuffer, 4);
        }

        public void Dispose()
        {
            if (argsBuffer != null && argsBuffer.IsValid())
            {
                argsBuffer.Dispose();
                argsBuffer = null;
            }
            if (instanceTransformations != null && instanceTransformations.IsValid())
            {
                instanceTransformations.Dispose();
                instanceTransformations = null;
            }
        }

        
    }
}