using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{

    public class MeshInstantiator : MonoBehaviour
    {

        public List<InstantiatableData> datas = new List<InstantiatableData>();

        public static MeshInstantiator meshInstantiator;

        private void Awake()
        {
            meshInstantiator = this;
        }

        private void OnDestroy()
        {
            meshInstantiator = null;
            datas.ForEach(d => d.Dispose());
            datas = null;
        }

        public void AddData(InstantiatableData data)
        {
            datas.Add(data);
        }


        //TODO: Use on OnRenderObject?
        private void Update()
        {
            int count = datas.Count;
            for (int i = 0; i < count; i++)
            {
                InstantiatableData data = datas[i];
                if (data.ShouldRemoveInstanceData)
                {
                    datas.RemoveAt(i);
                    count--;
                    i--;
                }
                else
                {
                    Graphics.DrawMeshInstancedIndirect(data.instanceMesh, 0, data.material, data.bounds.Value, data.argsBuffer);
                }
            }
        }

    }
}