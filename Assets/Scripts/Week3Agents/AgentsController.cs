using EasyButtons;
using UnityEngine;

namespace Week3Agents
{
    public class AgentsController : MonoBehaviour
    {
        [SerializeField] private ComputeShader cs;
        [SerializeField] private Material renderMat;
        [SerializeField] private int resolution;
        [SerializeField] private int stepsPerFrame;
        [SerializeField] private int stepInterval;
        [SerializeField] private int agentCount;
        [SerializeField] private int range;
        [SerializeField] private int diffuseRange = 1;
        [SerializeField, Range(-1, 1)] private float dirThreshold = 0.1f;
        [SerializeField, Range(0, 1)] private float trailDeposit = 0.1f;
        [SerializeField, Range(0, 1)] private float trailDecayFactor = 0.1f;
        [SerializeField] private float brushSize = 0.1f;
        [SerializeField] private float brushStrength = 0.1f;
        [SerializeField] private bool isSimulating;

        private int _renderTrailKernelId;
        private int _renderAgentsKernelId;
        private int _simulateKernelId;
        private int _resetTextureKernelId;
        private int _resetAgentsKernelId;
        private int _trailKernelId;
        private int _diffuseKernelId;
        private int _resDiv;
        private int _agentsDiv;

        private RenderTexture _readTex;
        private RenderTexture _vizTex;
        private RenderTexture _stigmergyTex;

        private ComputeBuffer _agentsBuffer;

        private Camera _mainCam;

        private float TimeSeed => Time.realtimeSinceStartup;

        private void Start()
        {
            _mainCam = Camera.main;
            SetKernelIds();
            ResetSim();
        }
        
        private void SetKernelIds()
        {
            _renderTrailKernelId = cs.FindKernel("RenderTrail");
            _renderAgentsKernelId = cs.FindKernel("RenderAgents");
            _resetTextureKernelId = cs.FindKernel("ResetTexture");
            _resetAgentsKernelId = cs.FindKernel("ResetAgents");
            _simulateKernelId = cs.FindKernel("Simulate");
            _trailKernelId = cs.FindKernel("MakeTrail");
            _diffuseKernelId = cs.FindKernel("DiffuseTexture");
        }
        
        [Button]
        private void ResetSim()
        {
            CreateTextures();
            _resDiv = resolution / 32;
            _agentsDiv = agentCount / 64;
            
            // _resDiv = resolution;
            // _agentsDiv = agentCount;

            Utils.SetVizTexture(_vizTex, renderMat);

            _agentsBuffer?.Release();
            _agentsBuffer = new ComputeBuffer(agentCount, sizeof(float) * 4);
        
            cs.SetFloat("_Time", TimeSeed);
            cs.SetInt("_AgentCount", agentCount);
            cs.SetInt("_Rez", resolution);
     
            ApplyParams();

            cs.SetTexture(_resetTextureKernelId,"_WriteTex", _stigmergyTex);
            cs.Dispatch(_resetTextureKernelId, _resDiv, _resDiv, 1);
            
            cs.SetTexture(_resetTextureKernelId,"_WriteTex", _readTex);
            cs.Dispatch(_resetTextureKernelId, _resDiv, _resDiv, 1);

            cs.SetBuffer(_resetAgentsKernelId, "_Agents", _agentsBuffer);
            cs.Dispatch(_resetAgentsKernelId, _agentsDiv, 1, 1);

            cs.SetBuffer(_simulateKernelId, "_Agents", _agentsBuffer);
            cs.SetTexture(_simulateKernelId, "_ReadTex", _readTex);
            
            cs.SetTexture(_renderTrailKernelId, "_VizTex", _vizTex);
            
            cs.SetTexture(_renderAgentsKernelId, "_VizTex", _vizTex);
            cs.SetBuffer(_renderAgentsKernelId, "_Agents", _agentsBuffer);

            cs.SetBuffer(_trailKernelId, "_Agents", _agentsBuffer);
            cs.SetTexture(_trailKernelId, "_StigmergyTex", _stigmergyTex);
        }

        [Button]
        private void ApplyParams()
        {
            cs.SetFloat("_DirThreshold", dirThreshold);
            cs.SetFloat("_TrailDeposit", trailDeposit);
            cs.SetFloat("_TrailDecay", trailDecayFactor);
            cs.SetFloat("_BrushStrength", brushStrength);
            cs.SetFloat("_BrushSize", brushSize);
            cs.SetInt("_Range", range);
            cs.SetInt("_DiffuseRange", diffuseRange);
        }
        
        private void CreateTextures()
        {
            _readTex = Utils.CreateTexture(_readTex, RenderTextureFormat.RFloat, resolution);
            _stigmergyTex = Utils.CreateTexture(_stigmergyTex, RenderTextureFormat.RFloat, resolution);
            _vizTex = Utils.CreateTexture(_vizTex, RenderTextureFormat.ARGB32, resolution);
        }

        private void Update()
        {
            if (Time.frameCount % (stepInterval + 1) != 0 || !isSimulating)
                return;

            for (int i = 0; i <= stepsPerFrame; i++)
                Step();
        }

        private void SetBrushPosition()
        {
            var pos = Vector2.zero;
            if (Input.GetMouseButton(0))
            {
                var mouseRay = _mainCam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(mouseRay, out var hitInfo))
                    pos = hitInfo.textureCoord * resolution;
            }
            
            cs.SetVector("_BrushPos", pos);
        }

        [Button]
        private void Step()
        {
            SetBrushPosition();
            
            cs.SetFloat("_Time", TimeSeed);

            cs.SetTexture(_simulateKernelId, "_ReadTex", _readTex);
            cs.Dispatch(_simulateKernelId, _agentsDiv, 1, 1);

            cs.SetTexture(_diffuseKernelId, "_ReadTex", _readTex);
            cs.SetTexture(_diffuseKernelId, "_StigmergyTex", _stigmergyTex);
            cs.Dispatch(_diffuseKernelId, _resDiv, _resDiv, 1);
            
            cs.SetTexture(_trailKernelId, "_StigmergyTex", _stigmergyTex);
            cs.Dispatch(_trailKernelId, _agentsDiv, 1, 1);
            Render();
            
            Utils.SwapTextures(ref _readTex, ref _stigmergyTex);
        }

        private void Render()
        {
            cs.SetTexture(_renderTrailKernelId, "_StigmergyTex", _stigmergyTex);
            cs.Dispatch(_renderTrailKernelId, _resDiv, _resDiv, 1);
            // cs.Dispatch(_renderAgentsKernelId, _agentsDiv, 1, 1);
        }

        private void OnDisable()
        {
            _agentsBuffer?.Release();
        }

        private void OnDestroy()
        {
            _agentsBuffer?.Release();
        }
    }
}
