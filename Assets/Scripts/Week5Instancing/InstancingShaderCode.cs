using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using EasyButtons;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class InstancingShaderCode : MonoBehaviour
{
    [SerializeField] private Material instanceMat;
    [SerializeField] private Material simulationMat;
    [SerializeField] private Mesh mesh;
    [SerializeField] private int resolution;
    [SerializeField] private float spacing = 0.5f;
    [SerializeField] private float size = 0.5f;
    

    private ComputeBuffer _argsBuffer;
    private uint[] args = new uint[5];
    private Bounds _bounds = new Bounds(Vector3.zero, Vector3.one * 100);

    // Start is called before the first frame update
    void Start()
    {

    }
    
    private void Update()
    {
        _argsBuffer?.Release();

        _argsBuffer = new ComputeBuffer(1, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);

        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)(resolution * resolution);
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);

        _argsBuffer.SetData(args);

        var tex = simulationMat.GetTexture("_MainTex");
        
        instanceMat.SetTexture("_InTex", tex);
        instanceMat.SetInt("_Rez", resolution);
        instanceMat.SetFloat("_Spacing", spacing);
        instanceMat.SetFloat("_Size", size);
        instanceMat.SetMatrix("_TrMat", transform.localToWorldMatrix);
        instanceMat.SetVector("_Position", transform.position);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, instanceMat, _bounds, _argsBuffer);
    }
}
