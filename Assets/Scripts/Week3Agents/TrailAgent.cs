﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;

[ExecuteAlways]
public class TrailAgent : MonoBehaviour
{
    [Header("Trail Agent Params")]
    [Range(64, 1000000)]
    public int agentsCount = 1;
    private ComputeBuffer agentsBuffer;

    [Range(0, 1)]
    public float trailDecayFactor = .9f;


    [Header("Mouse Input")]
    [Range(0, 100)]
    public int brushSize = 10;
    public GameObject interactivePlane;
    protected Vector2 hitXY;


    [Header("Setup")]
    [Range(8, 2048)]
    public int rez = 8;

    [Range(0, 50)]
    public int stepsPerFrame = 0;

    [Range(1, 50)]
    public int stepMod = 1;

    public Material outMat;
    public ComputeShader cs;

    private RenderTexture readTex;
    private RenderTexture writeTex;
    private RenderTexture outTex;
    private RenderTexture debugTex;

    private int agentsDebugKernel;
    private int moveAgentsKernel;
    private int writeTrailsKernel;
    private int renderKernel;
    private int diffuseTextureKernel;


    protected List<ComputeBuffer> buffers;
    protected List<RenderTexture> textures;

    protected int stepn = -1;

    /* 
    *
    *
    * RESET
    *
    *
    */

    void Start()
    {
        Reset();
    }

    [Button]
    public void Reset()
    {
        agentsDebugKernel = cs.FindKernel("AgentsDebugKernel");
        moveAgentsKernel = cs.FindKernel("MoveAgentsKernel");
        renderKernel = cs.FindKernel("RenderKernel");
        writeTrailsKernel = cs.FindKernel("WriteTrailsKernel");
        diffuseTextureKernel = cs.FindKernel("DiffuseTextureKernel");

        readTex = CreateTexture(rez, FilterMode.Point);
        writeTex = CreateTexture(rez, FilterMode.Point);
        outTex = CreateTexture(rez, FilterMode.Point);
        debugTex = CreateTexture(rez, FilterMode.Point);

        agentsBuffer = new ComputeBuffer(agentsCount, sizeof(float) * 4);
        buffers.Add(agentsBuffer);

        GPUResetKernel();
        Render();
    }

    private void GPUResetKernel()
    {
        int kernel;

        cs.SetInt("rez", rez);
        cs.SetInt("time", Time.frameCount);

        kernel = cs.FindKernel("ResetTextureKernel");
        cs.SetTexture(kernel, "writeTex", writeTex);
        cs.Dispatch(kernel, rez, rez, 1);

        cs.SetTexture(kernel, "writeTex", readTex);
        cs.Dispatch(kernel, rez, rez, 1);


        kernel = cs.FindKernel("ResetAgentsKernel");
        cs.SetBuffer(kernel, "agentsBuffer", agentsBuffer);
        cs.Dispatch(kernel, agentsCount / 64, 1, 1);
    }


    /* 
    *
    *
    * STEP
    *
    *
    */
    void Update()
    {

        if (Time.frameCount % stepMod == 0)
        {
            for (int i = 0; i < stepsPerFrame; i++)
            {
                Step();
            }
        }

    }

    [Button]
    public void Step()
    {

        HandleInput();


        stepn += 1;
        cs.SetInt("time", Time.frameCount);
        cs.SetInt("stepn", stepn);
        cs.SetInt("brushSize", brushSize);
        cs.SetVector("hitXY", hitXY);

        GPUMoveAgentsKernel();

        if (stepn % 2 == 1)
        {
            GPUDiffuseTextureKernel();
            GPUWriteTrailsKernel();
            SwapTex();
        }

        Render();
    }

    void HandleInput()
    {
        if (!Input.GetMouseButton(0))
        {
            hitXY.x = hitXY.y = 0;
            return;
        }

        RaycastHit hit;
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        {
            return;
        }


        if (hit.transform != interactivePlane.transform)
        {
            return;
        }

        hitXY = hit.textureCoord * rez;
    }

    private void GPUDiffuseTextureKernel()
    {
        cs.SetTexture(diffuseTextureKernel, "readTex", readTex);
        cs.SetTexture(diffuseTextureKernel, "writeTex", writeTex);
        cs.SetFloat("trailDecayFactor", trailDecayFactor);

        cs.Dispatch(diffuseTextureKernel, rez, rez, 1);
    }


    private void GPUMoveAgentsKernel()
    {
        cs.SetBuffer(moveAgentsKernel, "agentsBuffer", agentsBuffer);
        cs.SetTexture(moveAgentsKernel, "readTex", readTex);
        cs.SetTexture(moveAgentsKernel, "debugTex", debugTex);

        cs.Dispatch(moveAgentsKernel, agentsCount / 64, 1, 1);
    }

    private void GPUWriteTrailsKernel()
    {
        cs.SetBuffer(writeTrailsKernel, "agentsBuffer", agentsBuffer);

        cs.SetTexture(writeTrailsKernel, "writeTex", writeTex);

        cs.Dispatch(writeTrailsKernel, agentsCount / 64, 1, 1);
    }

    private void SwapTex()
    {
        RenderTexture tmp = readTex;
        readTex = writeTex;
        writeTex = tmp;
    }

    /* 
    *
    *
    * RENDER
    *
    *
    */
    private void Render()
    {
        GPURenderKernel();
      
        outMat.SetTexture("_UnlitColorMap", outTex);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneView.RepaintAll();
        }
    }


    private void GPURenderKernel()
    {
        cs.SetTexture(renderKernel, "readTex", readTex);
        cs.SetTexture(renderKernel, "outTex", outTex);
        cs.SetTexture(renderKernel, "debugTex", debugTex);

        cs.Dispatch(renderKernel, rez, rez, 1);
    }


    private void GPUAgentsDebugKernel()
    {
        cs.SetBuffer(agentsDebugKernel, "agentsBuffer", agentsBuffer);
        cs.SetTexture(agentsDebugKernel, "outTex", outTex);

        cs.Dispatch(agentsDebugKernel, agentsCount / 64, 1, 1);
    }

    /* 
    *
    *
    * Util
    *
    *
    */
    public void Release()
    {
        if (buffers != null)
        {
            foreach (ComputeBuffer buffer in buffers)
            {
                if (buffer != null)
                {
                    buffer.Release();
                }
            }
        }

        buffers = new List<ComputeBuffer>();

        if (textures != null)
        {
            foreach (RenderTexture tex in textures)
            {
                if (tex != null)
                {
                    tex.Release();
                }
            }
        }

        textures = new List<RenderTexture>();

    }
    private void OnDestroy()
    {
        Release();
    }

    private void OnEnable()
    {
        Release();
    }

    private void OnDisable()
    {
        Release();
    }

    protected RenderTexture CreateTexture(int r, FilterMode filterMode)
    {
        RenderTexture texture = new RenderTexture(r, r, 1, RenderTextureFormat.ARGBFloat);

        texture.name = "out";
        texture.enableRandomWrite = true;
        texture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        texture.volumeDepth = 1;
        texture.filterMode = filterMode;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.autoGenerateMips = false;
        texture.useMipMap = false;
        texture.Create();
        textures.Add(texture);

        return texture;
    }

}
