using UnityEngine;
using Random = UnityEngine.Random;

public static class Utils
{
    private static int s_ID_MainTex = Shader.PropertyToID("_UnlitColorMap");

    public static void SwapTextures(ref RenderTexture first, ref RenderTexture second)
    {
        var tmp = first;
        first = second;
        second = tmp;
    }
    
    public static void SetVizTexture(RenderTexture tex, Material mat)
    {
        mat.SetTexture(s_ID_MainTex, tex);
    }
    
    public static RenderTexture CreateTexture(RenderTexture tex, RenderTextureFormat format, int resolution)
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

    public static Vector4[] GetRandomGradientColors(float hueMax, float satMax, float valMax, int length)
    {
        var grad = new Gradient();
        var keys = 8;

        var colorKeys = new GradientColorKey[keys];
        var alphaKeys = new GradientAlphaKey[keys];
        
        for (int i = 0; i < keys; i++)
        {
            var t = (float)i / (keys - 1);
            
            var randColor = Random.ColorHSV(0f, hueMax, 0, satMax, 0, valMax);
            
            colorKeys[i] = new GradientColorKey(randColor, t);
            alphaKeys[i] = new GradientAlphaKey(1, t);
        }
        
        grad.SetKeys(colorKeys, alphaKeys);

        var colorArray = new Vector4[length];
        
        for (int i = 0; i < length; i++) 
            colorArray[i] = grad.Evaluate(Random.value);

        return colorArray;
    }
}
