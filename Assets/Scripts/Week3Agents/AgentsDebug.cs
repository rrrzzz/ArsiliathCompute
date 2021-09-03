using EasyButtons;
using UnityEngine;

namespace Week3Agents
{
    public class AgentsDebug : MonoBehaviour
    {
        [SerializeField] private ComputeShader cs;
        [SerializeField] private Material renderMat;
        [SerializeField] private int resolution;
        [SerializeField] private int stepsPerFrame;
        [SerializeField] private int stepInterval;
        [SerializeField] private int agentCount;
        [SerializeField, Range(0, 1)] private float trailDecayFactor = 0.9f;
        [SerializeField] private float brushSize = 0.1f;


        private int _renderKernelId;
        private int _simulateKernelId;
        private int _resetTextureKernelId;
        private int _resetAgentsKernelId;
        private int _trailKernelId;
        private int _diffuseKernelId;
        private int _agentsDiv;
        private int _stepN;

        private RenderTexture _readTex;
        private RenderTexture _vizTex;
        private RenderTexture _stigmergyTex;

        private ComputeBuffer _agentsBuffer;

        private Camera _mainCam;

        private float TimeSeed => Time.frameCount;

        private void Start()
        {
            _mainCam = Camera.main;
            SetKernelIds();
            ResetSim();
        }
        
        private void SetKernelIds()
        {
            _renderKernelId = cs.FindKernel("RenderKernel");
            _resetTextureKernelId = cs.FindKernel("ResetTextureKernel");
            _resetAgentsKernelId = cs.FindKernel("ResetAgentsKernel");
            _simulateKernelId = cs.FindKernel("MoveAgentsKernel");
            _trailKernelId = cs.FindKernel("WriteTrailsKernel");
            _diffuseKernelId = cs.FindKernel("DiffuseTextureKernel");
        }
        
        [Button]
        private void ResetSim()
        {
            CreateTextures();
 
            _agentsDiv = agentCount / 64;
            Utils.SetVizTexture(_vizTex, renderMat);

            _agentsBuffer?.Release();
            _agentsBuffer = new ComputeBuffer(agentCount, sizeof(float) * 4);
        
            cs.SetFloat("time", TimeSeed);
            cs.SetInt("rez", resolution);
     
            ApplyParams();

            cs.SetTexture(_resetTextureKernelId,"writeTex", _stigmergyTex);
            cs.Dispatch(_resetTextureKernelId, resolution, resolution, 1);
            
            cs.SetTexture(_resetTextureKernelId,"writeTex", _readTex);
            cs.Dispatch(_resetTextureKernelId, resolution, resolution, 1);

            cs.SetBuffer(_resetAgentsKernelId, "agentsBuffer", _agentsBuffer);
            cs.Dispatch(_resetAgentsKernelId, _agentsDiv, 1, 1);

            cs.SetBuffer(_simulateKernelId, "agentsBuffer", _agentsBuffer);
            cs.SetTexture(_simulateKernelId, "readTex", _readTex);
            
            cs.SetTexture(_renderKernelId, "outTex", _vizTex);
            
            // cs.SetTexture(_renderAgentsKernelId, "_VizTex", _vizTex);
            // cs.SetBuffer(_renderAgentsKernelId, "_Agents", _agentsBuffer);

            cs.SetBuffer(_trailKernelId, "agentsBuffer", _agentsBuffer);
            cs.SetTexture(_trailKernelId, "writeTex", _stigmergyTex);
        }

        [Button]
        private void ApplyParams()
        {
            // cs.SetFloat("_DirThreshold", dirThreshold);
            // cs.SetFloat("_TrailDeposit", trailDeposit);
            cs.SetFloat("trailDecayFactor", trailDecayFactor);
            // cs.SetFloat("_BrushStrength", brushStrength);
            cs.SetFloat("brushSize", brushSize);
            // cs.SetInt("_Range", range);
            // cs.SetInt("_DiffuseRange", diffuseRange);
        }
        
        private void CreateTextures()
        {
            _readTex = Utils.CreateTexture(_readTex, RenderTextureFormat.ARGBFloat, resolution);
            _stigmergyTex = Utils.CreateTexture(_stigmergyTex, RenderTextureFormat.ARGBFloat, resolution);
            _vizTex = Utils.CreateTexture(_vizTex, RenderTextureFormat.ARGBFloat, resolution);
        }

        private void Update()
        {
            if (Time.frameCount % (stepInterval + 1) != 0)
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
            
            cs.SetVector("hitXY", pos);
        }

        [Button]
        private void Step()
        {
            SetBrushPosition();
            _stepN++;
            
            cs.SetInt("stepn", _stepN);
            cs.SetFloat("time", TimeSeed);

            cs.SetTexture(_simulateKernelId, "readTex", _readTex);
            cs.Dispatch(_simulateKernelId, _agentsDiv, 1, 1);

            cs.SetTexture(_diffuseKernelId, "readTex", _readTex);
            cs.SetTexture(_diffuseKernelId, "writeTex", _stigmergyTex);
            cs.Dispatch(_diffuseKernelId, resolution, resolution, 1);
            
            cs.SetTexture(_trailKernelId, "writeTex", _stigmergyTex);
            cs.Dispatch(_trailKernelId, _agentsDiv, 1, 1);

            Utils.SwapTextures(ref _readTex, ref _stigmergyTex);
            Render();
        }

        private void Render()
        {
            cs.SetTexture(_renderKernelId, "readTex", _readTex);
            cs.Dispatch(_renderKernelId, resolution, resolution, 1);
            // cs.Dispatch(_renderAgentsKernelId, _agentsDiv, 1, 1);
            
            if (!Application.isPlaying)
            {
                UnityEditor.SceneView.RepaintAll();
            }
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
