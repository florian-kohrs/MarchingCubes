using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{

    public class MeshInstantiator : MonoBehaviour
    {

        public List<InstanciableData> datas = new List<InstanciableData>();

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

        public void AddData(InstanciableData data)
        {
            datas.Add(data);
        }


        //TODO: Use on OnRenderObject?
        private void Update()
        {
            int count = datas.Count;
            for (int i = 0; i < count; i++)
            {
                InstanciableData data = datas[i];
                if (data.ShouldRemoveInstanceData)
                {
                    datas.RemoveAt(i);
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