using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using EasyButtons;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class InstancingTest : MonoBehaviour
{
    [SerializeField] private Material instanceMat;
    [SerializeField] private Mesh mesh;

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

        // Debug.Log(mesh.GetIndexCount(0));
        // Debug.Log(args[2] = mesh.GetIndexStart(0));
        // Debug.Log(mesh.GetBaseVertex(0));

        args[0] = mesh.GetIndexCount(0);
        args[1] = 10;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);

        _argsBuffer.SetData(args);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, instanceMat, _bounds, _argsBuffer);
    }
}
