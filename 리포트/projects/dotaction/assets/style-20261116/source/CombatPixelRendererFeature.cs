using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DotAction.Rendering
{
    /// <summary>
    /// 최종 월드 출력에 정수 크기의 공통 픽셀 셀과 제한 팔레트를 적용한다.
    /// </summary>
    public sealed class CombatPixelRendererFeature : ScriptableRendererFeature
    {
        public Shader shader;
        private Material _material;
        private PixelPass _pass;

        public override void Create()
        {
            CoreUtils.Destroy(_material);
            if (shader != null)
            {
                _material = CoreUtils.CreateEngineMaterial(shader);
                _pass = new PixelPass(_material);
            }
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            CombatPixelStyle style = renderingData.cameraData.camera.GetComponent<CombatPixelStyle>();
            if (_pass != null && style != null && style.isActiveAndEnabled)
            {
                renderer.EnqueuePass(_pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
        }

        private sealed class PixelPass : ScriptableRenderPass
        {
            private readonly Material _material;
            private sealed class PassData
            {
                public TextureHandle source;
                public Material material;
            }

            public PixelPass(Material material)
            {
                _material = material;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing + 1;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                CombatPixelStyle style = cameraData.camera.GetComponent<CombatPixelStyle>();
                if (style == null || !style.isActiveAndEnabled || resources.isActiveTargetBackBuffer)
                {
                    return;
                }
                int width = cameraData.cameraTargetDescriptor.width;
                int height = cameraData.cameraTargetDescriptor.height;
                int scale = style.pixelize ? Mathf.Max(1, height / style.referenceHeight) : 1;
                _material.SetVector("_PixelGrid", new Vector4(width, height, scale, 0));
                int count = style.limitPalette && style.palette != null ? style.palette.Count : 0;
                var colors = new Vector4[64];
                for (int i = 0; i < count; i++)
                {
                    Color color = style.palette.colors[i];
                    colors[i] = QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
                }
                _material.SetVectorArray("_Palette", colors);
                _material.SetInt("_PaletteCount", count);
                TextureHandle source = resources.activeColorTexture;
                TextureDesc descriptor = graph.GetTextureDesc(source);
                descriptor.name = "Uniform Pixel Palette";
                descriptor.clearBuffer = false;
                TextureHandle destination = graph.CreateTexture(descriptor);
                using (var builder = graph.AddRasterRenderPass<PassData>("DotAction uniform pixels and palette", out var data))
                {
                    data.source = source;
                    data.material = _material;
                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                    builder.SetRenderFunc((PassData pass, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, pass.source, new Vector4(1, 1, 0, 0), pass.material, 0);
                    });
                }
                resources.cameraColor = destination;
            }
        }
    }
}
