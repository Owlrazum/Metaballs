using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CircleSDFFeature : ScriptableRendererFeature
{
    class CircleSDFPass : ScriptableRenderPass
    {
        private const string PROFILER_TAG = "CirlceSDFPass";

        private Material _metaballBlitMaterial;
        private RenderTargetIdentifier _cameraColorTargetIdentifier;

        private RenderTargetIdentifier _metaballRTIdentifier;
        private const string METABALL_RT_NAME = "MetaballRT";
        private int _metaballRTID = 2;

        public CircleSDFPass(
            Material metaBallBlitMaterialArg
            )
        {
            _metaballBlitMaterial = metaBallBlitMaterialArg;

            _metaballRTID = Shader.PropertyToID(METABALL_RT_NAME);
        }

        public void Setup(RenderTargetIdentifier cameraColorTargetIdent)
        {
            this._cameraColorTargetIdentifier = cameraColorTargetIdent;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(_metaballRTID, cameraTextureDescriptor);
            _metaballRTIdentifier = new RenderTargetIdentifier(_metaballRTID);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
            cmd.Clear();

            using (new ProfilingScope(cmd, new ProfilingSampler("FirstBlit")))
            { 
                cmd.Blit(_cameraColorTargetIdentifier, _metaballRTIdentifier, _metaballBlitMaterial, 0);
                cmd.SetRenderTarget(_cameraColorTargetIdentifier);
                cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 0, 0);
                cmd.Blit(_metaballRTIdentifier, _cameraColorTargetIdentifier);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_metaballRTID);
        }
    }


    public Material MetaballBlitMaterial;
    public RenderPassEvent RenderPassEvent;

    private CircleSDFPass pass;

    public override void Create()
    {
        pass = new CircleSDFPass(
            MetaballBlitMaterial
        );

        pass.renderPassEvent = RenderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var cameraColorTargetIdent = renderer.cameraColorTarget;
        pass.Setup(cameraColorTargetIdent);
        renderer.EnqueuePass(pass);
    }
}