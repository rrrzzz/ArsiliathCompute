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
        [SerializeField] private bool isSimulating;

        private int _renderKernelId;
        private int _simulateKernelId;
        private int _resetTextureKernelId;
        private int _resetAgentsKernelId;
        private int _resDiv;
        private int _agentsDiv;
    
        private RenderTexture _readTex;
        private RenderTexture _vizTex;
        private RenderTexture _writeTex;

        private ComputeBuffer _agentsBuffer;

        private float TimeSeed => Time.realtimeSinceStartup;

        private void Start()
        {
            SetKernelIds();
            ResetSim();
        }
        
        private void SetKernelIds()
        {
            _renderKernelId = cs.FindKernel("Render");
            _resetTextureKernelId = cs.FindKernel("ResetTexture");
            _resetAgentsKernelId = cs.FindKernel("ResetAgents");
            _simulateKernelId = cs.FindKernel("Simulate");
        }
        
        [Button]
        private void ResetSim()
        {
            CreateTextures();
            
            _resDiv = resolution / 16;
            _agentsDiv = agentCount / 16;
            
            _resDiv = resolution;
            _agentsDiv = agentCount;

            Utils.SetVizTexture(_vizTex, renderMat);

            _agentsBuffer?.Release();
            _agentsBuffer = new ComputeBuffer(agentCount, sizeof(float) * 4);
        
            cs.SetFloat("_Time", TimeSeed);
            cs.SetInt("_AgentCount", agentCount);
            cs.SetInt("_Rez", resolution);

            cs.SetBuffer(_resetAgentsKernelId, "_Agents", _agentsBuffer);
            cs.Dispatch(_resetAgentsKernelId, _agentsDiv, 1, 1);
            
            cs.SetBuffer(_simulateKernelId, "_Agents", _agentsBuffer);
            
            cs.SetBuffer(_renderKernelId, "_Agents", _agentsBuffer);
            cs.SetTexture(_renderKernelId, "_VizTex", _vizTex);
        }
        
        private void CreateTextures()
        {
            _readTex = Utils.CreateTexture(_readTex, RenderTextureFormat.RFloat, resolution);
            _writeTex = Utils.CreateTexture(_writeTex, RenderTextureFormat.RFloat, resolution);
            _vizTex = Utils.CreateTexture(_vizTex, RenderTextureFormat.ARGB32, resolution);
        }

        private void Update()
        {
            if (Time.frameCount % (stepInterval + 1) != 0 || !isSimulating)
                return;

            for (int i = 0; i <= stepsPerFrame; i++)
            {
                Step();
            }
        }
    
        [Button]
        private void Step()
        {
            cs.SetFloat("_Time", TimeSeed);
            cs.Dispatch(_simulateKernelId, _agentsDiv, 1, 1);

            Render();
        }

        private void Render()
        {
            cs.Dispatch(_renderKernelId, _agentsDiv, 1, 1);
        }
    }
}
