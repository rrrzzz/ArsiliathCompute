using System.IO;
using EasyButtons;
using UnityEngine;
using Random = UnityEngine.Random;

public class CCAManager : MonoBehaviour
{
    private const int MaxNumStates = 32;
    private const int MaxThreshold = 25;
    private const int MaxRange = 20;
    private const string s_ReadTexID = "_ReadTexture";
    private const string s_WriteTexID = "_WriteTexture";

    [SerializeField] 
    private ComputeShader caaCompute;
    [SerializeField] 
    private int resolution;
    [SerializeField, Range(1, MaxThreshold)] 
    private int threshold;
    [SerializeField, Range(0,1)] 
    private float sampleThreshold = 0.5f;
    [SerializeField, Range(2, MaxNumStates)] 
    private int numStates;
    [SerializeField, Range(1, MaxRange)] 
    private int range;
    [SerializeField] 
    private bool moore;
    [SerializeField, Range(1, 50)] 
    private int stepsPerFrame;
    [SerializeField, Range(0, 50)] 
    private int framesToSkip;
    [SerializeField] 
    private Material caaMaterial;
    // [SerializeField] 
    // private Material maskMaterial;
    [SerializeField] 
    private ColorMode colorMode;

    [SerializeField] 
    private int maskRez;
    
    [SerializeField] 
    private int maskRezTrue;


    // private RenderTexture _maskReadTexture;
    // private RenderTexture _maskWriteTexture;
    // private RenderTexture _maskOutTexture;
    private RenderTexture _readTexture;
    private RenderTexture _writeTexture;
    private RenderTexture _outTexture;
    private int _resetKernelId;
    private int _simulateKernelId;
    private static readonly int s_MainTex = Shader.PropertyToID("_UnlitColorMap");
    private int _dispatchThreadCount;
  

    public void Start()
    {
        SetKernels();
        RandomizeColors();
        StartSimulation();
    }
    
    public void Update()
    {
        if (Time.frameCount % (framesToSkip + 1) != 0) return;

        for (int i = 0; i < stepsPerFrame; i++)
        {
            // caaCompute.SetTexture(_simulateKernelId, "_Mask", _maskOutTexture);
            caaCompute.SetTexture(_simulateKernelId, "_OutTexture", _outTexture);
            caaCompute.SetTexture(_simulateKernelId, s_ReadTexID, _readTexture);
            caaCompute.SetTexture(_simulateKernelId, s_WriteTexID, _writeTexture);
            caaCompute.Dispatch(_simulateKernelId, _dispatchThreadCount, _dispatchThreadCount, 1);
            SwapTextures();
            
            //
            // caaCompute.SetTexture(_simulateKernelId, "_OutTexture", _maskOutTexture);
            // caaCompute.SetTexture(_simulateKernelId, "_Mask", _outTexture);
            // caaCompute.SetTexture(_simulateKernelId, s_ReadTexID, _maskReadTexture);
            // caaCompute.SetTexture(_simulateKernelId, s_WriteTexID, _maskWriteTexture);
            // caaCompute.Dispatch(_simulateKernelId, _dispatchThreadCount, _dispatchThreadCount, 1);
            // SwapTexturesMask();
        }
    }
  
    [Button]
    private void StartSimulation()
    {
        _dispatchThreadCount = resolution / 16;
        CreateTextures();
        // CreateMaskTextures();
        caaMaterial.SetTexture(s_MainTex, _outTexture);
        // maskMaterial.SetTexture(s_MainTex, _maskOutTexture);
        ApplyParams();
        ResetReadTexture();
        // ResetMaskReadTexture();
    }

    [Button]
    private void RandomizeAndStart()
    {
        _dispatchThreadCount = resolution / 16;
        CreateTextures();
        // CreateMaskTextures();
        caaMaterial.SetTexture(s_MainTex, _outTexture);
        // maskMaterial.SetTexture(s_MainTex, _maskOutTexture);
        RandomizeParameters();
        RandomizeColors();
        ResetReadTexture();
        // ResetMaskReadTexture();
    }

    [Button]
    private void RandomizeParameters()
    {
        numStates = Random.Range(2, MaxNumStates + 1);
        threshold = Random.Range(1, MaxThreshold + 1);
        range = Random.Range(1, MaxRange);
        moore = Random.Range(0, 1f) > 0.5;
        ApplyParams();
    }

    [Button]
    private void RandomizeColorMode()
    {
        colorMode = (ColorMode)Random.Range(0, (int)ColorMode.StateFull + 1);
        caaCompute.SetInt("_ColorMode", (int)colorMode);
    }
    
    [Button]
    private void RandomizeColors()
    {
        var grad = new Gradient();
        var keys = 8;

        var colorKeys = new GradientColorKey[keys];
        var alphaKeys = new GradientAlphaKey[keys];
        
        for (int i = 0; i < keys; i++)
        {
            var t = (float)i / (keys - 1);
            
            var randColor = Random.ColorHSV(0f, 1, 0, 1, 1, 1);
            
            colorKeys[i] = new GradientColorKey(randColor, t);
            alphaKeys[i] = new GradientAlphaKey(1, t);
        }
        
        grad.SetKeys(colorKeys, alphaKeys);

        var colorArray = new Vector4[numStates];
        
        for (int i = 0; i < numStates; i++) 
            colorArray[i] = grad.Evaluate(Random.value);
        
        caaCompute.SetVectorArray("_StateColors", colorArray);
    }

    [Button]
    private void RandomizeParametersMildly()
    {
        numStates = Random.Range(numStates - 2, numStates + 3);
        threshold = Random.Range(threshold - 2, threshold + 3);
        range = Random.Range(range - 2, range + 3);
        moore = Random.Range(0, 1f) > 0.5;
        ApplyParams();
    }

    [Button]
    private void ApplyParams()
    {
        caaCompute.SetInt("_NumStates", numStates);
        caaCompute.SetInt("_Threshold", threshold);
        caaCompute.SetInt("_Range", range);
        caaCompute.SetBool("_Moore", moore);
        caaCompute.SetInt("_Rez", resolution);
        caaCompute.SetInt("_ColorMode", (int)colorMode);
        caaCompute.SetInt("_MaskRez", maskRez);
        caaCompute.SetFloat("_SampleThreshold", sampleThreshold);
    }


    [Button]
    private void SaveCurrentParams()
    {
        var textPath = Path.Combine(Application.dataPath, "Resources", "SelectedCCAs.txt");
        var text = $"\nThreshold: {threshold}, States: {numStates}, Range: {range}, Moore: {moore}, ColorMode: {colorMode}";
        using (var appendStream = File.AppendText(textPath))
        {
            appendStream.Write(text);
            appendStream.Close();
        }
    }
    
    private void ResetReadTexture()
    {
        caaCompute.SetTexture(_resetKernelId, s_WriteTexID, _writeTexture);
        caaCompute.Dispatch(_resetKernelId, _dispatchThreadCount, _dispatchThreadCount, 1);
        SwapTextures();
    }
    
    // private void ResetMaskReadTexture()
    // {
    //     caaCompute.SetTexture(_resetKernelId, s_WriteTexID, _maskWriteTexture);
    //     caaCompute.Dispatch(_resetKernelId, _dispatchThreadCount, _dispatchThreadCount, 1);
    //     SwapTexturesMask();
    // }
    
    private void CreateTextures()
    {
        _outTexture = CreateRenderTex(RenderTextureFormat.ARGB32, resolution);
        _writeTexture = CreateRenderTex(RenderTextureFormat.RFloat, resolution);
        _readTexture = CreateRenderTex(RenderTextureFormat.RFloat, resolution);
    }

    // private void CreateMaskTextures()
    // {
    //     _maskOutTexture = CreateRenderTex(RenderTextureFormat.ARGB32, maskRezTrue);
    //     _maskWriteTexture = CreateRenderTex(RenderTextureFormat.RFloat, maskRezTrue);
    //     _maskReadTexture = CreateRenderTex(RenderTextureFormat.RFloat, maskRezTrue);
    // }
    
    private RenderTexture CreateRenderTex(RenderTextureFormat format, int res)
    {
        var renderTex = new RenderTexture(res, res, 1, format);
        renderTex.useMipMap = false;
        renderTex.filterMode = FilterMode.Point;
        renderTex.enableRandomWrite = true;
        renderTex.wrapMode = TextureWrapMode.Repeat;
        renderTex.Create();
        
        return renderTex;
    }

    private void SwapTextures()
    {
        var tmp = _readTexture;
        _readTexture = _writeTexture;
        _writeTexture = tmp;
    }
    
    // private void SwapTexturesMask()
    // {
    //     var tmp = _maskReadTexture;
    //     _maskReadTexture = _maskWriteTexture;
    //     _maskWriteTexture = tmp;
    // }
    
    private void SetKernels()
    {
        _resetKernelId = caaCompute.FindKernel("Reset");
        _simulateKernelId = caaCompute.FindKernel("Simulate");
    }
}

public enum ColorMode
{
    StateNormalized,
    StateFade,
    ThresholdNormalized,
    ThresholdFade,
    BasicColor,
    ColorRange,
    ColorRangeFade,
    ThresholdRangeColor,
    ThresholdRangeColorFade,
    StateFull
}
