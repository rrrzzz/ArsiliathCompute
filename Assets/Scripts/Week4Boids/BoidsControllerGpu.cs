using System;
using EasyButtons;
using UnityEngine;

namespace Week4Boids
{
    public class BoidsControllerGpu : MonoBehaviour
    {
        [SerializeField] private ComputeShader cs;
        [SerializeField] private Material renderMat;
        [SerializeField] private Material debugMat;
        [SerializeField] private int boidCount;
        [SerializeField] private int resolution = 16;
        [SerializeField] private bool cohesionOn;
        [SerializeField, Range(0,1)] private float cohesionStrength;
        [SerializeField] private bool avoidanceOn;
        [SerializeField, Range(0,1)] private float avoidanceStrength;
        [SerializeField] private bool steeringOn;
        [SerializeField, Range(0,1)] private float steeringStrength;
        [SerializeField] private bool wanderingOn;
        [SerializeField] private float speed = 1;
        [SerializeField] private int frameInterval = 1;
        [SerializeField] private float visionAngle = .1f;
        [SerializeField] private float wanderingAngle = 20f;
        [SerializeField] private int visionRange;

        private int _resetKernel;
        private int _resetTextureKernel;
        private int _simulateKernel;
        private int _renderKernel;
        private int _boidCountDiv;
        private int _resDiv;

        private ComputeBuffer _boidReadBuffer;
        private ComputeBuffer _boidWriteBuffer;
        private ComputeBuffer _debugBuffer;
        private RenderTexture _renderTex;
        private RenderTexture _readTex;
        private RenderTexture _writeTex;
        private RenderTexture _debugTex;

        private void Start()
        { 
            SetKernels();
            Reset();
        }

        private void SetKernels()
        {
            _resetKernel = cs.FindKernel("ResetKernel");
            _resetTextureKernel = cs.FindKernel("ResetTextureKernel");
            _simulateKernel = cs.FindKernel("SimulateKernel");
            _renderKernel = cs.FindKernel("RenderKernel");
        }

        [Button]
        private void Reset()
        {
            _boidCountDiv = boidCount;
            _boidCountDiv = boidCount / 64;

            _resDiv = resolution;
            _resDiv = resolution / 32;
            
            _renderTex = Utils.CreateTexture(_renderTex, RenderTextureFormat.ARGB32, resolution);
            _debugTex = Utils.CreateTexture(_debugTex, RenderTextureFormat.ARGB32, resolution);
            _readTex = Utils.CreateTexture(_debugTex, RenderTextureFormat.ARGB32, resolution);
            _writeTex = Utils.CreateTexture(_debugTex, RenderTextureFormat.ARGB32, resolution);
        
            Utils.SetVizTexture(_renderTex, renderMat);
            Utils.SetVizTexture(_debugTex, debugMat);
        
            _boidReadBuffer?.Release();
            _boidWriteBuffer?.Release();

            _boidReadBuffer = new ComputeBuffer(boidCount, sizeof(float) * 4);
            _boidWriteBuffer = new ComputeBuffer(boidCount, sizeof(float) * 4);
        
            cs.SetTexture(_renderKernel, "_VizTex", _renderTex);

            cs.SetTexture(_resetTextureKernel, "_VizTex", _readTex);
            cs.Dispatch(_resetTextureKernel, _resDiv, _resDiv, 1);
        
            cs.SetBuffer(_resetKernel, "_BoidWriteBuffer", _boidReadBuffer);
            cs.SetTexture(_resetKernel, "_WriteTex", _readTex);
            cs.SetTexture(_resetKernel, "_DebugTex", _debugTex);
            cs.Dispatch(_resetKernel, _boidCountDiv, 1, 1);

            cs.SetTexture(_simulateKernel, "_DebugTex", _debugTex);

            ApplyParams();
        }

        [Button]
        private void ApplyParams()
        {
            cs.SetInt("_BoidCount", boidCount);
            cs.SetInt("_Rez", resolution);
        
            cs.SetBool("_CohesionOn", cohesionOn);
            cs.SetFloat("_CohesionStrength", cohesionStrength);
            
            cs.SetBool("_AvoidanceOn", avoidanceOn);
            cs.SetFloat("_AvoidanceStrength", avoidanceStrength);
        
            cs.SetBool("_WanderingOn", wanderingOn);
            cs.SetFloat("_WanderingAngle", wanderingAngle * Mathf.Deg2Rad);
        
            cs.SetBool("_SteeringOn", steeringOn);
            cs.SetFloat("_SteeringStrength", steeringStrength);
        
            cs.SetFloat("_VisionAngle", visionAngle);
            cs.SetInt("_VisionRange", visionRange);
            cs.SetFloat("_Speed", speed);
        }

        private void SwapBuffers()
        {
            var tmp = _boidReadBuffer;
            _boidReadBuffer = _boidWriteBuffer;
            _boidWriteBuffer = tmp;
        }
    
        private void Update()
        {
            if (Time.frameCount % frameInterval != 0)
                return;
            

            // if (!Input.GetKeyDown(KeyCode.Space)) return;
            
            cs.SetFloat("_Seed", Time.frameCount);
            cs.SetTexture(_resetTextureKernel, "_VizTex", _renderTex);
            cs.Dispatch(_resetTextureKernel, _resDiv, _resDiv, 1);

            cs.SetBuffer(_simulateKernel, "_BoidReadBuffer", _boidReadBuffer);
            cs.SetBuffer(_simulateKernel, "_BoidWriteBuffer", _boidWriteBuffer);
            cs.SetTexture(_simulateKernel, "_WriteTex", _writeTex);
            cs.SetTexture(_simulateKernel, "_ReadTex", _readTex);

            cs.Dispatch(_simulateKernel, _boidCountDiv, 1, 1);

            // var output = new Vector4[boidCount];
            //
            // _debugBuffer.GetData(output);
            //
            // for (int i = 0; i < boidCount; i++)
            // {
            //     var b = output[i];
            //     Debug.Log($"Boid {i} pos: ({b.x}, {b.y}). Dir: ({b.z}, {b.w})");
            // }
            
            cs.SetBuffer(_renderKernel, "_BoidReadBuffer", _boidWriteBuffer);
            cs.Dispatch(_renderKernel, _boidCountDiv, 1, 1);
            cs.SetTexture(_resetTextureKernel, "_VizTex", _readTex);
            cs.Dispatch(_resetTextureKernel, _resDiv, _resDiv, 1);
            SwapBuffers();
            Utils.SwapTextures(ref _writeTex, ref _readTex);
        }
    }
}
