#if defined (UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<float3> _PosBuffer;
StructuredBuffer<float4> _ColorBuffer;
#endif


void ConfigureProcedural()
{}

void GetAttributes_float(float In, out float3 Position, out float4 Color)
{
    Position = 0;
    Color = 0;

    #if defined (UNITY_PROCEDURAL_INSTANCING_ENABLED)
    Position = _PosBuffer[(int)In];
    Color = _ColorBuffer[(int)In];
    #endif
}