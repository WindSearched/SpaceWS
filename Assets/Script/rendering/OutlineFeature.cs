using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/*
 * Script from 埃罗毛阿老师(bilibili id: 83603546)
 * video link:
 * https://www.bilibili.com/video/BV16jCsYnE3a/?share_source=copy_web&vd_source=bd1a46c7f4e7dcab1c7a9b12c77082e8
 * Thanks for help!
 */

public class OutlineFeature : ScriptableRendererFeature
{
    [SerializeField] private Material m_outlineMaterial;
    private OutlineRenderPass m_OutlineRenderPass;

    private bool IsValidMaterial => m_outlineMaterial != null &&
                                    m_outlineMaterial.shader != null &&
                                    m_outlineMaterial.shader.isSupported;

    public override void Create()
    {
        if (!IsValidMaterial) return;

        m_OutlineRenderPass = new OutlineRenderPass(m_outlineMaterial);
        m_OutlineRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_OutlineRenderPass != null)
        {
            renderer.EnqueuePass(m_OutlineRenderPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        m_OutlineRenderPass?.Dispose();
    }

    class OutlineRenderPass : ScriptableRenderPass
    {
        private static readonly int s_ShaderPropertyOutlineMask = Shader.PropertyToID("_OutlineMask");

        private readonly Material m_outlineMaterial;
        private readonly FilteringSettings m_filteringSettings;
        private readonly MaterialPropertyBlock m_propertiesBlock;
        private RTHandle m_outlineMask;

        private static readonly List<ShaderTagId> s_shaderTagIds = new()
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly")
        };

        public OutlineRenderPass(Material outlineMaterial)
        {
            m_outlineMaterial = outlineMaterial;
            m_filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: 2);
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            m_propertiesBlock = new MaterialPropertyBlock();
        }

        public void Dispose()
        {
            m_outlineMask?.Release();
            m_outlineMask = null;
        }

        // Called before Execute. 用于创建 RT 等
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 创建 Outline Mask RT
            if (m_outlineMask == null || m_outlineMask.rt.width != renderingData.cameraData.cameraTargetDescriptor.width)
            {
                m_outlineMask?.Release();
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;
                desc.colorFormat = RenderTextureFormat.ARGB32;
                m_outlineMask = RTHandles.Alloc(desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_OutlineMask");
            }

            ConfigureTarget(m_outlineMask);
            ConfigureClear(ClearFlag.Color, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get("OutLine Command");

            // 设置 RenderTarget 并清空
            cmd.SetRenderTarget(m_outlineMask);
            cmd.ClearRenderTarget(false, true, Color.clear);

            // 创建 DrawingSettings
            var drawingSettings = CreateDrawingSettings(
                s_shaderTagIds,
                ref renderingData,
                SortingCriteria.None
            );

            // 创建 RendererList
            var rendererListParams = new RendererListParams(
                renderingData.cullResults,
                drawingSettings,
                m_filteringSettings
            );
            var rendererList = context.CreateRendererList(ref rendererListParams);

            // 绘制
            cmd.DrawRendererList(rendererList);

            //draw ouyline
            cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
            m_propertiesBlock.SetTexture(s_ShaderPropertyOutlineMask, m_outlineMask);
            cmd.DrawProcedural(Matrix4x4.identity, m_outlineMaterial, 0, MeshTopology.Triangles, 3, 1, m_propertiesBlock);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Cleanup 如果需要
        }
    }
}
