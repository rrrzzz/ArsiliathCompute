using System;
using System.Numerics;
using EasyButtons;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector4 = UnityEngine.Vector4;

public class Ca1DManager : MonoBehaviour
{
    [SerializeField] 
    private ComputeShader caaCompute;
    [SerializeField]
    private Material caaMat;

    [Header("First params")]
    [SerializeField, Range(0,1f)]
    private float lambda;
    [SerializeField] 
    private int numStates;
    [SerializeField]
    private int neighbourCount;
    [SerializeField] 
    private int rulesTotal;
    
    [Header("Second params")]
    [SerializeField, Range(0,1f)]
    private float lambda2;
    [SerializeField]
    private int numStates2;
    [SerializeField]
    private int neighbourCount2;
    [SerializeField] 
    private int rules2Total;
    
    [Header("Common params")]
    [SerializeField] private int resolution;
    [SerializeField] private int stepInterval;
    [SerializeField] private int stepsPerFrame;
    [SerializeField] private int initRange;
    [SerializeField] private int interval;
    [SerializeField] private bool centerOnly;
    [SerializeField] private bool isInterval;
    [SerializeField] private bool useTwoRules;
    
    [SerializeField, Range(0,1)] private float hueMax;
    [SerializeField, Range(0,1)] private float satMax;
    [SerializeField, Range(0,1)] private float valMax;
    
    private static readonly int s_MainTex = Shader.PropertyToID("_UnlitColorMap");
    private float Seed => Time.realtimeSinceStartup % 1000;
    private int SeedRules => (int)Time.realtimeSinceStartup % 10000000;
    
    private int _simulateKernelId;
    private int _initKernelId;
    private int _rulesKernelId;
    private int _resetKernelId;
    private int _currentRow;
    private RenderTexture _stateTex;
    private RenderTexture _statesTexTo;
    private RenderTexture _renderTex;
    private bool _isSimulating;
    private ComputeBuffer _rulesBuffer;
    private ComputeBuffer _rulesBuffer2;

    private void OnValidate()
    {
        var total = BigInteger.Pow(numStates, neighbourCount);
        var total2 = BigInteger.Pow(numStates2, neighbourCount2);

        rulesTotal = total > Int32.MaxValue ? Int32.MaxValue : (int) total;
        rules2Total = total2 > Int32.MaxValue ? Int32.MaxValue : (int) total2;
    }

    private void Start()
    {
        _simulateKernelId = caaCompute.FindKernel("SimulateRow");
        _initKernelId = caaCompute.FindKernel("InitState");
        _resetKernelId = caaCompute.FindKernel("ResetState");
        SetRandomColors();
        
        InitRules();
    }

    private void Update()
    {
        if (!_isSimulating)
            return;
        
        if (Time.frameCount % (stepInterval + 1) != 0)
            return;
        
        for (int i = 0; i <= stepsPerFrame; i++)
        {
            if (_currentRow == resolution)
            {
                caaCompute.SetBool("_IsScrolling", true);
                caaCompute.SetInt("_RowId", resolution - 1);
                caaCompute.SetTexture(_simulateKernelId, "_StatesTex", _stateTex);
                caaCompute.SetTexture(_simulateKernelId, "_StatesTexTo", _statesTexTo);
                caaCompute.Dispatch(_simulateKernelId, resolution / 16 + 1, resolution / 16 + 1, 1);
                SwapTextures();
            }
            else
            {
                caaCompute.SetBool("_IsScrolling", false);
                caaCompute.SetInt("_RowId", _currentRow++);
                caaCompute.Dispatch(_simulateKernelId, resolution / 16 + 1, 1, 1);
            }
        }
    }

    private void SwapTextures()
    {
        var tmp = _statesTexTo;
        _statesTexTo = _stateTex;
        _stateTex = tmp;
    }

    [Button]
    private void InitState()
    {
        _isSimulating = false;
        _currentRow = 0;
        
        CreateTextures();
        caaMat.SetTexture(s_MainTex, _renderTex);
        caaCompute.SetTexture(_simulateKernelId, "_StatesTexTo", _statesTexTo);
        caaCompute.SetFloat("_Lambda", lambda);
        caaCompute.SetInt("_SeedRules", SeedRules);
        caaCompute.SetFloat("_Seed", Seed);
        caaCompute.SetInt("_Interval", interval);
        caaCompute.SetInt("_InitRange", initRange);
        caaCompute.SetBool("_IsCenterOnly", centerOnly);
        caaCompute.SetBool("_IsInterval", isInterval);
        caaCompute.SetInt("_NumStates", numStates);
        caaCompute.SetInt("_NumStates2", numStates2);
        caaCompute.SetInt("_Rez", resolution);
        caaCompute.SetInt("_NeighbourhoodSize", neighbourCount);
        caaCompute.SetInt("_NeighbourhoodSize2", neighbourCount2);
        caaCompute.SetTexture(_initKernelId, "_StatesTex", _stateTex);
        caaCompute.SetTexture(_initKernelId, "_RenderTex", _renderTex);
        caaCompute.Dispatch(_initKernelId, resolution / 16 + 1, 1, 1);
        StartSimulation();
    }

    [Button]
    private void StartSimulation()
    {
        
        caaCompute.SetBuffer(_simulateKernelId, "_Rules2", _rulesBuffer2);
        caaCompute.SetBuffer(_simulateKernelId, "_Rules", _rulesBuffer);
        caaCompute.SetTexture(_simulateKernelId, "_StatesTex", _stateTex);
        caaCompute.SetTexture(_simulateKernelId, "_RenderTex", _renderTex);
        
        _currentRow = 0;
        _isSimulating = true;
    }

    [Button]
    private void ResetSimulation()
    {
        caaCompute.SetTexture(_resetKernelId, "_RenderTex", _renderTex);
        caaCompute.Dispatch(_resetKernelId, resolution / 16 + 1, resolution / 16 + 1, 1);
    }
    
    [Button]
    private void InitRules()
    {
        var rules = GetRandomRules();
        _rulesBuffer?.Release();
    
        _rulesBuffer = new ComputeBuffer(rules.Length, sizeof(int));
        _rulesBuffer.SetData(rules);

        
            rules = GetRandomRules2();
            _rulesBuffer2?.Release();
            
            _rulesBuffer2 = new ComputeBuffer(rules.Length, sizeof(int));
            _rulesBuffer2.SetData(rules);
            caaCompute.SetBool("_UseTwoRules", useTwoRules);
        
        InitState();
    }
    

    [Button]
    private void ChangeRules()
    {
        _isSimulating = false;
        var rules = GetRandomRules();
        _rulesBuffer?.Release();
    
        _rulesBuffer = new ComputeBuffer(rules.Length, sizeof(int));
        _rulesBuffer.SetData(rules);
        caaCompute.SetBuffer(_simulateKernelId, "_Rules", _rulesBuffer);
            
            
        rules = GetRandomRules2();
        _rulesBuffer2?.Release();
        
        _rulesBuffer2 = new ComputeBuffer(rules.Length, sizeof(int));
        _rulesBuffer2.SetData(rules);
        caaCompute.SetBool("_UseTwoRules", useTwoRules);
        caaCompute.SetBuffer(_simulateKernelId, "_Rules2", _rulesBuffer2);
        
        
        caaCompute.SetInt("_NeighbourhoodSize", neighbourCount);
        caaCompute.SetInt("_NeighbourhoodSize2", neighbourCount2);
        _isSimulating = true;
    }

    private void CreateTextures()
    {
        _stateTex = Utils.CreateTexture(_stateTex, RenderTextureFormat.RFloat, resolution);
        _statesTexTo = Utils.CreateTexture(_statesTexTo, RenderTextureFormat.RFloat, resolution);
        _renderTex = Utils.CreateTexture(_renderTex, RenderTextureFormat.ARGB32, resolution);
    }

    private RenderTexture CreateTexture(RenderTexture tex, RenderTextureFormat format)
    {
        if (tex != null)
            tex.Release();
        
        tex = new RenderTexture(resolution, resolution, 0, format)
        {
            useMipMap = false,
            enableRandomWrite = true, 
            filterMode = FilterMode.Point, 
            wrapMode = TextureWrapMode.Repeat
        };

        tex.Create();

        return tex;
    }
    
    [Button]
    private void SetRandomColors()
    {
        var numS = Math.Max(numStates, numStates2);
        var colors = Utils.GetRandomGradientColors(hueMax, satMax, valMax, numS);
        caaCompute.SetVectorArray("_StateColors", colors);
    }

    int[] GetRandomRules()
    {
        var rules = new int[(int)Mathf.Pow(numStates, neighbourCount)];
        for (int i = 0; i < rules.Length; i++)
        {
            var random = Random.Range(0, 1f);
            if (random >= lambda)
            {
                rules[i] = numStates - 1;
            }
            else
            {
                rules[i] = Random.Range(0, numStates - 1);
            }
        }

        return rules;
    }
    
    int[] GetRandomRules2()
    {
        var rules = new int[(int)Mathf.Pow(numStates2, neighbourCount2)];
        for (int i = 0; i < rules.Length; i++)
        {
            var random = Random.Range(0, 1f);
            if (random >= lambda2)
            {
                rules[i] = numStates2 - 1;
            }
            else
            {
                rules[i] = Random.Range(0, numStates2 - 1);
            }
        }

        return rules;
    }

}
