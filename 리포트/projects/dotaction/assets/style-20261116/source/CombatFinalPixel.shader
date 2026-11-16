Shader "DotAction/Uniform Pixel Palette"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _PixelGrid;
            float4 _Palette[64];
            int _PaletteCount;

            float4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = (floor(input.texcoord * _PixelGrid.xy / _PixelGrid.z) + .5) * _PixelGrid.z / _PixelGrid.xy;
                float3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;
                if (_PaletteCount > 0)
                {
                    float best = 1e20;
                    float3 mapped = _Palette[0].rgb;
                    for (int i = 0; i < _PaletteCount; i++)
                    {
                        float3 difference = color - _Palette[i].rgb;
                        float distance = dot(difference * difference, float3(.2126, .7152, .0722));
                        if (distance < best)
                        {
                            best = distance;
                            mapped = _Palette[i].rgb;
                        }
                    }
                    color = mapped;
                }
                return float4(color, 1);
            }
            ENDHLSL
        }
    }
}
