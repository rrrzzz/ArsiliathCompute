Texture2D<float4> _InTex;
int _Rez;
float _Spacing;
float _Size;
float3 _Position;

float4x4 _TrMat;

PackedVaryingsType VertexInstanced(AttributesMesh inputMesh, uint instanceID : SV_InstanceID)
{
    // Instance object space
    float3 pos = inputMesh.positionOS;

    // SET COLOR

    float2 uv = float2(instanceID % _Rez, instanceID / _Rez);


    
    // float2 uv = float2(instanceID % _Rez / (float)_Rez, instanceID / _Rez / (float)_Rez);
    float4 col = _InTex[uv];
    
    // #ifdef ATTRIBUTES_NEED_COLOR
    // inputMesh.color = col;
    // #endif
    
    // Grid Position
    float3 gpos = 0;
    gpos.xz = uv - _Rez / 2.0;
    gpos *= _Spacing;

    pos.xz *= _Size;
    // SET POSITION
    // inputMesh.positionOS = mul(_TrMat, pos + gpos).xyz + _Position;
    inputMesh.positionOS = (pos + gpos).xyz;

    VaryingsType vt;
    vt.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(vt);
}

void FragInstanced(PackedVaryingsToPS packedInput,
    OUTPUT_GBUFFER(outGBuffer)
#ifdef _DEPTHOFFSET_ON
    , out float outputDepth : SV_Depth
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

    #ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
    #else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
    #endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);


    ///////////////////////////////////////////////
    // Workshop Customize

    surfaceData.baseColor = input.color.rgb;
    ///////////////////////////////////////////////


    ENCODE_INTO_GBUFFER(surfaceData, builtinData, posInput.positionSS, outGBuffer);

    #ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
    #endif
}