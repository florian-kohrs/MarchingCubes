using MeshGPUInstanciation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleCombinedPerVert : MonoBehaviour
{
    public int instanceCount = 1000;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;
    public float range;
    public Texture t;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    private struct MeshProperties
    {
        public Matrix4x4 mat;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 ;      // matrix;
        }
    }

    void Start()
    {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        Color[] c = new Color[instanceMesh.vertexCount];
        for (int i = 0; i < instanceMesh.vertexCount; i++)
        {
            c[i] = Color.Lerp(Color.red,Color.blue, Random.value);
        }
        instanceMesh.colors = c;
        UpdateBuffers();
        Bounds b = new Bounds(Vector3.zero, new Vector3(range, range, range));
        new InstanciableData(instanceMesh,instanceCount, meshPropertiesBuffer, instanceMaterial, b);
    }

    void Update()
    {
        
        // Update starting position buffer
        //if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
        //    UpdateBuffers();

        //// Pad input
        //if (Input.GetAxisRaw("Horizontal") != 0.0f)
        //    instanceCount = (int)Mathf.Clamp(instanceCount + Input.GetAxis("Horizontal") * 40000, 1.0f, 5000000.0f);
        //float range = this.range * 8;
 
        //// Render
        //Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(range, range, range)), argsBuffer);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(265, 25, 200, 30), "Instance Count: " + instanceCount.ToString());
        instanceCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)instanceCount, 1.0f, 50000.0f);
    }

    void UpdateBuffers()
    {
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

        // Positions
        if (meshPropertiesBuffer != null)
            meshPropertiesBuffer.Release();
        meshPropertiesBuffer = new ComputeBuffer(instanceCount, MeshProperties.Size());
        MeshProperties[] properties = new MeshProperties[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(20.0f, 100.0f);
            float height = Random.Range(-2.0f, 2.0f);
            float size = Random.Range(0.05f, 0.25f);
            MeshProperties props = new MeshProperties();
            Vector3 position = new Vector3(Random.Range(-range, range), Random.Range(-range, range), Random.Range(-range, range));
            Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
            Vector3 scale = Vector3.one * 5;

            props.mat = Matrix4x4.TRS(position, rotation, scale);
           // props.color = Color.Lerp(Color.red, Color.blue, Random.value);
            properties[i] = props;
        }
        meshPropertiesBuffer.SetData(properties);
        instanceMaterial.SetBuffer("_Properties", meshPropertiesBuffer);
        //instanceMaterial.SetTexture("_MainTex", t);

        // Indirect args
        if (instanceMesh != null)
        {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    void OnDisable()
    {
        if (meshPropertiesBuffer != null)
            meshPropertiesBuffer.Release();
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }
}
