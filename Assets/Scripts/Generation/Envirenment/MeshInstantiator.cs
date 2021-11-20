using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshGPUInstanciation
{
    public class MeshInstantiator : MonoBehaviour
    {

        public List<InstanciableData> datas = new List<InstanciableData>();

        public void AddData(InstanciableData data)
        {
            datas.Add(data);
        }

        private void Update()
        {
            int count = datas.Count;
            for (int i = 0; i < count; i++)
            {
                InstanciableData data = datas[i];
                Graphics.DrawMeshInstancedIndirect(data.instanceMesh, 0, data.material, data.bounds, data.argsBuffer);
            }
        }

    }
}