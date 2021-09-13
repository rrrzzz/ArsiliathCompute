using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Week5Instancing
{
    public class PositionsBufferRenderer : PositionsBufferGenerator
    {
        [SerializeField] private Material renMat;
        [SerializeField] private Mesh instancedMesh;
        
        private ComputeBuffer _argsBuffer;
        private uint[] _args = new uint[5];
        private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10);

        private protected override void Start()
        {
            base.Start();
            _argsBuffer = new ComputeBuffer(5, sizeof(int), ComputeBufferType.IndirectArguments);
        }
        
        private protected override void Update()
        {
            base.Update();
            
            _args[0] = instancedMesh.GetIndexCount(0);
            _args[1] = (uint)pointCount;
            _args[2] = instancedMesh.GetIndexStart(0);
            _args[3] = instancedMesh.GetBaseVertex(0);
            _args[4] = 0;
            
            _argsBuffer.SetData(_args);
            renMat.SetBuffer("_PosBuffer", PosBuffer);
            renMat.SetBuffer("_ColorBuffer", ColorBuffer);
            
            Graphics.DrawMeshInstancedIndirect(instancedMesh, 0, renMat, bounds, _argsBuffer);
        }
    }
}